using CommonLibrary.Core;
using GameServer.Operations.Request;
using GameServer.Operations.Response;
using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System.Diagnostics;

namespace GameServer.Operations
{
    public class GameOperationHandle
    {
        public void HandleRequest(OperationCode operationCode, HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (operationCode)
            {
                case OperationCode.MasterCreateGame:
                    CreateGameRequest(handleRequest);
                    break;
                case OperationCode.MasterRemoveGame:
                    RemoveGameRequest(handleRequest);
                    break;

                case OperationCode.JoinGame:
                    JoinGameRequest(handleRequest);
                    break;
                case OperationCode.LeaveGame:
                    LeaveGameRequest(handleRequest);
                    break;
                case OperationCode.Rpc:
                    Rpc(handleRequest);
                    break;
                default:
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.InvalidRequest, null, handleRequest.DeliveryMethod);
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), operationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void CreateGameRequest(HandleRequest handleRequest)
        {
            CreateGameRequest request = MessagePackSerializer.Deserialize<CreateGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.RoomID);
            if (game == null)
                game = GameApplication.Instance.CreateGame(request.RoomID);
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnCreateGame, null, handleRequest.DeliveryMethod);
        }

        private void RemoveGameRequest(HandleRequest handleRequest)
        {
            RemoveGameRequest request = MessagePackSerializer.Deserialize<RemoveGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.RoomID);
            if (game != null)
                GameApplication.Instance.RemoveGame(game);
            HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnRemoveGame, null, handleRequest.DeliveryMethod);
        }

        private void JoinGameRequest(HandleRequest handleRequest)
        {
            JoinGameRequest request = MessagePackSerializer.Deserialize<JoinGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.RoomID);
            if (game != null)
            {
                GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
                if (gamePeer == null)
                {
                    gamePeer = new GamePeer(handleRequest.Peer, request.UserID);
                    GameApplication.Instance.AddClientPeer(gamePeer);
                }
                game.AddClientPeer(gamePeer);
                gamePeer.OnJoinGame(game);

                OnJoinGameResponse response = new OnJoinGameResponse();
                response.RoomID = game.GameID;
                response.UserID = gamePeer.UserID;
                byte[] data = MessagePackSerializer.Serialize(response);
                MsgPack msgPack = new MsgPack(data);
                foreach (var item in game.ClientPeers)
                {
                    HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnJoinGame, msgPack, handleRequest.DeliveryMethod);
                }
            }
        }

        private void LeaveGameRequest(HandleRequest handleRequest)
        {
            GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (gamePeer != null)
            {
                Game? game = gamePeer.CurGame;
                if (game != null)
                {
                    game.RemoveClientPeer(gamePeer);
                    gamePeer.OnLeaveGame();

                    OnLeaveGameResponse response = new OnLeaveGameResponse();
                    response.RoomID = game.GameID;
                    response.UserID = gamePeer.UserID;
                    byte[] data = MessagePackSerializer.Serialize(response);
                    MsgPack msgPack = new MsgPack(data);
                    foreach (var item in game.ClientPeers)
                    {
                        HandleResponse.SendResponse(handleRequest.Peer, ReturnCode.OnLeaveGame, msgPack, handleRequest.DeliveryMethod);
                    }
                }
            }
        }

        private void Rpc(HandleRequest handleRequest)
        {
            GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.Peer.Id);
            if (gamePeer != null && gamePeer.CurGame != null)
            {
                foreach (var item in gamePeer.CurGame.ClientPeers)
                {
                    HandleResponse.SendResponse(item.NetPeer, ReturnCode.OnRpc, handleRequest.MsgPack, handleRequest.DeliveryMethod);
                }
            }
        }
    }
}
