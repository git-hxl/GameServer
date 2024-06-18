using LiteNetLib;
using MessagePack;
using Serilog;

using SharedLibrary;

namespace GameServer
{
    public class OperationHandler : OperationHandlerBase
    {
        public override void OnRequest(BasePeer basePeer, OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //Log.Information("操作代码 {0}", operationCode);
            switch (operationCode)
            {
                case OperationCode.JoinRoom:
                    OnJoinRoom(basePeer, data);
                    break;
                case OperationCode.LeaveRoom:
                    OnLeaveRoom(basePeer, data);
                    break;
                case OperationCode.SyncEvent:
                    OnSyncEvent(basePeer, data, deliveryMethod);
                    break;
                default:
                    Log.Error("未知的操作代码 {0}", operationCode);
                    break;
            }
            HotManager.Instance.GetHandler("HotOperationHandler").OnRequest(basePeer, operationCode, data, deliveryMethod);
        }

        public override void OnResponse(BasePeer basePeer, OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            Log.Information("操作代码 {0} 返回代码 {1}", operationCode, returnCode);
            HotManager.Instance.GetHandler("HotOperationHandler").OnResponse(basePeer, operationCode, returnCode, data, deliveryMethod);
        }

        private void OnJoinRoom(BasePeer basePeer, byte[] data)
        {
            if (basePeer == null)
            {
                return;
            }

            ClientPeer? clientPeer = basePeer as ClientPeer;

            if (clientPeer == null)
            {
                return;
            }

            JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(data);

            clientPeer.UserInfo = request.UserInfo;

            IRoom? room = RoomManager.Instance.GetRoom(request.RoomID);

            if (room == null)
            {
                basePeer.SendResponse(OperationCode.JoinRoom, ReturnCode.JoinRoomFailedByIsNotExistedRoomID, null, DeliveryMethod.ReliableOrdered);
                return;
            }
            else
            {
                if (room.OnPlayerEnter(clientPeer))
                {
                    JoinRoomResponse response = new JoinRoomResponse();
                    response.RommInfo = room.RoomInfo;
                    response.UserInfos = room.ClientPeers.Select((a) => a.UserInfo).ToList();

                    data = MessagePackSerializer.Serialize(response);

                    basePeer.SendResponse(OperationCode.JoinRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    basePeer.SendResponse(OperationCode.JoinRoom, ReturnCode.JoinRoomFailed, null, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void OnLeaveRoom(BasePeer basePeer, byte[] data)
        {
            if (basePeer == null)
            {
                return;
            }

            ClientPeer? clientPeer = basePeer as ClientPeer;

            if (clientPeer == null)
            {
                return;
            }

            IRoom? room = RoomManager.Instance.GetRoomByClientPeer(basePeer);

            if (room == null)
            {
                basePeer.SendResponse(OperationCode.LeaveRoom, ReturnCode.LeaveRoomFailed, null, DeliveryMethod.ReliableOrdered);
                return;
            }
            else
            {
                room.OnPlayerLeave(clientPeer);
                basePeer.SendResponse(OperationCode.LeaveRoom, ReturnCode.Success, null, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnSyncEvent(BasePeer basePeer, byte[] data, DeliveryMethod deliveryMethod)
        {
            IRoom? room = RoomManager.Instance.GetRoomByClientPeer(basePeer);

            if (room != null)
            {
                foreach (var item in room.ClientPeers)
                {
                    item.SendRequest(OperationCode.SyncEvent, data, deliveryMethod);
                }
            }
        }
    }
}
