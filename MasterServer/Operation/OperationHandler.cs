using LiteNetLib;

namespace MasterServer.Operation
{
    internal class OperationHandler
    {
        public void OnOperationRequest(OperationCode operationCode, MasterPeer masterPeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            switch (operationCode)
            {
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
    }
}
