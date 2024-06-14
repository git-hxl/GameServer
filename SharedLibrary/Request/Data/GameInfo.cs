using MessagePack;
[MessagePackObject]
public class GameInfo
{
    [Key(0)]
    public int Players { get; set; }
    [Key(1)]
    public int CPU { get; set; }
    [Key(2)]
    public int Memory { get; set; }

    [Key(3)]
    public string IPEndPoint { get; set; }
    [Key(4)]
    public int Rooms { get; set; }
}