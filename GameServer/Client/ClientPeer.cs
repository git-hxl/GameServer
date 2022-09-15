using GameServer.Request;
using GameServer.Room;
using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Utils;

namespace GameServer.Client
{
    public class ClientPeer
    {
        public NetPeer NetPeer { get; private set; }

        public PeerInfo PeerInfo { get; private set; }

        public Room.Room? Room { get; private set; }

        public ClientPeer(NetPeer netPeer)
        {
            NetPeer = netPeer;

            PeerInfo = new PeerInfo();
        }

        public void OnAuth(OperationRequest operationRequest)
        {
            AuthRequest request = MessagePackSerializer.Deserialize<AuthRequest>(operationRequest.Data);
            try
            {
                string tokenStr = SecurityUtil.AESDecrypt(request.Token, GameApplication.ServerConfig.encryptKey);
                Token? token = JsonConvert.DeserializeObject<Token>(tokenStr);
                if (token != null)
                {

                    PeerInfo.UserID = token.UserID;
                    PeerInfo.NickName = token.NickName;

                    AuthResponse response = new AuthResponse();
                    response.UserID = token.UserID;
                    response.NickName = token.NickName;

                    byte[] data = MessagePackSerializer.Serialize(response);
                    OperationResponse.CreateResponse(operationRequest, (byte)ReturnCode.Success, data).SendTo(NetPeer);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
            }
            OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AuthTokenError).SendTo(NetPeer);
        }

        public void OnCreateRoom(OperationRequest operationRequest)
        {
            if (PeerInfo.UserID == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NoAuth).SendTo(NetPeer);
                return;
            }

            if (Room != null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyInRoom).SendTo(NetPeer);
                return;
            }

            CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(operationRequest.Data);

            if (PeerInfo.UserID == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NoAuth).SendTo(NetPeer);
                return;
            }

            string roomId = Guid.NewGuid().ToString();

            RoomInfo roomInfo = new RoomInfo();

            roomInfo.RoomID = roomId;

            roomInfo.OwnerID = PeerInfo.UserID;

            roomInfo.RoomName = request.RoomName;
            roomInfo.IsVisible = request.IsVisible;
            roomInfo.Password = request.Password;
            roomInfo.MaxPeers = request.MaxPeers;
            roomInfo.RoomProperties = request.RoomProperties;

            Room.Room room = new Room.Room(roomInfo);

            RoomCache.Instance.AddRoom(roomId, room);

            Room = room;

            room.AddClientPeer(this);

            CreateRoomResponse response = new CreateRoomResponse();

            response.RoomID = roomId;

            byte[] data = MessagePackSerializer.Serialize(response);
            OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data).SendTo(NetPeer);

            return;
        }

        public void OnJoinRoom(OperationRequest operationRequest)
        {
            if (Room != null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.AlreadyInRoom).SendTo(NetPeer);
                return;
            }

            JoinRoomRequest request = MessagePackSerializer.Deserialize<JoinRoomRequest>(operationRequest.Data);

            if (PeerInfo.UserID == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NoAuth).SendTo(NetPeer);
                return;
            }

            Room.Room? room = RoomCache.Instance.GetRoom(request.RoomID);
            if (room == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.RoomNotExisted).SendTo(NetPeer);
                return;
            }
            if (room.RoomInfo.Password != request.Password)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.PasswordError).SendTo(NetPeer);
                return;
            }
            if (room.ClientPeers.Count >= room.RoomInfo.MaxPeers)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.FullRoom).SendTo(NetPeer);
                return;
            }

            Room = room;

            room.AddClientPeer(this);

            JoinRoomResponse response = new JoinRoomResponse();

            response.UserID = PeerInfo.UserID;

            response.RoomInfo = room.RoomInfo;

            byte[] data = MessagePackSerializer.Serialize(response);

            OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data);

            var netPeers = room.ClientPeers.Select((a) => a.NetPeer).ToArray();

            operationResponse.SendTo(netPeers);
        }

        public void OnLeaveRoom(OperationRequest operationRequest)
        {
            if (Room == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInRoom).SendTo(NetPeer);
                return;
            }

            LeaveRoomRequest request = MessagePackSerializer.Deserialize<LeaveRoomRequest>(operationRequest.Data);

            LeaveRoom();
        }

        public void OnGetRoomList(OperationRequest operationRequest)
        {

            GetRoomListRequest request = MessagePackSerializer.Deserialize<GetRoomListRequest>(operationRequest.Data);

            GetRoomListResponse response = new GetRoomListResponse();

            response.RoomInfos = RoomCache.Instance.Rooms.Select((a) => a.Value.RoomInfo).ToList();

            byte[] data = MessagePackSerializer.Serialize(response);

            OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data);

            operationResponse.SendTo(NetPeer);
        }

        public void OnRpc(OperationRequest operationRequest)
        {
            if (Room == null)
            {
                OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.NotInRoom).SendTo(NetPeer);
                return;
            }

            OperationResponse operationResponse = OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, operationRequest.Data);

            var netPeers = Room.ClientPeers.Select((a) => a.NetPeer).ToArray();

            operationResponse.SendTo(netPeers);
        }

        private void LeaveRoom()
        {
            if (Room != null)
            {
                Room.RemoveClientPeer(this);

                LeaveRoomResponse response = new LeaveRoomResponse();

                response.UserID = PeerInfo.UserID;

                response.RoomInfo = Room.RoomInfo;

                byte[] data = MessagePackSerializer.Serialize(response);

                OperationResponse operationResponse = new OperationResponse(OperationCode.LeaveRoom, ReturnCode.Success, data, DeliveryMethod.ReliableOrdered);

                var netPeers = Room.ClientPeers.Select((a) => a.NetPeer).ToArray();

                operationResponse.SendTo(netPeers);

                Room = null;
            }
        }

        public void OnDisconnected()
        {
            LeaveRoom();
        }
    }
}
