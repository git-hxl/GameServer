using LiteNetLib;
using MessagePack;
using SharedLib.Models;
using SharedLib.Protocol;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class ChatHandler : ILobbyHandler
{
    public ushort MessageId => MessageIds.Chat;
    public bool RequireAuth => true;

    private readonly LobbyManager _lobby;

    public ChatHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public void Handle(NetPeer peer, byte[] payload)
    {
        var req = MessagePackSerializer.Deserialize<ChatRequest>(payload);
        if (req != null)
            _lobby.Chat(peer, req);
    }
}
