using LiteNetLib;
using ShareLibrary;
using ShareLibrary.Message;

namespace GameServer
{
    internal class OperationHandlerDefault
    {
        public OperationResponse OnOperationRequest(NetPeer peer, OperationRequest operationRequest)
        {
            OperationResponse operationResponse = null;

            switch(operationRequest.OperationCode)
            {
                case OperationCode.CreateGame:

                    break;
            }

            if (operationResponse == null)
                operationResponse = OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);

            return operationResponse;
        }
    }
}
