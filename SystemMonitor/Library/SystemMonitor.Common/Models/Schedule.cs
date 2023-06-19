using Microsoft.Extensions.DependencyInjection;
using SystemMonitor.Common.Abstract;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Common.Models
{
    /// <summary>
    /// A Schedule contains the intervals, reporting history, and monitor for a given service.
    /// </summary>
    public class Schedule : ISchedule, IDisposable
    {
        private const int LockTimeout = 1000;
        private readonly object _dataLock = new();
        private HashSet<IHostResponse> _responseHistory = new();
        private readonly IServiceScope _serviceScope;

        public Guid Id { get; }
        public int HostId { get; }
        public int MonitorId { get; set; }
        public TimeSpan InitDelay { get; set; }
        public IList<IScheduleTime> Times { get; set; } = new List<IScheduleTime>();
        public IMonitorAsync? MonitorAsync { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int Attempts { get; set; }
        public IConfigurationParameters? ConfigurationParameters { get; set; }
        public string Name { get; set; } = string.Empty;
        public double? Value { get; set; }
        public string? Range { get; set; }

        /// <summary>
        /// The units the value should be displayed in
        /// </summary>
        public Units Units { get; set; } = Units.Auto;
        public IHost? Host { get; set; }
        public DateTime LastRuntime { get; set; }
        public DateTime LastUpTime { get; set; }
        /// <summary>
        /// The length of time the service has been down for previously
        /// </summary>
        public TimeSpan PreviousDownTime { get; set; }
        public IHostResponse? LastResponse { get; set; }
        public bool Notify { get; set; }
        public int ConsecutiveAttempts { get; set; }
        public HashSet<IHostResponse>? ResponseHistory
        {
            get
            {
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        return _responseHistory;
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }
                return null;
            }
            set
            {
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        _responseHistory = value;
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }
            }
        }

        public double UpTime
        {
            get
            {
                var downTimeSeconds = 0d;
                var upTime = 0d;
                var upCount = 0;
                var downCount = 0;
                var startTime = DateTime.MinValue;
                var endTime = DateTime.MinValue;
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        if (_responseHistory.Count > 0)
                        {
                            startTime = _responseHistory.OrderBy(x => x.DateChecked).FirstOrDefault()!.DateChecked;
                            endTime = _responseHistory.OrderByDescending(x => x.DateChecked).FirstOrDefault()!.DateChecked;

                            upCount = _responseHistory.Count(x => x.IsUp);
                            downCount = (int)Math.Abs(upCount - _responseHistory.Count());

                            // compute how many minutes of downtime there were
                            var downResponses = _responseHistory.Where(x => !x.IsUp).ToList();
                            // how often is each iteration?
                            var intervalSeconds = 0d;
                            foreach (var time in Times)
                            {
                                intervalSeconds += time.Interval.TotalSeconds;
                            }
                            foreach (var response in downResponses)
                            {
                                downTimeSeconds += intervalSeconds;
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }

                var timeSpan = endTime - startTime;
                // how much downtime?
                var timespanSeconds = timeSpan.TotalSeconds;
                if (timespanSeconds > 0)
                    upTime = (1 - (downTimeSeconds / timespanSeconds)) * 100.0;
                else
                    upTime = 100.0;
                /*
				if (upCount > 0)
					upTime = (1 - ((double)downCount / upCount)) * 100.0;
				else
					upTime = 0;*/
                return upTime;
            }
        }

        public double HealthStatus
        {
            get
            {
                var status = 0d;
                var countMinutes = 30;
                var startTime = DateTime.UtcNow.AddMinutes(-countMinutes);
                var upCount = 0;
                var downCount = 0;
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        var list = _responseHistory.Where(x => x.DateChecked > startTime).ToList();
                        upCount = list.Count(x => x.IsUp);
                        downCount = (int)Math.Abs(upCount - list.Count());
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }

                // get the status
                if (upCount > 0)
                    status = (double)Math.Round(1 - ((decimal)downCount / upCount), 1);
                else
                    status = 0;

                return status;
            }
        }

        public bool IsFlapping
        {
            get
            {
                var isFlapping = false;
                var count = 21;
                var upCount = 0;
                //int downCount = 0;
                var flappingPercent = 0d;
                var lowFlappingThreshold = 0.15;
                var highFlappingThreshold = 0.85;
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        // as per: http://nagios.sourceforge.net/docs/3_0/flapping.html
                        if (_responseHistory.Count >= count)
                        {
                            var responseList = _responseHistory.OrderByDescending(x => x.DateChecked).Take(count).ToList();
                            upCount = responseList.Count(x => x.IsUp);
                            //downCount = (int)Math.Abs(upCount - responseList.Count(x => !x.IsUp));
                            // compute the number of state changes (change of high to low, or low to high)
                            var stateChanges = 0;
                            for (var i = 0; i < responseList.Count; i++)
                            {
                                if (i < responseList.Count - 2 && responseList[i].IsUp != responseList[i + 1].IsUp)
                                    stateChanges++;
                            }
                            flappingPercent = (double)stateChanges / responseList.Count;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }
                // We consider a uptime downtime ratio as flapping if: less than 90% downtime and greater than 10 percent up.

                if (flappingPercent > lowFlappingThreshold && flappingPercent < highFlappingThreshold)
                    isFlapping = true;

                return isFlapping;
            }
        }

        public bool IsUp
        {
            get
            {
                var isUp = true;
                var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
                if (isLocked)
                {
                    try
                    {
                        if (LastResponse != null)
                        {
                            isUp = LastResponse.IsUp;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_dataLock);
                    }
                }
                return isUp;
            }
        }

        public Schedule(IServiceProvider serviceProvider, int hostId, int monitorId, string name, string monitorName, IHost host, TimeSpan timeout, int attempts)
        {
            Id = Guid.NewGuid();
            HostId = hostId;
            MonitorId = monitorId;
            Name = name;
            Attempts = attempts;
            Timeout = timeout;
            LastUpTime = DateTime.UtcNow;
            PreviousDownTime = TimeSpan.Zero;

            _serviceScope = serviceProvider.CreateScope();
            MonitorAsync = MonitorFactory.Create(_serviceScope, monitorName, monitorId);

            if (MonitorAsync == null)
                throw new ArgumentException($"Unknown Monitor module '{monitorName}'");

            // set the timeouts
            MonitorAsync.TimeoutMilliseconds = (long)timeout.TotalMilliseconds;

            Host = host;
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (isLocked)
            {
                try
                {
                    ResponseHistory = new HashSet<IHostResponse>();
                }
                finally
                {
                    Monitor.Exit(_dataLock);
                }
            }
        }

        public Schedule(IServiceProvider serviceProvider, int monitorId, string name, string monitorName)
        {
            Id = Guid.NewGuid();
            MonitorId = monitorId;
            _serviceScope = serviceProvider.CreateScope();
            MonitorAsync = MonitorFactory.Create(_serviceScope, monitorName, monitorId);

            if (MonitorAsync == null)
                throw new ArgumentException($"Unknown Monitor module '{monitorName}'");

            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (isLocked)
            {
                try
                {
                    ResponseHistory = new HashSet<IHostResponse>();
                }
                finally
                {
                    Monitor.Exit(_dataLock);
                }
            }
        }

        public IGraphData? ToGraphData(int recordCount = 500)
        {
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (isLocked)
            {
                try
                {
                    if (ResponseHistory != null && ResponseHistory.Count > 0)
                    {
                        var lastDataEntry = DateTime.MinValue;
                        if (ResponseHistory != null && ResponseHistory.Count > 0)
                            lastDataEntry = ResponseHistory.OrderByDescending(y => y.DateChecked).Take(1).FirstOrDefault()!.DateChecked;

                        var data = new List<double>();
                        var frequency = TimeSpan.FromSeconds(0);
                        if (ResponseHistory != null && ResponseHistory.Count > 0)
                            data = ResponseHistory.OrderBy(y => y.DateChecked).Take(recordCount).Select(x => x.IsUp ? x.ResponseTime.TotalMilliseconds : -1).ToList<double>();
                        if (ResponseHistory != null && ResponseHistory.Count > 1)
                            frequency = lastDataEntry - ResponseHistory.OrderByDescending(y => y.DateChecked).Take(2).ToList()[1].DateChecked;
                        IGraphData graphData = new GraphData()
                        {
                            Data = data,
                            LastDataEntry = lastDataEntry,
                            Frequency = frequency
                        };
                        return graphData;
                    }

                    return new GraphData();
                }
                finally
                {
                    Monitor.Exit(_dataLock);
                }
            }
            return null;
        }

        public void AddResponseHistory(IHostResponse response)
        {
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (!isLocked) return;

            try
            {
                if (IsUp) 
                    LastUpTime = DateTime.UtcNow;
                else
                    PreviousDownTime = DateTime.UtcNow.Subtract(LastUpTime);
                _responseHistory.Add(response);
            }
            finally
            {
                Monitor.Exit(_dataLock);
            }
        }

        public void RemoveWhere(Predicate<IHostResponse> match)
        {
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (!isLocked) return;

            try
            {
                _responseHistory.RemoveWhere(match);
            }
            finally
            {
                Monitor.Exit(_dataLock);
            }
        }

        public int Count(Func<IHostResponse, bool> match)
        {
            var result = 0;
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (!isLocked) return result;

            try
            {
                result = _responseHistory.Count(match);
            }
            finally
            {
                Monitor.Exit(_dataLock);
            }
            return result;
        }

        public void Clear()
        {
            var isLocked = Monitor.TryEnter(_dataLock, LockTimeout);
            if (!isLocked) return;

            try
            {
                _responseHistory.Clear();
            }
            finally
            {
                Monitor.Exit(_dataLock);
            }
        }

        public override bool Equals(object? obj)
        {
            var scheduleObj = obj as ISchedule;
            return Id.Equals(scheduleObj?.Id);
        }

        public override string ToString()
        {
            if (MonitorAsync != null)
                return $"{Name}:{MonitorAsync.ServiceName}";
            else
                return Name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Schedule a, Schedule b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Schedule a, Schedule b)
        {
            return !a.Equals(b);
        }

        public void Dispose()
        {
            MonitorAsync?.Dispose();
            _serviceScope.Dispose();
        }
    }
}
