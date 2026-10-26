using LiteNetLib;
using SharedLib.Protocol;
using SharedLib.Utils;
using SharedLib.Handlers;
using LobbyServer.Player;

namespace LobbyServer.Handlers.Client;

public abstract class LobbyStateHandler<TRequest> : MessageHandler<TRequest> where TRequest : class
{
    protected readonly PlayerManager Players;

    protected LobbyStateHandler(PlayerManager players)
    {
        Players = players;
    }

    protected override bool TryAuthorize(NetPeer peer)
    {
        return Players.GetByPeer(peer) != null;
    }

    protected override void OnUnauthorized(NetPeer peer)
    {
        MessageHelper.Send(peer, MessageId, ReturnCode.NotInLobby);
    }
}
