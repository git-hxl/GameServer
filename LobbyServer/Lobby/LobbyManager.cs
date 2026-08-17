using LiteNetLib;
using LobbyServer.Player;
using LobbyServer.Room;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace LobbyServer.Lobby;

public class LobbyManager
{
    private readonly NetManager _netManager;
    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public int UserCount => _players.Count;

    public LobbyManager(NetManager netManager, PlayerManager players, RoomManager rooms)
    {
        _netManager = netManager;
        _players = players;
        _rooms = rooms;
    }

    public (JoinLobbyResponse Response, ReturnCode Code) Join(NetPeer peer, JoinLobbyRequest request)
    {
        var userId = request.Player.UserId;
        Log.Information("[LobbyManager] 加入大厅 userId={UserId}", userId);

        var existing = _players.Get(userId);
        if (existing != null)
        {
            Log.Warning("[LobbyManager] 替换已有连接 userId={UserId}", userId);

            if (existing.Peer != peer)
            {
                _rooms.RemovePlayer(existing.Peer);
                _netManager.DisconnectPeer(existing.Peer);
            }
        }

        _players.Add(peer, request.Player);

        Log.Information("[LobbyManager] 大厅加入 userId={UserId} nickname={Nickname} 在线人数={Count}",
            userId, request.Player.Nickname, _players.Count);

        return (new JoinLobbyResponse { Player = request.Player }, ReturnCode.Success);
    }

    public (LeaveLobbyResponse Response, ReturnCode Code) Leave(NetPeer peer, LeaveLobbyRequest request)
    {
        Log.Information("[LobbyManager] 离开大厅 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null)
        {
            Log.Warning("[LobbyManager] 离开大厅失败：用户不在大厅中 userId={UserId}", request.UserId);
            return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.NotInLobby);
        }

        if (player.Peer != peer)
        {
            Log.Warning("[LobbyManager] 离开大厅失败：Peer不匹配 userId={UserId}", request.UserId);
            return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.NotInLobby);
        }

        _players.Remove(request.UserId);

        Log.Information("[LobbyManager] 大厅离开 userId={UserId} 在线人数={Count}",
            request.UserId, _players.Count);

        return (new LeaveLobbyResponse { UserId = request.UserId }, ReturnCode.Success);
    }

    public void Chat(NetPeer peer, ChatRequest request)
    {
        Log.Information("[LobbyManager] 聊天消息 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null || player.Peer != peer)
        {
            Log.Warning("[LobbyManager] 聊天被拒绝：用户不在大厅中 userId={UserId}", request.UserId);
            return;
        }

        Log.Information("[LobbyManager] 聊天 userId={UserId} nickname={Nickname} content={Content}",
            request.UserId, request.Nickname, request.Content);

        Broadcast(MessageIds.ChatNotify, ReturnCode.Success, new ChatNotify
        {
            UserId = request.UserId,
            Nickname = request.Nickname,
            Content = request.Content
        });
    }

    private void Broadcast(ushort messageId, ReturnCode code, object data)
    {
        var frame = MessageHelper.SerializeFrame(messageId, code, data);
        foreach (var player in _players.All)
        {
            MessageHelper.Send(player.Peer, frame, DeliveryMethod.ReliableOrdered);
        }
    }
}
