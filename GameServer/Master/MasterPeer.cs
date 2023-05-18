using GameServer.Server;
using LiteNetLib;
using MessagePack;
using Serilog;
using SharedLibrary.Message;
using SharedLibrary.Operation;
using SharedLibrary.Room;
using SharedLibrary.Server;
using SharedLibrary.Utils;

namespace GameServer.Master
{
    internal class MasterPeer : ServerPeer
    {
        public bool IsRegister { get; private set; }

        public SystemInfo SystemInfo { get; private set; }

        public MasterPeer(NetPeer peer) : base(peer)
        {
            SystemInfo = new SystemInfo();
        }

        public void RegisterToMaster()
        {
            SendRequestToServer(ServerOperationCode.RegisterToMaster, null, DeliveryMethod.ReliableOrdered);
        }

        public void RegisterToMasterResponse()
        {
            if (IsRegister == false)
            {
                Task.Run(async () =>
                {
                    while (NetPeer != null && NetPeer.ConnectionState == ConnectionState.Connected)
                    {
                        await Task.Delay(1000);
                        SendGameServerInfo();

                        SendRoomList();
                    }
                    IsRegister = false;
                });
                IsRegister = true;
            }
        }

        public void CreateRoomRequest(byte[] data)
        {
            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(data);

            if (request.RoomInfo != null)
            {
                if (!string.IsNullOrEmpty(request.RoomInfo.RoomID))
                {
                    Room room = new Room(request.RoomInfo);
                    GameServer.Server.GameServer.Instance.Rooms.Add(room);
                    Log.Information($"CreateRoom Success {0}" + room.RoomInfo.RoomID);
                }
            }
        }



        private void SendGameServerInfo()
        {
            ServerInfo serverInfo = new ServerInfo();
            serverInfo.CPU = SystemInfo.GetCPUPercent();
            serverInfo.Memory = SystemInfo.GetCPUPercent();
            serverInfo.Players = GameServer.Server.GameServer.Instance.GamePeers.Count();
            byte[] data = MessagePackSerializer.Serialize(serverInfo);
            SendRequestToServer(ServerOperationCode.UpdateGameServerInfo, data, DeliveryMethod.ReliableOrdered);
        }

        private void SendRoomList()
        {
            List<RoomInfo> roomList = GameServer.Server.GameServer.Instance.Rooms.Select(a => a.RoomInfo).ToList();
            byte[] data = MessagePackSerializer.Serialize(roomList);
            SendRequestToServer(ServerOperationCode.UpdateRoomList, data, DeliveryMethod.ReliableOrdered);
        }
    }
}
