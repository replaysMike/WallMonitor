using System.Text.RegularExpressions;
using SystemMonitor.Common.Abstract;

namespace SystemMonitor.Common.Models
{
    public static class ScheduleGenerator
    {
        public static IList<IScheduleTime> GenerateScheduleTimesFromInterval(TimeSpan timespan)
        {
            IList<IScheduleTime> times = new List<IScheduleTime>();
            var time = new ScheduleTime(timespan);
            times.Add(time);
            return times;
        }

        public static IList<IScheduleTime> GenerateScheduleTimesFromChrontabFormat(string scheduleFormatted)
        {
            // todo: rewrite this shit, it is embarrassing.
            // parse extended chrontab format: ([sec] [min] [hour] [day of month] [month] [day of week]),([sec] [min] [hour] [day of month] [month] [day of week])
            IList<IScheduleTime> times = new List<IScheduleTime>();
            var chronSchedules = new List<string>();
            var groups = Regex.Matches(@"\[.*?\]", scheduleFormatted);
            if (groups.Count > 0)
            {
                foreach (var group in groups)
                    chronSchedules.Add(group.ToString().Replace("(", "").Replace(")", ""));
            }
            else
            {
                chronSchedules.Add(scheduleFormatted);
            }
            foreach (var group in chronSchedules)
            {
                var parts = group.ToString().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var sSeconds = "*";
                var sMinutes = "*/1";
                var sHours = "*";
                var sDayOfMonth = "*";
                var sMonth = "*";
                var sDayOfWeek = "*";
                var iSeconds = 0;
                var xSeconds = 0;
                var iMinutes = 0;
                var xMinutes = 0;
                var iHours = 0;
                var xHours = 0;
                var iDayOfMonth = 0;
                var xDayOfMonth = 0;
                var iMonth = 0;
                var xMonth = 0;
                var iDayOfWeek = 0;
                var xDayOfWeek = 0;

                if (parts.Length > 0)
                    sSeconds = parts[0];
                if (parts.Length > 1)
                    sMinutes = parts[1];
                if (parts.Length > 2)
                    sHours = parts[2];
                if (parts.Length > 3)
                    sDayOfMonth = parts[3];
                if (parts.Length > 4)
                    sMonth = parts[4];
                if (parts.Length > 5)
                    sDayOfWeek = parts[5];

                var timeSpan = new TimeSpan();

                var secParts = sSeconds.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (secParts.Length == 1)
                    iSeconds = int.Parse(secParts[0] == "*" ? "0" : secParts[0]);
                else if (secParts.Length == 2)
                    xSeconds = int.Parse(secParts[1]);

                var minParts = sMinutes.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (minParts.Length == 1)
                    iMinutes = int.Parse(minParts[0] == "*" ? "0" : minParts[0]);
                else if (minParts.Length == 2)
                    xMinutes = int.Parse(minParts[1]);

                var hourParts = sHours.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (hourParts.Length == 1)
                    iHours = int.Parse(hourParts[0] == "*" ? "0" : hourParts[0]);
                else if (hourParts.Length == 2)
                    xHours = int.Parse(hourParts[1]);

                var dayParts = sDayOfWeek.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (dayParts.Length == 1)
                    iDayOfMonth = int.Parse(dayParts[0] == "*" ? "0" : dayParts[0]);
                else if (dayParts.Length == 2)
                    xDayOfMonth = int.Parse(dayParts[1]);

                var monthParts = sMonth.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (monthParts.Length == 1)
                    iMonth = int.Parse(monthParts[0] == "*" ? "0" : monthParts[0]);
                else if (monthParts.Length == 2)
                    xMonth = int.Parse(monthParts[1]);

                var weekParts = sDayOfWeek.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (weekParts.Length == 1)
                    iDayOfWeek = int.Parse(weekParts[0] == "*" ? "0" : weekParts[0]);
                else if (weekParts.Length == 2)
                    xDayOfWeek = int.Parse(weekParts[1]);

                if (xSeconds > 0)
                    timeSpan = TimeSpan.FromSeconds(xSeconds);
                if (xMinutes > 0)
                    timeSpan = TimeSpan.FromMinutes(xMinutes);
                if (xHours > 0)
                    timeSpan = TimeSpan.FromHours(xHours);
                if (xDayOfMonth > 0)
                    timeSpan = TimeSpan.FromDays(xDayOfMonth);

                var time = new ScheduleTime(timeSpan);
                times.Add(time);
            }

            return times;
        }
    }
}
