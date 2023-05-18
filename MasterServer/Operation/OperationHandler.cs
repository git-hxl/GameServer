using LiteNetLib;
using MasterServer.Game;
using MasterServer.Server;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;
namespace MasterServer.Operation
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnClientRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("Client request {0}", operationCode.ToString());
            MasterPeer masterPeer = (MasterPeer)serverPeer;
            switch (operationCode)
            {
                case OperationCode.Register:
                    masterPeer.RegisterRequest(data);
                    break;
                case OperationCode.Login:
                    masterPeer.LoginRequest(data);
                    break;
                case OperationCode.GetRoomList:
                    masterPeer.GetRoomListRequest(data);
                    break;
                case OperationCode.CreateRoom:
                    masterPeer.CreateRoomRequest(data);
                    break;
            }
        }

        public override void OnClientResponse(OperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            //throw new NotImplementedException();
        }

        public override void OnServerRequest(ServerOperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("Server request {0}", operationCode.ToString());
            GamePeer gamePeer = (GamePeer)serverPeer;
            switch (operationCode)
            {
                case ServerOperationCode.RegisterToMaster:
                    gamePeer.RegisterGameServerRequest(data);
                    break;
                case ServerOperationCode.UpdateGameServerInfo:
                    gamePeer.UpdateGamerServerInfoRequest(data);
                    break;

                case ServerOperationCode.UpdateRoomList:
                    gamePeer.UpdateRoomListRequest(data);
                    break;
            }
        }

        public override void OnServerResponse(ServerOperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            //throw new NotImplementedException();
        }
    }
}
