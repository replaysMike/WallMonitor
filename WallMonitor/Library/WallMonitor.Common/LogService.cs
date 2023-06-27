using System.Text;

namespace WallMonitor.Common
{
    /// <summary>
    /// Logging class
    /// </summary>
    public class LogService : IDisposable
	{
		const int MaxInMemoryLength = 1024 * 512;

		private readonly StringBuilder _sb = new (MaxInMemoryLength * 2);
		private readonly object _logLock = new ();
		private readonly object _fileLock = new ();
        private readonly TimeSpan _maxLogFileFlushInterval = new (0,0,5);
        private readonly Timer _flushTimer;
		private bool _isDisposed;

		/// <summary>
		/// The logfile filename path
		/// </summary>
		public string LogFile { get; set; } = "";

        /// <summary>
		/// Maximum log file size for rollover (default: 64MB)
		/// </summary>
		public long MaxLogFileSize { get; set; } = 1024 * 1024 * 64;

        /// <summary>
		/// The type of rollover mechanism to use
		/// </summary>
		public RolloverType RolloverType { get; set; } = RolloverType.Rollover;

        public LogService(string filename)
		{
			LogFile = filename;
			_flushTimer = new Timer(Timer_tick, null, (int)_maxLogFileFlushInterval.TotalMilliseconds, Timeout.Infinite);
		}

		public LogService(string filename, RolloverType rolloverType)
		{
			LogFile = filename;
			RolloverType = rolloverType;
			_flushTimer = new Timer(Timer_tick, null, (int)_maxLogFileFlushInterval.TotalMilliseconds, Timeout.Infinite);
		}

		public void Debug(string message, params string[] p)
		{
			if (_isDisposed) throw new ObjectDisposedException("LogService");
			WriteLine(LogLevel.Debug, message, p);
		}

		public void Info(string message, params string[] p)
		{
			if (_isDisposed) throw new ObjectDisposedException("LogService");
			WriteLine(LogLevel.Info, message, p);
		}

		public void Warn(string message, params string[] p)
		{
			if (_isDisposed) throw new ObjectDisposedException("LogService");
			WriteLine(LogLevel.Warning, message, p);
		}

		public void Error(string message, params string[] p)
		{
			if (_isDisposed) throw new ObjectDisposedException("LogService");
			WriteLine(LogLevel.Error, message, p);
		}

		/// <summary>
		/// Log a message
		/// </summary>
		public void WriteLine(LogLevel logLevel, string message, params string[] p)
		{
			if (_isDisposed) throw new ObjectDisposedException("LogService");

			var fullMessage = string.Format(message, p);
			fullMessage = $"@@{DateTime.UtcNow:yyyy-MM-dd hh:mm:ss tt} [{logLevel}]: {fullMessage}{Environment.NewLine}";
			LogToMemory(fullMessage);
		}

		private void LogToMemory(string message)
		{
			long stringLength = 0;
			lock (_logLock)
			{
				_sb.Append(message);
				stringLength = _sb.Length;
			}
			if (stringLength >= MaxInMemoryLength)
			{
				FlushMemoryToDisk();
			}
		}

		private void Timer_tick(object state)
		{
			// if there is data to log, log it
			var startTime = DateTime.UtcNow;
			long stringLength = 0;
			lock (_logLock)
				stringLength = _sb.Length;
			if (stringLength > 0)
				FlushMemoryToDisk();
			var elapsed = DateTime.UtcNow - startTime;
			_flushTimer.Change((int)_maxLogFileFlushInterval.TotalMilliseconds, Timeout.Infinite);
		}

		private void FlushMemoryToDisk()
		{
			// lock all file manipulation operations
			lock (_fileLock)
			{
				try
				{
					var flushData = "";
					lock (_logLock)
					{
						flushData = _sb.ToString();
						_sb.Clear();
					}
					if (flushData.Length > 0 && !string.IsNullOrEmpty(LogFile))
					{
						var ext = Path.GetExtension(LogFile);
						var filename = Path.GetFileNameWithoutExtension(LogFile);
						var filepath = Path.GetPathRoot(LogFile);
						var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
						var file = $"{filename}{ext}";
						File.AppendAllText(file, flushData, Encoding.UTF8);

						if (File.Exists(file))
						{
							var fi = new FileInfo(file);
							if (fi.Length >= MaxLogFileSize)
							{
								// do rollover
								if (RolloverType == RolloverType.Rollover)
								{
									// move file to new name
									var num = 1;
									// find out how many log files we have for today
									var files = Directory.GetFiles(filepath, $"{filename}-{dateStr}*.{ext}", SearchOption.TopDirectoryOnly);
									num = files.Length + 1;
									var rolloverFilename = $"{filename}-{dateStr}_{num}{ext}";
									try
									{
										File.Move(file, rolloverFilename);
									}
									catch (Exception ex)
									{
										// err
									}
								}
								else if (RolloverType == RolloverType.Truncate)
								{
									// truncate the file to the correct size (this implementation uses more memory)
									var extraBytes = fi.Length - MaxLogFileSize;
									if (extraBytes > 0)
									{
										using (var ms = new MemoryStream((int)MaxLogFileSize))
										{
											using (var s = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
											{

												s.Seek(-1 * MaxLogFileSize, SeekOrigin.End);
												s.CopyTo(ms);
												s.SetLength(MaxLogFileSize);
												s.Position = 0;
												ms.Position = 0;
												ms.CopyTo(s);
											}
										}
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					// err
				}
			}
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposing) return;
			if (isDisposing)
			{
				_flushTimer.Dispose();
				FlushMemoryToDisk();
				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
