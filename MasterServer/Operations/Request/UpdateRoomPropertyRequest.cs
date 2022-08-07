using MessagePack;
using System.Collections;

namespace MasterServer.Operations.Request
{
    [MessagePackObject]
    public class UpdateRoomPropertyRequest
    {
        [Key(0)]
        public Hashtable CustomProperties;
    }

    [MessagePackObject]
    public class UpdateRoomPropertyResponse
    {
        [Key(0)]
        public string RoomID;
        [Key(1)]
        public Hashtable CustomProperties;
    }
}