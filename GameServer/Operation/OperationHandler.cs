using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace GameServer.Operation
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("receive request {0}", operationCode.ToString());
            GamePeer gamePeer = (GamePeer)serverPeer;
            switch (operationCode)
            {
                case OperationCode.GameServerRegister:

                    break;
                case OperationCode.Register:

                    break;
                case OperationCode.Login:

                    break;
                case OperationCode.Logout:
                    break;
                case OperationCode.JoinRoom:
                    break;
                case OperationCode.LeaveRoom:
                    break;
                case OperationCode.CreateRoom:
                    break;
                case OperationCode.GetRoomList:
                    break;
            }
        }

        public override void OnResponse(OperationCode operationCode, ReturnCode returnCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("receive response {0} returncode {1}", operationCode.ToString(), returnCode.ToString());
            GamePeer gamePeer = (GamePeer)serverPeer;
            switch (operationCode)
            {
                case OperationCode.GameServerRegister:
                    gamePeer.OnGameServerRegisterResponse(data);
                    break;
                case OperationCode.Register:

                    break;
                case OperationCode.Login:

                    break;
                case OperationCode.Logout:
                    break;
                case OperationCode.JoinRoom:
                    break;
                case OperationCode.LeaveRoom:
                    break;
                case OperationCode.CreateRoom:
                    break;
                case OperationCode.GetRoomList:
                    break;
            }
        }
    }
}
