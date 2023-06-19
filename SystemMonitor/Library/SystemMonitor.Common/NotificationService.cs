using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using SystemMonitor.Common.Models;

namespace SystemMonitor.Common
{
    /// <summary>
    /// Handles managing notification messages
    /// </summary>
    public class NotificationService : IDisposable
    {
        public List<NotificationGroup> NotificationGroups { get; set; }
        public string? MailServer { get; set; }
        public int MailPort { get; set; }
        public string? MailUsername { get; set; }
        public string? MailPassword { get; set; }
        public string? MailFromAddress { get; set; }
        public int MailTimeout { get; set; }
        public string LogPath { get; set; }

        private readonly LogService _logService;
        private readonly object _messageQueueLock = new();
        private readonly Queue<NotificationData> _messageQueue = new();
        private readonly ManualResetEvent _closeSignal = new(false);
        private readonly List<NotificationData> _messageHistory = new();
        private readonly MessageFactory _messageFactory = new();
        private Thread? _messageQueueThread;
        private bool _isDisposed;


        public NotificationService(string logPath, string mailServer, string mailFromAddress, int mailTimeout = 10000)
        {
            LogPath = logPath;
            NotificationGroups = new List<NotificationGroup>();
            MailServer = mailServer;
            MailPort = 25;
            MailFromAddress = mailFromAddress;
            MailTimeout = mailTimeout;
            _logService = new LogService(logPath, RolloverType.Rollover);
        }

        public NotificationService(string logPath, string mailServer, int mailPort, string mailFromAddress, string mailUsername, string mailPassword, int mailTimeout = 10000)
        {
            LogPath = logPath;
            NotificationGroups = new List<NotificationGroup>();
            MailServer = mailServer;
            MailPort = mailPort;
            MailFromAddress = mailFromAddress;
            MailUsername = mailUsername;
            MailPassword = mailPassword;
            MailTimeout = mailTimeout;
            _logService = new LogService(logPath, RolloverType.Rollover);
        }

        public void Add(NotificationGroup notificationGroup)
        {
            if (_isDisposed) throw new ObjectDisposedException("NotificationService");

            NotificationGroups.Add(notificationGroup);
        }

        public void QueueMessage(NotificationData message)
        {
            if (_isDisposed) throw new ObjectDisposedException("NotificationService");

            lock (_messageQueueLock)
                _messageQueue.Enqueue(message);
        }

        public void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException("NotificationService");
            LaunchMessageQueueThread();
        }

        public void Stop()
        {
            _closeSignal.Set();
        }

        /// <summary>
        /// Launch the dispatch thread
        /// </summary>
        private void LaunchMessageQueueThread()
        {
            if (_isDisposed) throw new ObjectDisposedException("NotificationService");

            _closeSignal.Reset();
            _messageQueueThread = new Thread(new ThreadStart(MessageQueueThread));
            _messageQueueThread.Priority = ThreadPriority.Normal;
            _messageQueueThread.Start();
        }

        private void MessageQueueThread()
        {
            while (!_closeSignal.WaitOne(500))
            {
                NotificationData message = null;
                lock (_messageQueueLock)
                {
                    var messageCount = _messageQueue.Count;
                    if (messageCount > 0)
                    {
                        message = _messageQueue.Dequeue();
                    }
                }

                if (message == null) continue;

                foreach (var group in NotificationGroups)
                {
                    if (group.Enabled)
                    {
                        var isWithinRepeatInterval = CheckRepeatInterval(group, message);
                        var isWithinMinThreshold = CheckMinThreshold(group, message);
                        // determine if this message belongs to a group of consecutive messages
                        var findIncidentNumber = FindIncidentNumber(message);
                        message.IncidentNumber = findIncidentNumber;

                        if (isWithinRepeatInterval && isWithinMinThreshold)
                        {
                            Debug.WriteLine($"Sending message to group {group.Name} {message.Schedule.Name}:{message.Schedule.MonitorAsync.ServiceName} IsUp:{message.IsUp}");
                            foreach (var member in group.Members)
                                SendEmailMessage(member, message);
                            foreach (var member in group.Members)
                                SendPhoneMessage(member, message);
                            message.IsSent = true;
                        }
                        else
                        {
                            Debug.WriteLine(
                                $"Ignoring message to group {group.Name} {message.Schedule.Name}:{message.Schedule.MonitorAsync.ServiceName} IsUp:{message.IsUp} until repeat interval is reached.");
                            message.IsSent = false;
                        }
                        // add message to history, noting if it was sent or filtered
                        _messageHistory.Add(message);
                    }
                    PruneMessageHistory();
                }
            }
        }

