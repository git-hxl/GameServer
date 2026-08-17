using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class JoinLobbyHandler : MessageHandler<JoinLobbyRequest>
{
    public override ushort MessageId => MessageIds.JoinLobby;

    private readonly LobbyManager _lobby;

    public JoinLobbyHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public override void HandleMessage(NetPeer peer, JoinLobbyRequest request)
    {
        var (res, code) = _lobby.Join(peer, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinLobbyResponse());
    }
}
