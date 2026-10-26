using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;
using LobbyServer.Room;
using Serilog;

namespace LobbyServer.Handlers.Client;

public class LeaveLobbyHandler : MessageHandler<LeaveLobbyRequest>
{
    public override ushort MessageId => MessageIds.LeaveLobby;

    private readonly PlayerManager _players;
    private readonly RoomManager _rooms;

    public LeaveLobbyHandler(PlayerManager playerManager, RoomManager roomManager)
    {
        _players = playerManager;
        _rooms = roomManager;
    }

    public override void HandleMessage(NetPeer peer, LeaveLobbyRequest request)
    {
        Log.Information("[LobbyManager] 离开大厅 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null || !ReferenceEquals(player.Peer, peer))
        {
            Log.Warning("[LobbyManager] 离开大厅失败：用户不在大厅中或 Peer 不匹配 userId={UserId}", request.UserId);
            MessageHelper.Send(peer, MessageId, ReturnCode.NotInLobby, new LeaveLobbyResponse { UserId = request.UserId });
            return;
        }

        _rooms.LeaveRoom(request.UserId);

        _players.Remove(player.Info.UserId);
        
        Log.Information("[LobbyManager] 大厅离开 userId={UserId} 在线人数={Count}",
            request.UserId, _players.OnlineCount);
        
        MessageHelper.Send(peer, MessageId, ReturnCode.Success, new LeaveLobbyResponse { UserId = request.UserId });
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new LeaveLobbyResponse());
    }
}