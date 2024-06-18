using MessagePack;
[MessagePackObject(true)]
public class UserInfo
{
    public string UID { get; set; }
    public string NickName { get; set; }
    public bool IsRobot { get; set; }
}