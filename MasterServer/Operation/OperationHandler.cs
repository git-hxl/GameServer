using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace MasterServer.Operation
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("receive request {0}", operationCode.ToString());
            MasterPeer masterPeer = (MasterPeer)serverPeer;
            switch (operationCode)
            {
                case OperationCode.GameServerRegister:
                    masterPeer.RegisterGameServer(data);
                    break;
                case OperationCode.UpdateServerState:
                    masterPeer.UpdateGameServer(data);
                    break;
                case OperationCode.Register:
                    masterPeer.RegisterRequest(data);
                    break;
                case OperationCode.Login:
                    masterPeer.LoginRequest(data);
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
        }
    }
}
