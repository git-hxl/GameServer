using MasterServer;
using Serilog;
using System.Diagnostics;

namespace GameServer
{
    internal class OperationHandler
    {
        public OperationResponse OnOperationRequest(GameClientPeer peer, OperationRequest operationRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            OperationResponse operationResponse = null;

            switch (operationRequest.OperationCode)
            {
                case OperationCode.CreateRoom:
                    operationResponse = peer.OnCreateRoomRequest(operationRequest);
                    break;
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            stopwatch.Stop();

            Log.Information("request operationcode: {0} returncode {1} stopWatchTime {2}", operationResponse.OperationCode, operationResponse.ReturnCode, stopwatch.ElapsedMilliseconds);

            return operationResponse;
        }

        public void OnOperationResponse(GameClientPeer peer, OperationResponse operationResponse)
        {
            Log.Information("response operationcode {0} returncode {1}", operationResponse.OperationCode, operationResponse.ReturnCode);
            switch (operationResponse.OperationCode)
            {
                case OperationCode.RegisterGameServer:
                    peer.OnRegisterToMasterResponse(operationResponse);
                    break;
            }
        }
    }
}