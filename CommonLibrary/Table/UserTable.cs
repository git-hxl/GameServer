namespace CommonLibrary.Table
{
    public class UserTable
    {
        public int ID { get; }
        public string Account { get; } = "";
        public string Password { get; } = "";
        public string NickName { get; } = "";
        public DateTime LastLoginTime { get; }
    }
}
