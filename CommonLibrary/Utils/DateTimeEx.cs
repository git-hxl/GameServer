namespace CommonLibrary.Utils
{
    public static class DateTimeEx
    {
        /// <summary>
        /// 时间戳（秒）
        /// </summary>
        /// <returns></returns>
        public static long TimeStamp()
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow-startTime).TotalSeconds;
        }

        /// <summary>
        /// 转化时间戳
        /// </summary>
        /// <param name="timeStamp">时间戳（秒）</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(long timeStamp)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return startTime.AddSeconds(timeStamp);
        }
    }
}
