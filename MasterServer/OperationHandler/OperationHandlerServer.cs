using LiteNetLib;
using MasterServer.GameServer;
using MasterServer.Operations;
using Serilog;
using ShareLibrary;
using ShareLibrary.MasterGame.Request;

namespace MasterServer
{
    internal class OperationHandlerServer
    {
        public static OperationResponse OnOperationRequest(NetPeer peer, OperationRequest operationRequest)
        {
            switch (operationRequest.OperationCode)
            {
                case OperationCode.RegisterGameServer:

                    return HandleRegisterGamerServer(peer, operationRequest);
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);
        }


        public static OperationResponse HandleRegisterGamerServer(NetPeer peer, OperationRequest operationRequest)
        {
            RegisterServerRequest request = MessagePack.MessagePackSerializer.Deserialize<RegisterServerRequest>(operationRequest.Data);

            Log.Information("register gameserver: {0} request info:{1}", peer.EndPoint.ToString(), request.EndPoint);

            if(GameServerCache.Instance.ContainsKey(peer.Id))
            {
                return OperationResponse.CreateFailedResponse(operationRequest,ReturnCode.RegisterGameServerFailed);
            }
            else
            {

                GameServerPeer gameServerPeer = new GameServerPeer(peer);

                GameServerCache.Instance.RegisterGameServer(peer.Id, gameServerPeer);

                RegisterGameServerResponse response = new RegisterGameServerResponse();

                //response.EndPoint = MasterApplication.Instance.

                byte[] data = MessagePack.MessagePackSerializer.Serialize(response);
                return OperationResponse.CreateResponse(operationRequest, (byte)ReturnCode.Success, data);
            }
        }
    }
}
