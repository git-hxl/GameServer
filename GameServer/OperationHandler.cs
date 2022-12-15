using GameServer.Client;
using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using System.Diagnostics;


namespace GameServer
{
    internal class OperationHandler : OperationHandlerBase
    {
        public override void OnOperationRequest(NetPeer peer, OperationRequest operationRequest)
        {
            ClientPeer? clientPeer = PeerManager.Instance.GetClientPeer(peer.Id);
            if (clientPeer == null) return ;

            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (operationRequest.OperationCode)
            {
                case OperationCode2.Auth:
                    clientPeer.OnAuth(operationRequest);
                    break;
                case OperationCode2.CreateRoom:
                    clientPeer.OnCreateRoom(operationRequest);
                    break;
                case OperationCode2.JoinRoom:
                    clientPeer.OnJoinRoom(operationRequest);
                    break;
                case OperationCode2.LeaveRoom:
                    clientPeer.OnLeaveRoom(operationRequest);
                    break;
                case OperationCode2.GetRoomList:
                    clientPeer.OnGetRoomList(operationRequest);
                    break;
                case OperationCode2.RPC:
                    clientPeer.OnRpc(operationRequest);
                    break;
            }
            stopwatch.Stop();
            Log.Information("client request operationcode: {0} stopWatchTime {1}", operationRequest.OperationCode, stopwatch.ElapsedMilliseconds);
        }

        public override void OnOperationResponse(NetPeer peer, OperationResponse operationResponse)
        {

        }
    }
}
