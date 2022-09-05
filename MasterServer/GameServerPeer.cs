using LiteNetLib;
using MessagePack;

namespace MasterServer
{
    internal class GameServerPeer : IComparable<GameServerPeer>
    {
        public NetPeer Peer { get; private set; }

        public int RoomCount { get; private set; }
        public int PlayerCount { get; private set; }
        public int CPUPercent { get; private set; }
        public int MemoryPercent { get; private set; }


        public GameServerPeer(NetPeer peer)
        {
            this.Peer = peer;
        }

        public OperationResponse OnRegisterGameServer(OperationRequest operationRequest)
        {
            RegisterServerRequest request = MessagePackSerializer.Deserialize<RegisterServerRequest>(operationRequest.Data);
            //todo:相关验证

            GameServerCache.Instance.RegisterGameServer(Peer.Id, this);

            RegisterServerResponse response = new RegisterServerResponse();
            byte[] data = MessagePackSerializer.Serialize(response);

            return OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data);
        }

        public OperationResponse OnUpdateGameServer(OperationRequest operationRequest)
        {
            UpdateServerInfoRequest request = MessagePackSerializer.Deserialize<UpdateServerInfoRequest>(operationRequest.Data);

            RoomCount = request.RoomCount;
            PlayerCount = request.PlayerCount;
            CPUPercent = request.CPUPercent;
            MemoryPercent = request.MemoryPercent;

            return OperationResponse.CreateNoneResponse();
        }

        public void OnCreateRoomResponse(OperationResponse operationResponse)
        {
            CreateRoomResponse response = MessagePackSerializer.Deserialize<CreateRoomResponse>(operationResponse.Data);
            MasterClientPeer masterClientPeer = PlayerCache.Instance.GetPlayer(response.UserID);
            if (operationResponse.ReturnCode == ReturnCode.Success)
            {
                CreateRoomRequest request = MessagePackSerializer.Deserialize<CreateRoomRequest>(response.RequestData);

                RoomState roomState = RoomCache.Instance.AddRoom(response.RoomID, request);
                if (roomState != null)
                {
                    roomState.AddPlayer(response.UserID);
                    masterClientPeer.Room = roomState;
                }
            }

            if (masterClientPeer != null)
            {
                operationResponse.SendTo(masterClientPeer.Peer);
            }
        }

        public int CompareTo(GameServerPeer? other)
        {
            if (other == null) return 1;
            int result = PlayerCount.CompareTo(other.PlayerCount);
            if (result == 0)
            {
                result = CPUPercent.CompareTo(other.CPUPercent);
                if (result == 0)
                {
                    MemoryPercent.CompareTo(other.MemoryPercent);
                }
            }
            return result;
        }
    }
}