using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class LeaveLobbyHandler : MessageHandler<LeaveLobbyRequest>
{
    public override ushort MessageId => MessageIds.LeaveLobby;

    private readonly LobbyManager _lobby;

    public LeaveLobbyHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public override void HandleMessage(NetPeer peer, LeaveLobbyRequest request)
    {
        var (res, code) = _lobby.Leave(peer, request);
        MessageHelper.Send(peer, MessageId, code, res);
    }

    protected override void OnDeserializeFailed(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new LeaveLobbyResponse());
    }
}
