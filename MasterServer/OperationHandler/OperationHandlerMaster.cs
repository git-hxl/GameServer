
using ShareLibrary;

namespace MasterServer.OperationHandler
{
    internal class OperationHandlerMaster
    {
        public OperationResponse OnOperationRequest(MasterPeer masterPeer,OperationRequest operationRequest)
        {
            switch(operationRequest.OperationCode)
            {
                case OperationCode.Auth:
                    return masterPeer.OnHandleAuth(operationRequest);
            }

            return OperationResponse.CreateFailedResponse(operationRequest, ReturnCode.InvalidOperation);
        }
    }
}
