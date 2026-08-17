using LiteNetLib;

namespace SharedLib.Handlers;

public interface IHandler
{
    ushort MessageId { get; }
    void Handle(NetPeer peer, byte[] payload);
}
