using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Lobby;
using LobbyServer.Player;
using Serilog;

namespace LobbyServer.Handlers.Client;

public class JoinLobbyHandler : MessageHandler<JoinLobbyRequest>
{
    public override ushort MessageId => MessageIds.JoinLobby;

    private readonly PlayerManager _players;

    public JoinLobbyHandler(PlayerManager players)
    {
        _players = players;
    }

    public override void HandleMessage(NetPeer peer, JoinLobbyRequest request)
    {
        var userId = request.Player.UserId;
        Log.Information("[LobbyManager] 加入大厅 userId={UserId}", userId);

        var player = _players.Get(userId);
        if (player != null)
        {
            player.SetPeer(peer);

            Log.Information("[LobbyManager] 玩家恢复会话 userId={UserId} roomId={RoomId}",
                userId, player.CurrentRoomId);

            MessageHelper.Send(peer, MessageId, ReturnCode.Success, new JoinLobbyResponse { Player = request.Player });
        }

        _players.Add(peer, request.Player);

        Log.Information("[LobbyManager] 大厅加入 userId={UserId} nickname={Nickname} 在线人数={Count}",
            userId, request.Player.Nickname, _players.OnlineCount);

        MessageHelper.Send(peer, MessageId, ReturnCode.Success, new JoinLobbyResponse { Player = request.Player });
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinLobbyResponse());
    }
}