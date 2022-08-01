using CommonLibrary.Core;
using GameServer.Operations.Request;
using MessagePack;
using System.Diagnostics;

namespace GameServer.Operations
{
    public class GameOperationHandle
    {
        public void HandleRequest(HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (handleRequest.OperationCode)
            {
                case OperationCode.CreateGame:
                    CreateGameRequest(handleRequest);
                    break;
                case OperationCode.RemoveGame:
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
                    HandleResponse.SendResponse(handleRequest, MsgPack.Pack(ReturnCode.InvalidRequest));
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }

        private void CreateGameRequest(HandleRequest handleRequest)
        {
            CreateGameRequest request = MessagePackSerializer.Deserialize<CreateGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.GameID);
            if (game == null)
                game = GameApplication.Instance.CreateGame(request.GameID);

            CreateGameResponse response = new CreateGameResponse();
            response.GameID = game.GameID;
            byte[] data = MessagePackSerializer.Serialize(response);
            HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
            return;
        }

        private void RemoveGameRequest(HandleRequest handleRequest)
        {
            RemoveGameRequest request = MessagePackSerializer.Deserialize<RemoveGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.GameID);
            if (game != null)
            {
                GameApplication.Instance.RemoveGame(game);
                RemoveGameResponse response = new RemoveGameResponse();
                response.GameID = game.GameID;
                byte[] data = MessagePackSerializer.Serialize(response);
                HandleResponse.SendResponse(handleRequest, MsgPack.Pack(data));
                return;
            }
        }

        private void JoinGameRequest(HandleRequest handleRequest)
        {
            JoinGameRequest request = MessagePackSerializer.Deserialize<JoinGameRequest>(handleRequest.MsgPack.Data);
            Game? game = GameApplication.Instance.GetGame(request.GameID);
            if (game != null)
            {
                GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.NetPeer);
                if (gamePeer == null)
                {
                    gamePeer = new GamePeer(handleRequest.NetPeer, request.UserID);
                    GameApplication.Instance.AddClientPeer(handleRequest.NetPeer, gamePeer);
                }
                game.AddClientPeer(gamePeer);
                gamePeer.OnJoinGame(game);

                JoinGameResponse response = new JoinGameResponse();
                response.GameID = game.GameID;
                response.UserID = gamePeer.UserID;
                byte[] data = MessagePackSerializer.Serialize(response);
                foreach (var item in game.ClientPeers)
                {
                    HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                }

                return;
            }
        }

        private void LeaveGameRequest(HandleRequest handleRequest)
        {
            LeaveGameRequest leaveGameRequest = MessagePackSerializer.Deserialize<LeaveGameRequest>(handleRequest.MsgPack.Data);
            GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (gamePeer != null)
            {
                Game? game = gamePeer.CurGame;
                if (game != null)
                {
                    LeaveGameResponse response = new LeaveGameResponse();
                    response.GameID = game.GameID;
                    response.UserID = gamePeer.UserID;
                    byte[] data = MessagePackSerializer.Serialize(response);
                    foreach (var item in game.ClientPeers)
                    {
                        HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, MsgPack.Pack(data));
                    }

                    game.RemoveClientPeer(gamePeer);
                    gamePeer.OnLeaveGame();
                    return;
                }
            }
        }

        private void Rpc(HandleRequest handleRequest)
        {
            GamePeer? gamePeer = GameApplication.Instance.GetClientPeer(handleRequest.NetPeer);
            if (gamePeer != null && gamePeer.CurGame != null)
            {
                foreach (var item in gamePeer.CurGame.ClientPeers)
                {
                    HandleResponse.SendToPeer(item.NetPeer, handleRequest.OperationCode, handleRequest.MsgPack, handleRequest.DeliveryMethod);
                }
            }
        }
    }
}
