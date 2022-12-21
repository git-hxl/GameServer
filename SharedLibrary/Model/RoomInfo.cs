using MessagePack;

namespace SharedLibrary.Model
{
    [MessagePackObject]
    public class RoomInfo
    {
        [Key(0)]
        public string OwnerUID { get; set; }
        [Key(1)]
        public int RoomType { get; set; }
        [Key(2)]
        public string RoomName { get; set; }
        [Key(3)]
        public string RoomPassword { get; set; }
        [Key(4)]
        public int RoomMaxPlayers { get; set; }
    }
}
