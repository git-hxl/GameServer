using MessagePack;
[MessagePackObject]
public class RegisterRequest
{
    [Key(0)]
    public string Account { get; set; }
    [Key(1)]
    public string Password { get; set; }
}

[MessagePackObject]
public class RegisterResponse
{
    [Key(0)]
    public int UID { get; set; }
}