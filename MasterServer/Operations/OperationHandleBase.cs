using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using CommonLibrary.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;

namespace MasterServer.Operations
{
    public class OperationHandleBase
    {
        public async void HandleRequest(HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            HandleResponse? handleResponse = null;
            switch (handleRequest.OperationCode)
            {
                case OperationCode.Register:
                    handleResponse = await handleRequest.ClientPeer.RegisterRequest(handleRequest.OperationCode, handleRequest.RequestData);
                    break;
                case OperationCode.Login:
                    handleResponse = await handleRequest.ClientPeer.LoginRequest(handleRequest.OperationCode,handleRequest.RequestData);
                    break;
                case OperationCode.JoinLobby:
                    handleResponse = await handleRequest.ClientPeer.JoinLobbyRequest(handleRequest.OperationCode, handleRequest.RequestData);
                    break;
                case OperationCode.LevelLobby:

                case OperationCode.CreateGame:

                case OperationCode.JoinGame:

                case OperationCode.JoinRandomGame:

                case OperationCode.GetGameList:

                case OperationCode.Disconnect:

                default:
                    break;
            }
            if (handleResponse == null)
                handleResponse = CreateFailedResponse(handleRequest.ClientPeer, handleRequest.OperationCode, ErrorMsg.InvalidRequest);
            SendResponse(handleResponse);
            Console.WriteLine("代码耗时：{0}, SendTime：{1}" ,stopwatch.ElapsedMilliseconds,DateTimeEx.TimeStamp);
        }

        public void SendResponse(HandleResponse handleResponse)
        {
            if (handleResponse == null)
                return;
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)handleResponse.OperationCode);
            if (handleResponse.ResponseData != null && handleResponse.ResponseData.Length > 0)
                netDataWriter.Put(handleResponse.ResponseData);
            handleResponse.ClientPeer.NetPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        private HandleResponse CreateFailedResponse(ClientPeer clientPeer, OperationCode operationCode, string msg)
        {
            HandleResponse handleResponse = new HandleResponse(clientPeer, operationCode);
            ResponseBasePack responseBasePack = new ResponseBasePack();
            responseBasePack.ReturnCode = ReturnCode.Failed;
            responseBasePack.DebugMsg = msg;
            handleResponse.ResponseData = MessagePack.MessagePackSerializer.Serialize(responseBasePack);
            return handleResponse;
        }

    }
}