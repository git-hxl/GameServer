using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer.Server;
using MessagePack;
using Serilog;
using SharedLibrary.Message;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace MasterServer.Game
{
    internal class GamePeer : ServerPeer
    {
        public ServerInfo ServerInfo { get; private set; }
        public GamePeer(NetPeer peer) : base(peer) { }

        public void RegisterGameServerRequest(byte[] data)
        {
            Log.Information("RegisterGameServerRequest:{0}", NetPeer.EndPoint.ToString());
            SendResponseToServer(ServerOperationCode.RegisterToMaster, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
        }

        public void UpdateGamerServerInfoRequest(byte[] data)
        {
            ServerInfo serverInfo = MessagePackSerializer.Deserialize<ServerInfo>(data);
            this.ServerInfo = serverInfo;
        }

        public void UpdateRoomListRequest(byte[] data)
        {
            List<RoomInfo> roomInfos = MessagePackSerializer.Deserialize<List<RoomInfo>>(data);

            MasterServer.Server.MasterServer.Instance.RoomInfos = roomInfos;
        }
    }
}