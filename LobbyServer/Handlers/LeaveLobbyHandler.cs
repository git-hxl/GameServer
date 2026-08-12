using LiteNetLib;
using MessagePack;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class LeaveLobbyHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.LeaveLobby;
    public bool RequireAuth => true;

    private readonly LobbyManager _lobby;

    public LeaveLobbyHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<LeaveLobbyRequest>(payload);
        if (req == null)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new LeaveLobbyResponse());
            return;
        }
        var (res, code) = _lobby.Leave(peer, req);
        MessageHelper.Send(peer, MessageId, code, res);
    }
}
