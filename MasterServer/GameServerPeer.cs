using LiteNetLib;
using MessagePack;

namespace MasterServer
{
    internal class GameServerPeer
    {
        private NetPeer peer;

        public GameServerPeer(NetPeer peer)
        {
            this.peer = peer;
        }

        public OperationResponse OnRegisterGameServer(OperationRequest operationRequest)
        {
            RegisterServerRequest request = MessagePackSerializer.Deserialize<RegisterServerRequest>(operationRequest.Data);
            //todo:相关验证

            GameServerCache.Instance.RegisterGameServer(peer.Id, this);

            RegisterServerResponse response = new RegisterServerResponse();
            byte[] data = MessagePackSerializer.Serialize(response);

            return OperationResponse.CreateResponse(operationRequest, ReturnCode.Success, data);
        }
    }
}