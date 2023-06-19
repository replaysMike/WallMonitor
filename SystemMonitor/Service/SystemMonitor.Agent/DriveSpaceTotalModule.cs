using SystemMonitor.Common.IO;

namespace SystemMonitor.Agent
{

    public class DriveSpaceTotalModule : IDisposable, IModule
    {
        public const string ModuleName = "DiskInstalled";
        public string Name => ModuleName;
        public string CurrentValue => IOHelper.GetFriendlyBytes(ValueAsDictionary.Sum(x => x.Value) / 1024 / 1024 / 1024);
        public IDictionary<string, long> CurrentDictionary => ValueAsDictionary;

        public double Value => ValueAsDictionary.Sum(x => x.Value);
        public int ErrorCode { get; private set; }
        internal IDictionary<string, long> ValueAsDictionary { get; private set; } = new Dictionary<string, long>();

        // use a long timer, memory size will only ever change on capable hot addable hardware (such as VMs)
        private readonly System.Timers.Timer _timer = new(TimeSpan.FromMinutes(1));

        private DriveSpaceTotalModule() { }

        public static async Task<IModule> CreateAsync()
        {
            var me = new DriveSpaceTotalModule();
            await me.InitializeAsync();
            return me;
        }

        private async Task InitializeAsync()
        {
            // run immediately
            await UpdateAsync();

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private async void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            await UpdateAsync();
            _timer.Start();
        }

        private async Task UpdateAsync()
        {
#if OS_WINDOWS
            ValueAsDictionary = await UpdateDiskInstalledWindowsAsync();
#endif
#if OS_LINUX
            ValueAsDictionary = await UpdateDiskInstalledUnixAsync();
#endif
        }

#if OS_WINDOWS
        private async Task<IDictionary<string, long>> UpdateDiskInstalledWindowsAsync()
        {
            var disks = new Dictionary<string, long>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    disks.Add(drive.Name, drive.TotalSize);
                }
            }

            return disks;
        }
#endif

#if OS_LINUX
        private async Task<IDictionary<string, long>> UpdateDiskInstalledUnixAsync()
        {
            var disks = new Dictionary<string, long>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady && drive.TotalSize > 0)
                    {
                        disks.Add(drive.Name, drive.TotalSize);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return disks;
        }
#endif

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
