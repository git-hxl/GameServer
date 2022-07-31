using MessagePack;

namespace MasterServer.Operations.Response
{
    [MessagePackObject]
    public class OnRoomListResponse
    {
        [Key(0)]
        public LobbyProperty LobbyProperty;
        [Key(1)]
        public List<RoomProperty> Rooms;
    }
}