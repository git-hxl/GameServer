using LiteNetLib;
using MasterServer.MasterClient;
using MasterServer.MasterClient.Request;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace MasterServer.OperationHandler
{
    internal class OperationHandler
    {
        public OperationResponse OnOperationRequest(MasterClientPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse operationResponse;
            switch (operationRequest.OperationCode)
            {
                case OperationCode.Auth:
                    operationResponse = peer.OnHandleAuth(operationRequest);
                    break;
                case OperationCode.JoinLobby:
                    operationResponse = peer.OnHandleJoinLobby(operationRequest);
                    break;
                case OperationCode.LeaveLobby:
                    operationResponse = peer.OnHandleLeaveLobby(operationRequest);

                    break;
            }
            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} returncode {1} stopWatchTime {2}", operationResponse.OperationCode, operationResponse.ReturnCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }

        public OperationResponse OnOperationRequest(GameServerPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse operationResponse;
            switch (operationRequest.OperationCode)
            {
                case OperationCode.Auth:
                    operationResponse = peer.OnHandleAuth(operationRequest);
                    break;
                case OperationCode.JoinLobby:
                    operationResponse = peer.OnHandleJoinLobby(operationRequest);
                    break;
                case OperationCode.LeaveLobby:
                    operationResponse = peer.OnHandleLeaveLobby(operationRequest);

                    break;
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} returncode {1} stopWatchTime {2}", operationResponse.OperationCode, operationResponse.ReturnCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }
    }
}
}
