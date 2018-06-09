using System;

namespace TorrentSwifter.Helpers
{
    internal static class TimeHelper
    {
        private static readonly DateTime UnixStartDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static long GetUnixTimestampFromDate(DateTime date)
        {
            if (date <= UnixStartDate)
                return 0L;

            return (long)DateTime.UtcNow.Subtract(UnixStartDate).TotalSeconds;
        }

        public static DateTime GetDateFromUnixTimestamp(long timestamp)
        {
            return UnixStartDate.AddSeconds(timestamp).ToLocalTime();
        }
    }
}
