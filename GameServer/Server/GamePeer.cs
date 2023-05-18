using LiteNetLib;
using MessagePack;
using SharedLibrary.Message;
using SharedLibrary.Operation;
using SharedLibrary.Room;
using SharedLibrary.Server;

namespace GameServer.Server
{
    internal class GamePeer : ServerPeer
    {
        public GamePeer(NetPeer peer) : base(peer) { }

        public void JoinRoomRequest(byte[] data)
        {
            JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(data);

            Room? room = GameServer.Instance.Rooms.FirstOrDefault((a) => a.RoomInfo.RoomID == request.RoomID);

            if (room != null)
            {
                if (room.RoomInfo.RoomPassword == request.RoomPassword)
                {
                    if (room.AddPlayer(this))
                    {
                        this.UserInfo = request.UserInfo;

                        JoinRoomResponse response = new JoinRoomResponse();
                        response.UserInfos = room.ServerPeers.Select(p => p.UserInfo).ToList();
                        data = MessagePackSerializer.Serialize(response);

                        foreach (var item in room.ServerPeers)
                        {
                            GamePeer gamePeer = item as GamePeer;
                            gamePeer?.SendResponseToClient(OperationCode.JoinRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
                        }
                    }
                    else
                    {
                        SendResponseToClient(OperationCode.JoinRoom, ReturnCode.JoinRoomFailed, null, DeliveryMethod.ReliableOrdered);
                    }
                }
                else
                {
                    SendResponseToClient(OperationCode.JoinRoom, ReturnCode.JoinRoomFailedByPassword, null, DeliveryMethod.ReliableOrdered);
                }

            }
            else
            {
                SendResponseToClient(OperationCode.JoinRoom, ReturnCode.JoinRoomFailedByIsNotExistedRoomID, null, DeliveryMethod.ReliableOrdered);
            }
        }

        public void LeaveRoomRequest(byte[] data)
        {
            LeaveRoomRequest request = MessagePackSerializer.Deserialize<LeaveRoomRequest>(data);
            Room? room = GameServer.Instance.Rooms.FirstOrDefault((a) => a.RoomInfo.RoomID == request.RoomID);
            if (room != null)
            {
                if (room.RemovePlayer(this))
                {
                    LeaveRoomResponse response = new LeaveRoomResponse();
                    response.RoomID = request.RoomID;
                    response.UserInfo = this.UserInfo;

                    data = MessagePackSerializer.Serialize(response);

                    foreach (var item in room.ServerPeers)
                    {
                        GamePeer gamePeer = item as GamePeer;
                        gamePeer?.SendResponseToClient(OperationCode.LeaveRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
                    }

                    SendResponseToClient(OperationCode.LeaveRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);

                    if (this.UserInfo.UID == room.RoomInfo.OwnerID)
                    {
                        if (room.ServerPeers.Count > 0)
                        {
                            //更换房主
                            room.RoomInfo.OwnerID = room.ServerPeers.FirstOrDefault().UserInfo.UID;
                            data = MessagePackSerializer.Serialize(room.RoomInfo);
                            foreach (var item in room.ServerPeers)
                            {
                                GamePeer gamePeer = item as GamePeer;
                                gamePeer?.SendResponseToClient(OperationCode.ReplaceRoomOwner, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);
                            }
                            
                        }
                    }
                }
            }
            else
            {
                SendResponseToClient(OperationCode.LeaveRoom, ReturnCode.LeaveRoomFailed, null, DeliveryMethod.ReliableOrdered);
            }

        }
    }
}