using LiteNetLib;
using MasterServer;
using Serilog;

namespace GameServer
{
    internal class GameClientPeer
    {
        private NetPeer? peer;

        public GameClientPeer(NetPeer peer)
        {
            this.peer = peer;
        }


        public void RegisterToMaster()
        {
            RegisterServerRequest request = new RegisterServerRequest();

            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);

            OperationRequest operationRequest = new OperationRequest(OperationCode.RegisterGameServer, data, DeliveryMethod.ReliableOrdered);

            operationRequest.SendTo(peer);
        }

        public void OnRegisterToMasterResponse(OperationResponse operationResponse)
        {
            if (operationResponse.ReturnCode != ReturnCode.Success)
                return;
            RegisterServerResponse response = MessagePack.MessagePackSerializer.Deserialize<RegisterServerResponse>(operationResponse.Data);

        }
    }
}
