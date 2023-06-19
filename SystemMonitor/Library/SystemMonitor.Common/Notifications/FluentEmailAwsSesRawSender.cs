using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;

namespace SystemMonitor.Common.Notifications
{
    public class FluentEmailAwsSesRawSender : ISender
    {
        private readonly ILogger<FluentEmailAwsSesRawSender> _logger;
        private readonly EmailConfiguration _configuration;
        private AWSCredentials? _awsCredentials = null;

        public FluentEmailAwsSesRawSender(ILogger<FluentEmailAwsSesRawSender> logger, EmailConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            return SendAsync(email, token)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            var response = new SendResponse();
            try
            {
                var mailMessage = CreateMailMessage(email);
                if ((token.HasValue ? (token.GetValueOrDefault().IsCancellationRequested ? 1 : 0) : 0) != 0)
                {
                    response.ErrorMessages.Add("Message was cancelled by cancellation token.");
                    return response;
                }

                var credentials = GetAWSCredentials();
                var clientConfig = new AmazonSimpleEmailServiceConfig
                {
                    Timeout = _configuration.Timeout,
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_configuration.AwsSesConfiguration.AwsRegion),
                };
                using var client = new AmazonSimpleEmailServiceClient(credentials, clientConfig);

                await client.SendRawEmailAsync(mailMessage);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to '{(email?.Data?.ToAddresses.Any() == true ? string.Join(",", email.Data.ToAddresses) : "")}'");
                response.ErrorMessages.Add(ex.Message);
            }
            return response;
        }

        private AWSCredentials GetAWSCredentials()
        {
            if (_awsCredentials == null)
            {
                _awsCredentials = new BasicAWSCredentials(_configuration.AwsSesConfiguration.Credentials.AccessKey, _configuration.AwsSesConfiguration.Credentials.SecretKey);
            }
            return _awsCredentials;
        }

        /// <summary>
        ///     Maps and constructs the payload
        /// </summary>
        /// <remarks>
        ///    see https://github.com/awsdocs/amazon-ses-developer-guide/blob/master/doc-source/send-using-sdk-net.md
        /// </remarks>
        private SendRawEmailRequest CreateMailMessage(IFluentEmail email)
        {
            EmailData data = email.Data;

            var dataStream = new MemoryStream();
            var textWriter = new StreamWriter(dataStream);
            var boundary = Guid.NewGuid().ToString().Replace("-", "");
            var rawRequest = $@"From: {data.FromAddress.ToString()}
Reply-To: {data.ReplyToAddresses.Select(x => x.EmailAddress).First()}
To: {data.ToAddresses.Select(x => x.EmailAddress).First()}
Subject: {data.Subject}
MIME-Version: 1.0
Content-Type: multipart/alternative; boundary=mk3-{boundary}; charset=UTF-8

--mk3-{boundary}
Content-Type: text/plain; charset=UTF-8
Content-Transfer-Encoding: 7bit

{(string.IsNullOrWhiteSpace(data.PlaintextAlternativeBody) ? "" : data.PlaintextAlternativeBody)}

--mk3-{boundary}
Content-Type: text/html; charset=UTF-8
Content-Transfer-Encoding: 7bit

{data.Body}

--mk3-{boundary}--
";
            textWriter.Write(rawRequest);
            var sendRequest = new SendRawEmailRequest
            {
                Destinations = new List<string> { },
                RawMessage = new Amazon.SimpleEmail.Model.RawMessage
                {
                    Data = dataStream
                },
            };
            return sendRequest;
        }
    }
}
