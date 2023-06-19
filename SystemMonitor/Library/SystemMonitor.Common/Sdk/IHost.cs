using System.Net;

namespace SystemMonitor.Common.Sdk
{
    public interface IHost
	{
		/// <summary>
		/// Host name
		/// </summary>
		string? Name { get; set; }

		/// <summary>
		/// Ip address
		/// </summary>
		IPAddress? Ip { get; set; }

		/// <summary>
		/// Host Uri
		/// </summary>
		Uri? Hostname { get; set; }
	}
}
