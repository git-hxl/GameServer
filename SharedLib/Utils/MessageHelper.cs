using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using SharedLib.Protocol;

namespace SharedLib.Utils;

public static class MessageHelper
{
    public static void Send(NetPeer peer, ushort messageId, ReturnCode code, object data)
    {
        var writer = new NetDataWriter();
        writer.Put(messageId);
        writer.Put((byte)code);
        writer.Put(MessagePackSerializer.Serialize(data));
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
