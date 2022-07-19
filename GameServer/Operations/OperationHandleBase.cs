using CommonLibrary.MessagePack;
using CommonLibrary.Operations;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System.Diagnostics;

namespace GameServer.Operations
{
    internal class OperationHandleBase
    {
        public void HandleRequest(HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (handleRequest.OperationCode)
            {
                case GameOperationCode.JoinGame:
                    JoinGame(handleRequest);
                    break;
                case GameOperationCode.ExitGame:
                    ExitGame(handleRequest);
                    break;
                case GameOperationCode.RPC:

                    break;

                default:
                    SendResponse(handleRequest.Peer, handleRequest.OperationCode, ReturnCode.InvalidRequest);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void SendResponse(NetPeer peer, GameOperationCode operationCode, ReturnCode returnCode, byte[]? responseData = null)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            netDataWriter.Put((byte)returnCode);
            if (responseData != null)
                netDataWriter.Put(responseData);
            //游戏数据只保证最新的数据包
            peer.Send(netDataWriter, DeliveryMethod.ReliableSequenced);
        }

        private void JoinGame(HandleRequest handleRequest)
        {
            RpcPack pack = MessagePackSerializer.Deserialize<RpcPack>(handleRequest.RequestData);
            Game game = GameApplication.Instance.GetOrCreateGame(pack.RoomID);
            if (game != null)
            {
                if (game.AddPeer(handleRequest.Peer, pack.UserID))
                {
                    foreach (var item in game.GamePeers)
                    {
                        SendResponse(item.NetPeer, handleRequest.OperationCode, ReturnCode.Success, handleRequest.RequestData);
                    }
                    return;
                }

            }
            SendResponse(handleRequest.Peer, handleRequest.OperationCode, ReturnCode.InvalidRequest);
        }

        private void ExitGame(HandleRequest handleRequest)
        {
            RpcPack pack = MessagePackSerializer.Deserialize<RpcPack>(handleRequest.RequestData);
            Game game = GameApplication.Instance.GetOrCreateGame(pack.RoomID);
            if (game != null)
            {
                GamePeer? gamePeer = GameApplication.Instance.GetGamePeer(handleRequest.Peer);
                if (gamePeer != null && game.RemovePeer(gamePeer))
                {
                    foreach (var item in game.GamePeers)
                    {
                        SendResponse(item.NetPeer, handleRequest.OperationCode, ReturnCode.Success, handleRequest.RequestData);
                    }
                    return;
                }
            }
            SendResponse(handleRequest.Peer, handleRequest.OperationCode, ReturnCode.InvalidRequest);
        }
    }
}
