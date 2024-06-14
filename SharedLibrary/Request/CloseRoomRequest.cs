using MessagePack;

[MessagePackObject]
public class CloseRoomRequest
{
    [Key(0)]
    public string RoomID { get; set; }
}
