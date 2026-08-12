using LiteNetLib;
using MessagePack;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class JoinLobbyHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.JoinLobby;
    public bool RequireAuth => false;

    private readonly LobbyManager _lobby;

    public JoinLobbyHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<JoinLobbyRequest>(payload);
        if (req == null)
        {
            MessageHelper.Send(peer, MessageId, ReturnCode.DeserializeFailed, new JoinLobbyResponse());
            return;
        }
        var (res, code) = _lobby.Join(peer, req);
        MessageHelper.Send(peer, MessageId, code, res);
    }
}
