using WallMonitor.Common.IO;

namespace WallMonitor.Agent
{

    public class DriveSpaceAvailableModule : IDisposable, IModule
    {
        public const string ModuleName = "DiskAvailable";
        public string Name => ModuleName;
        public string CurrentValue => IOHelper.GetFriendlyBytes(ValueAsDictionary.Sum(x => x.Value));
        public IDictionary<string, long> CurrentDictionary => ValueAsDictionary;

        public double Value => ValueAsDictionary.Sum(x => x.Value);
        public int ErrorCode { get; private set; }
        internal IDictionary<string, long> ValueAsDictionary { get; private set; } = new Dictionary<string, long>();

        private readonly System.Timers.Timer _timer = new(TimeSpan.FromSeconds(5));

        private DriveSpaceAvailableModule() { }

        public static async Task<IModule> CreateAsync()
        {
            var me = new DriveSpaceAvailableModule();
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
            ValueAsDictionary = await UpdateDiskAvailableWindowsAsync();
#endif
#if OS_LINUX
            ValueAsDictionary = await UpdateDiskAvailableUnixAsync();
#endif
        }

#if OS_WINDOWS
        private async Task<IDictionary<string, long>> UpdateDiskAvailableWindowsAsync()
        {
            var disks = new Dictionary<string, long>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    disks.Add(drive.Name, drive.AvailableFreeSpace);
                }
            }

            return disks;
        }
#endif

#if OS_LINUX
        private async Task<IDictionary<string, long>> UpdateDiskAvailableUnixAsync()
        {
            var disks = new Dictionary<string, long>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady && drive.TotalSize > 0)
                    {
                        disks.Add(drive.Name, drive.AvailableFreeSpace);
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
