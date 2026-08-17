using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Lobby;

namespace LobbyServer.Handlers;

public class ChatHandler : MessageHandler<ChatRequest>
{
    public override ushort MessageId => MessageIds.Chat;

    private readonly LobbyManager _lobby;

    public ChatHandler(LobbyManager lobby)
    {
        _lobby = lobby;
    }

    public override void HandleMessage(NetPeer peer, ChatRequest request)
    {
        _lobby.Chat(peer, request);
    }
}
