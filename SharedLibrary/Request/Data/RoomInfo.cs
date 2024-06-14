
using MessagePack;

[MessagePackObject]
public class RoomInfo
{
    [Key(0)]
    public string RoomID { get; set; }
    [Key(1)]
    public string RoomName { get; set; }
    [Key(2)]
    public string RoomDescription { get; set; }
    [Key(3)]
    public int RoomType { get; set; }
    [Key(4)]
    public string RoomPassword { get; set; }
    [Key(5)]
    public int RoomMaxPlayers { get; set; }
}