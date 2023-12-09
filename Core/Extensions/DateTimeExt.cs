using System;

namespace Core.Extensions
{
    public static class DateTimeExt
    {
        public const string DateFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";
        public const string SimpleDateFormat = "yyyy/MM/dd";

        public static string ToSimpleFormat(this DateTime date)
        {
            return date.ToString(SimpleDateFormat);
        }

        public static string ToISOFormat(this DateTime date)
        {
            /*@
            var isRaw = date.toISOString != null;
            if (isRaw) return date.toISOString();
            */
            return date.ToString(DateFormat);
        }

        public static string ToISOFormat(this DateTimeOffset date)
        {
            return date.ToString(DateFormat);
        }

        public static string DateConverter(this string str)
        {
            var datestr = "";
            /*@
               var date = new Date(str);
               var mnth = ("0" + (date.getMonth()+1)).slice(-2);
               var day  = ("0" + date.getDate()).slice(-2);
               var hours1  = ("0" + date.getHours()).slice(-2);
               var minutes = ("0" + date.getMinutes()).slice(-2);
               var seconds  = ("0" + date.getSeconds()).slice(-2);
               var year = date.getFullYear();
               datestr= `${year}/${mnth}/${day} ${hours1}:${minutes}:${seconds}`
             */
            return datestr;
        }

        public static (int, int, int) ToDayHourMinute(this TimeSpan span)
        {
            var days = (int)span.TotalDays;
            var minute = (int)span.TotalMinutes - days * 24 * 60;
            var leftMinute = minute % 60;
            var hour = minute / 60;
            return (days, hour, leftMinute);
        }

        public static string DateString(this DateTimeOffset yourDate, DateTime? compareDate = null)
        {
            if (compareDate == null)
            {
                compareDate = DateTime.Now;
            }
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(compareDate.Value.Ticks - yourDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }

            if (delta < 2 * MINUTE)
            {
                return "a minute ago";
            }

            if (delta < 45 * MINUTE)
            {
                return ts.Minutes + " minutes ago";
            }

            if (delta < 90 * MINUTE)
            {
                return "an hour ago";
            }

            if (delta < 24 * HOUR)
            {
                return ts.Hours + " hours ago";
            }

            if (delta < 48 * HOUR)
            {
                return "yesterday";
            }

            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        public static string ToUserFormat(this DateTime date)
        {
            return date.ToString("yyyy/MM/dd HH:mm");
        }

        public static DateTime? TryParseDateTime(this string date)
        {
            if (date.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsed = DateTime.TryParse(date, out var res);
            if (!parsed)
            {
                return null;
            }

            return res;
        }

        public static double GetBusinessDays(DateTime start)
        {
            var end = DateTime.Now.Date;
            if (start.DayOfWeek == DayOfWeek.Saturday)
            {
                start = start.AddDays(2);
            }
            else if (start.DayOfWeek == DayOfWeek.Sunday)
            {
                start = start.AddDays(1);
            }

            if (end.DayOfWeek == DayOfWeek.Saturday)
            {
                end = end.AddDays(-1);
            }
            else if (end.DayOfWeek == DayOfWeek.Sunday)
            {
                end = end.AddDays(-2);
            }

            int diff = (int)end.Subtract(start.Date).TotalDays;

            int result = diff / 7 * 5 + diff % 7;

            if (end.DayOfWeek < start.DayOfWeek)
            {
                return result - 2;
            }
            else
            {
                return result;
            }
        }

        public static int GetDays(DateTime start, DateTime end)
        {
            int result = (int)end.Subtract(start).TotalDays;
            return result;
        }

        public static int TotalMonth(DateTime start, DateTime end)
        {
            return end.Year * 12 + end.Month - start.Year * 12 - start.Month;
        }

        public static DateTime FirstDayOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static DateTime LastDayOfMonth(this DateTime dt)
        {
            return dt.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }

        public static DateTime FirstDayOfNextMonth(this DateTime dt)
        {
            return dt.FirstDayOfMonth().AddMonths(1);
        }
    }
}