        /// <summary>
        /// Determine if this notification is part of the same series of notifications (series of consecutive down msgs, or a new up message)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string FindIncidentNumber(NotificationData message)
        {
            string incidentNumber = null;

            //todo: this could be dangerous is business logic is changed.
            // currently, this method is called before the current message is added to the response history
            //logic: if the previous message was a down message, then its part of the group.


            if (message.IsUp)
            {
                var previousMessage = _messageHistory.Where(x => x.Schedule.Id == message.Schedule.Id).OrderByDescending(x => x.DateCreated).FirstOrDefault();
                var nextIncidentNumber = NotificationData.GenerateIncidentNumber();
                if (previousMessage != null)
                {
                    incidentNumber = previousMessage.IncidentNumber;
                }
                else
                {
                    incidentNumber = nextIncidentNumber;
                }
                return incidentNumber;
            }


            // find the last message that was sent, and grab its incident number
            var lastMessage = _messageHistory.Where(x => x.Schedule.Id == message.Schedule.Id).OrderByDescending(x => x.DateCreated).FirstOrDefault();
            if (lastMessage != null)
            {
                incidentNumber = NotificationData.GetCurrentIncidentNumber();
            }
            else
            {
                incidentNumber = NotificationData.GenerateIncidentNumber();
            }
            return incidentNumber;
        }

        /// <summary>
        /// Check the history of messages that has been sent, and return true if we have not exceeded the repeat interval.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool CheckMinThreshold(NotificationGroup group, NotificationData message)
        {
            // we only check the threshold if this is a down message.
            if (!message.IsUp)
            {
                // count how many down messages there are near the last message
                var timeFrame = message.Schedule.Times.First().Interval;

                var mostRecentMessages = _messageHistory.Where(x =>
                        x.Schedule.Id == message.Schedule.Id
                        && x.IsUp == message.IsUp
                        && x.DateCreated >= DateTime.UtcNow.AddSeconds(-1 * ((group.MinAlertThreshold + 1) * timeFrame.TotalSeconds))
                        ).ToList();
                if (mostRecentMessages.Count >= group.MinAlertThreshold)
                {
                    // threshold is hit
                    return true;
                }
            }
            else
            {
                // auto allow all up messages
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check the history of messages that has been sent, and return true if we have not exceeded the repeat interval.
        /// </summary>
        /// <param name="group">The group the message is being sent to</param>
        /// <param name="message">The message to be sent</param>
        /// <returns></returns>
        private bool CheckRepeatInterval(NotificationGroup group, NotificationData message)
        {
            var lastMessageForSchedule = _messageHistory.Where(x =>
                    x.IsSent == true
                    && x.Schedule.Id == message.Schedule.Id
                    && x.IsUp == message.IsUp)
                .OrderByDescending(x => x.DateCreated).FirstOrDefault();
            if (lastMessageForSchedule != null)
            {
                var diff = DateTime.UtcNow - lastMessageForSchedule.DateCreated;
                if (diff.TotalSeconds < group.MaxRepeatIntervalSeconds)
                    return false;
            }

            return true;
        }

        private void PruneMessageHistory()
        {
            if (_messageHistory.Count > 5000)
                _messageHistory.RemoveRange(0, _messageHistory.Count - 5000);
        }

        private void SendEmailMessage(NotificationGroupMember recipient, NotificationData message)
        {
            if (_isDisposed || string.IsNullOrEmpty(MailFromAddress) || string.IsNullOrEmpty(MailServer))  return;

            var emailMessage = _messageFactory.CreateMessage(-1, message.Schedule, message.IsUp, recipient.MessageFormat, message.BroadcastMessage, new Dictionary<string, string>() { { "INCIDENTNUMBER", message.IncidentNumber } });

            _logService.Info("Incident[{2}] Email[{0}] with message: {1}", recipient.Email, emailMessage.Message, message.IncidentNumber);
            var client = new SmtpClient(MailServer, MailPort);
            client.Timeout = MailTimeout;
            var fromAddress = new MailAddress(MailFromAddress);
            var mail = new MailMessage();


            if (!string.IsNullOrEmpty(MailUsername))
            {
                var basicCredential = new NetworkCredential(MailUsername, MailPassword);
                client.UseDefaultCredentials = false;
                client.Credentials = basicCredential;
            }

            mail.From = fromAddress;
            mail.Subject = emailMessage.Subject;
            mail.Body = recipient.Html ? emailMessage.HTMLMessage : emailMessage.Message;
            mail.IsBodyHtml = true;
            mail.To.Add(recipient.Email);
            try
            {
                client.Send(mail);
            }
            catch (Exception)
            {
            }
            mail.Dispose();
            client.Dispose();
        }

        private void SendPhoneMessage(NotificationGroupMember recipient, NotificationData message)
        {
            if (_isDisposed) return;

            var phoneMessage = _messageFactory.CreateMessage(-1, message.Schedule, message.IsUp, recipient.MessageFormat, message.BroadcastMessage, new Dictionary<string, string>() { { "INCIDENTNUMBER", message.IncidentNumber } });
            _logService.Info("Incident[{2}] Phone[{0}] with message: {1}", recipient.Phone, phoneMessage.Message, message.IncidentNumber);
        }

        /// <summary>
        /// Do cleanup work
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    Stop();

                    lock (_messageQueueLock)
                    {
                        _messageQueue.Clear();
                    }
                    _closeSignal.Dispose();
                    _logService.Dispose();
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Dispose(true);
            GC.SuppressFinalize(this);

        }
    }
}
