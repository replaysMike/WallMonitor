using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using SystemMonitor.Common;
using SystemMonitor.Common.Sdk;

namespace SystemMonitor.Monitors
{
    /// <summary>
    /// Checks for the existence of a database using an active connection
    /// </summary>
    public class PostgresqlServerMonitorAsync : IMonitorAsync
    {
        private string Database { get; set; } = string.Empty;
        private string? ServerVersion { get; set; }
        public string ServiceName => "Postgresql";
        public string ServiceDescription => "Monitors Postgresql database with optional query execution.";
        public int Iteration { get; private set; }

        public string DisplayName => ServiceName;
        public int MonitorId { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public string ConfigurationDescription => $"Host: {Host}\r\nUsername: {Username}";
        public string Host { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public GraphType GraphType => GraphType.Value;
        private readonly ILogger _logger;

        public PostgresqlServerMonitorAsync(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IHostResponse> CheckHostAsync(IHost host, IConfigurationParameters parameters, System.Threading.CancellationToken cancelToken)
        {
            Iteration++;
            if (TimeoutMilliseconds <= 0)
                TimeoutMilliseconds = 5000;

            var response = HostResponse.Create();
            try
            {
                var startTime = DateTime.UtcNow;
                var connectionString = "";
                var query = "";
                var matchType = "Value > 0";
                if (parameters.Any())
                {
                    if (parameters.Contains("MatchType"))
                        matchType = parameters.Get<string>("MatchType");
                    if (parameters.Contains("ConnectionString"))
                        connectionString = parameters.Get("ConnectionString");
                    if (parameters.Contains("Query"))
                        query = parameters.Get("Query");
                }

                if (!string.IsNullOrEmpty(connectionString))
                {
                    try
                    {
                        // set timeout in seconds
                        var timeoutInt = (int)(TimeoutMilliseconds / 1000);
                        // make sure our timeout is always > 0, or it will wait infinitely
                        if (timeoutInt <= 0)
                            timeoutInt = 1;
                        if (connectionString.IndexOf("Timeout=", StringComparison.InvariantCultureIgnoreCase) < 0)
                            connectionString += $"Timeout={timeoutInt};";
                        var builder = new NpgsqlConnectionStringBuilder(connectionString);
                        Host = builder.Host;
                        Username = builder.Username;
                        await using var connection = new NpgsqlConnection(connectionString);
                        Database = connection.Database;
                        await connection.OpenAsync(cancelToken);

                        ServerVersion = connection.ServerVersion;
                        if (!string.IsNullOrEmpty(query))
                        {
                            await using var cmd = connection.CreateCommand();
                            cmd.CommandText = query;
                            var count = 0;
                            try
                            {
                                // use a DataReader for flexibility. Supports SELECT COUNT(*), SELECT *, SELECT SUM(Column)
                                var reader = await cmd.ExecuteReaderAsync(cancelToken);
                                if (reader.HasRows)
                                {
                                    var dt = new DataTable();
                                    dt.Load(reader);
                                    count = dt.Rows.Count;
                                    if (dt.Rows.Count == 1 && dt.Columns.Count == 1)
                                    {
                                        var valueObj = dt.Rows[0][0];
                                        try
                                        {
                                            response.Value = (double)Convert.ChangeType(valueObj, typeof(double));
                                        }
                                        catch (FormatException ex)
                                        {
                                            _logger.LogError(ex, $"The value returned by the '{query}' was not a numeric type and cannot be evaluated.");
                                        }
                                    }
                                }
                            }
                            catch (NpgsqlException ex)
                            {
                                _logger.LogError(ex, $"Error executing query '{query}'");
                            }
                            response.IsUp = MatchComparer.Compare("Value", response.Value ?? 0, "Count", count, matchType);
                        }
                        else
                        {
                            response.IsUp = true;
                        }
                        connection.Close();
                        response.ResponseTime = DateTime.UtcNow - startTime;
                    }
                    catch (NpgsqlException ex)
                    {
                        response.IsUp = false;
                    }
                }
            }
            catch (Exception ex)
            {
                response.IsUp = false;
                _logger.LogError(ex, $"Exception thrown in '{nameof(PostgresqlServerMonitorAsync)}'");
            }

            return response;
        }

        public void Dispose()
        {

        }
    }
}
