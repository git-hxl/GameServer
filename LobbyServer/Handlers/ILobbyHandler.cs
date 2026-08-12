using LiteNetLib;

namespace LobbyServer.Handlers;

public interface ILobbyHandler
{
    ushort MessageId { get; }
    bool RequireAuth { get; }
    void Handle(NetPeer peer, byte[] payload);
}
