using LiteNetLib;
using LobbyServer.Player;
using LobbyServer.Room;
using Serilog;
using SharedLib.Handlers;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace LobbyServer.Handlers.Client;

public class JoinLobbyHandler : MessageHandler<JoinLobbyRequest>
{
    public override ushort MessageId => MessageIds.JoinLobby;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public JoinLobbyHandler(PlayerManager players, RoomManager rooms)
    {
        _players = players;
        _rooms = rooms;
    }

    /// <summary>
    /// 加入大厅；已存在的离线玩家走重连绑定，不重复创建。
    /// </summary>
    /// <param name="peer">客户端连接。</param>
    /// <param name="request">加入大厅请求。</param>
    public override void HandleMessage(NetPeer peer, JoinLobbyRequest request)
    {
        if (request.Player == null || request.Player.UserId <= 0)
        {
            Log.Warning("[LobbyManager] 加入大厅失败：无效玩家信息");
            MessageHelper.Send(peer, MessageId, ReturnCode.Error, new JoinLobbyResponse());
            return;
        }

        var userId = request.Player.UserId;
        Log.Information("[LobbyManager] 加入大厅 userId={UserId}", userId);

        var player = _players.Get(userId);
        if (player != null)
        {
            var oldPeer = player.Peer;
            if (oldPeer != null && oldPeer != peer)
                oldPeer.NetManager.DisconnectPeer(oldPeer);

            _players.BindOnline(player, peer, request.Player);
            _rooms.RefreshPlayerPending(userId);

            Log.Information("[LobbyManager] 玩家恢复会话 userId={UserId} roomId={RoomId}",
                userId, player.CurrentRoomId);

            MessageHelper.Send(peer, MessageId, ReturnCode.Success, new JoinLobbyResponse { Player = request.Player });
            return;
        }

        _players.Add(peer, request.Player);
        _rooms.RefreshPlayerPending(userId);

        Log.Information("[LobbyManager] 大厅加入 userId={UserId} nickname={Nickname} 在线人数={Count}",
            userId, request.Player.Nickname, _players.OnlineCount);

        MessageHelper.Send(peer, MessageId, ReturnCode.Success, new JoinLobbyResponse { Player = request.Player });
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinLobbyResponse());
    }
}
