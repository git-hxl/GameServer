using LiteNetLib;
using SharedLibrary.Operation;
using SharedLibrary.Server;

namespace GameServer.Operation
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnRequest(OperationCode operationCode, ServerPeer serverPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
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

        }
    }
}
