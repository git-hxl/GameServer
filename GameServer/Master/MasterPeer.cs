using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary;

namespace GameServer
{
    internal class MasterPeer : BasePeer
    {
        public MasterPeer(NetPeer peer) : base(peer)
        {
            SendToMasterState();
        }


        public async void SendToMasterState()
        {
            await Task.Delay(2000);
            if (NetPeer.ConnectionState != ConnectionState.Connected)
            {
                Log.Error("Master connect Failed!!!");
                Environment.Exit(0);

                return;
            }

            _ = Task.Run(async () =>
            {
                while (NetPeer != null && NetPeer.ConnectionState == ConnectionState.Connected)
                {
                    await Task.Delay(10000);
                    SendGameServerInfo();
                }
            });

            _ = Task.Run(async () =>
            {
                while (NetPeer != null && NetPeer.ConnectionState == ConnectionState.Connected)
                {
                    await Task.Delay(5000);
                    SendRoomList();
                }
            });
        }


        private void SendGameServerInfo()
        {
            if (GameServer.Instance.SystemInfo == null)
            {
                return;
            }

            GameInfo gameInfo = new GameInfo();
            gameInfo.CPU = GameServer.Instance.SystemInfo.GetCPUPercent();
            gameInfo.Memory = GameServer.Instance.SystemInfo.GetMemoryPercent();
            gameInfo.Players = GameServer.Instance.ClientPeers.Count();
            gameInfo.IPEndPoint = NetPeer.EndPoint.ToString();
            gameInfo.Rooms = RoomManager.Instance.GetRooms().Count;
            byte[] data = MessagePackSerializer.Serialize(gameInfo);
            SendRequest(OperationCode.UpdateGameServerInfo, data, DeliveryMethod.ReliableOrdered);
        }


        private void SendRoomList()
        {
            List<RoomInfo> roomList = RoomManager.Instance.GetRooms().Select(a => a.RoomInfo).ToList();
            byte[] data = MessagePackSerializer.Serialize(roomList);
            SendRequest(OperationCode.UpdateRoomList, data, DeliveryMethod.ReliableOrdered);
        }

        public override void OnRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //base.OnRequest(operationCode, data, deliveryMethod);

            switch (operationCode)
            {
                case OperationCode.CreateRoom:

                    RoomInfo roomInfo = MessagePackSerializer.Deserialize<RoomInfo>(data);
                    bool success = RoomManager.Instance.CreateRoom(roomInfo);

                    if (success)
                    {
                        Log.Information("创建房间信息：{0}", JsonConvert.SerializeObject(roomInfo));
                        SendResponse(operationCode, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);

                        SendRoomList();
                    }

                    else
                    {
                        SendResponse(operationCode, ReturnCode.CreateRoomFailed, null, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                case OperationCode.CloseRoom:

                    CloseRoomRequest closeRoomRequest = MessagePackSerializer.Deserialize<CloseRoomRequest>(data);

                    Room? room = RoomManager.Instance.GetRoom(closeRoomRequest.RoomID);

                    if (room != null)
                    {
                        RoomManager.Instance.CloseRoom(room);

                        SendResponse(operationCode, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        SendResponse(operationCode, ReturnCode.CloseRoomFailed, null, DeliveryMethod.ReliableOrdered);
                    }
                    break;
            }

        }


        public override void OnResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {

        }
    }
}
