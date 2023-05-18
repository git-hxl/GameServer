using GameServer.Master;
using GameServer.Server;
using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace GameServer.Operation
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnClientRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("Client request {0}", operationCode.ToString());
            GamePeer gamePeer = (GamePeer)serverPeer;
            switch (operationCode)
            {
                case OperationCode.JoinRoom:
                    gamePeer.JoinRoomRequest(data);
                    break;
                case OperationCode.LeaveRoom:
                    gamePeer.LeaveRoomRequest(data);
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

            MasterPeer masterPeer = (MasterPeer)serverPeer;
            switch (operationCode)
            {
                case ServerOperationCode.CreateRoom:
                    masterPeer.CreateRoomRequest(data);
                    break;
            }
        }

        public override void OnServerResponse(ServerOperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("Server response {0} return {1}", operationCode.ToString(), returnCode.ToString());

            MasterPeer masterPeer = (MasterPeer)serverPeer;
            switch (operationCode)
            {
                case ServerOperationCode.RegisterToMaster:
                    masterPeer.RegisterToMasterResponse();
                    break;
                case ServerOperationCode.UpdateGameServerInfo:
                    break;

            }
        }
    }
}
