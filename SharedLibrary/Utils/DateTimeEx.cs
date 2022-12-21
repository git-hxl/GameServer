namespace MasterServer.Utils
{
    public static class DateTimeEx
    {
        /// <summary>
        /// 时间戳（毫秒）
        /// </summary>
        /// <returns></returns>
        public static long TimeStamp
        {
            get
            {
                DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            }
        }

        /// <summary>
        /// 转化时间戳
        /// </summary>
        /// <param name="timeStamp">时间戳（毫秒）</param>
        /// <returns></returns>
        public static DateTime ConvertToUtcDateTime(long timeStamp)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return startTime.AddMilliseconds(timeStamp);
        }

        public static DateTime ConvertToLocalDateTime(long timeStamp)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            return startTime.AddMilliseconds(timeStamp);
        }
    }
}
