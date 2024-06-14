using MessagePack;

[MessagePackObject]
public class CreateRoomRequest
{
    [Key(0)]
    public string RoomName { get; set; }
    [Key(1)]
    public string RoomDescription { get; set; }
    [Key(2)]
    public int RoomType { get; set; }
    [Key(3)]
    public string RoomPassword { get; set; }
    [Key(4)]
    public int RoomMaxPlayers { get; set; }
}

[MessagePackObject]
public class CreateRoomResponse
{
    [Key(0)]
    public string RoomID { get; set; }
    [Key(1)]
    public string GameServer { get; set; }
}