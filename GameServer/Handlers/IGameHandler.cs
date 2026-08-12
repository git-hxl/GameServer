using LiteNetLib;

namespace GameServer.Handlers;

public interface IGameHandler
{
    ushort MessageId { get; }
    void Handle(NetPeer peer, byte[] payload);
}
