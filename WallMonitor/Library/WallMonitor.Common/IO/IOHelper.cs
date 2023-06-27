namespace WallMonitor.Common.IO
{
    public static class IOHelper
    {
        /// <summary>
        /// Get the size of bytes in a human readable format
        /// </summary>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static string GetFriendlyBytes(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num) + suf[place];
        }
    }
}
