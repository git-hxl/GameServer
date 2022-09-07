using GameServer.Request;
using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;
namespace TestClient
{
    public class TestServer : Server
    {
        public TestServer(ServerConfig serverConfig) : base(serverConfig)
        {

        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Log.Information("peer receive:{0} threadid:{1}", peer.EndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);
            byte operationType = reader.GetByte();
            if (operationType == 1)
            {
                OperationCode operationCode = (OperationCode)reader.GetByte();
                ReturnCode returnCode = (ReturnCode)reader.GetByte();

                OperationResponse operationResponse = new OperationResponse(operationCode, returnCode, reader.GetRemainingBytes(), deliveryMethod);

                Log.Information("peer receive operationcode {0} returncode {1}  ", operationCode.ToString(), returnCode.ToString());

                if (returnCode == ReturnCode.Success)
                {
                    switch (operationCode)
                    {
                        case OperationCode.CreateRoom:

                            CreateRoomResponse response = MessagePackSerializer.Deserialize<CreateRoomResponse>(operationResponse.Data);

                            Log.Information("roomid {0} ", response.RoomID);

                            break;

                        case OperationCode.JoinRoom:

                            JoinRoomResponse response1 = MessagePackSerializer.Deserialize<JoinRoomResponse>(operationResponse.Data);

                            Log.Information("userid {0} joined room {1} players {2} ",response1.UserID,JsonConvert.SerializeObject(response1.RoomInfo), JsonConvert.SerializeObject(response1.Players));

                            break;
                    }
                }

            }

        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            Log.Information("peer connected:{0} threadid:{1}", peer.EndPoint.ToString(), Thread.CurrentThread.ManagedThreadId);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("peer disconnected:{0} info:{1} threadid:{2}", peer.EndPoint.ToString(), disconnectInfo.Reason.ToString(), Thread.CurrentThread.ManagedThreadId);
        }
    }
}
