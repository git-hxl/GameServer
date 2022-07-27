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
                    Rpc(handleRequest);
                    break;
                default:
                    SendResponse(handleRequest.GamePeer, handleRequest.OperationCode, ReturnCode.InvalidRequest, DeliveryMethod.ReliableOrdered);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void SendResponse(GamePeer peer, GameOperationCode operationCode, ReturnCode returnCode, DeliveryMethod deliveryMethod, byte[]? responseData = null)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            netDataWriter.Put((byte)returnCode);
            if (responseData != null)
                netDataWriter.Put(responseData);
            //游戏数据只保证最新的数据包
            peer.NetPeer.Send(netDataWriter, deliveryMethod);
            GameApplication.Instance.Server.TriggerUpdate();
        }

        private void JoinGame(HandleRequest handleRequest)
        {
            RpcPack pack = MessagePackSerializer.Deserialize<RpcPack>(handleRequest.RequestData);
            Game game = GameApplication.Instance.GetOrCreateGame(pack.RoomID);
            if (game != null)
            {
                if (handleRequest.GamePeer.JoinGame(game, pack.UserID))
                {
                    foreach (var item in game.GamePeers)
                    {
                        SendResponse(item, handleRequest.OperationCode, ReturnCode.Success, handleRequest.DeliveryMethod, handleRequest.RequestData);
                    }
                    return;
                }
            }
            SendResponse(handleRequest.GamePeer, handleRequest.OperationCode, ReturnCode.InvalidRequest, handleRequest.DeliveryMethod);
        }

        private void ExitGame(HandleRequest handleRequest)
        {
            RpcPack pack = MessagePackSerializer.Deserialize<RpcPack>(handleRequest.RequestData);
            Game game = GameApplication.Instance.GetOrCreateGame(pack.RoomID);
            if (game != null)
            {
                if (handleRequest.GamePeer.ExitGame())
                {
                    foreach (var item in game.GamePeers)
                    {
                        SendResponse(item, handleRequest.OperationCode, ReturnCode.Success, handleRequest.DeliveryMethod, handleRequest.RequestData);
                    }
                    return;
                }
            }
            SendResponse(handleRequest.GamePeer, handleRequest.OperationCode, ReturnCode.InvalidRequest, handleRequest.DeliveryMethod);
        }

        private void Rpc(HandleRequest handleRequest)
        {
            RpcPack pack = MessagePackSerializer.Deserialize<RpcPack>(handleRequest.RequestData);
            Game game = GameApplication.Instance.GetOrCreateGame(pack.RoomID);
            if (game != null)
            {
                foreach (var item in game.GamePeers)
                {
                    SendResponse(item, handleRequest.OperationCode, ReturnCode.Success, handleRequest.DeliveryMethod, handleRequest.RequestData);
                }
            }
        }
    }
}
