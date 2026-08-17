using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using SharedLib.Protocol;

namespace SharedLib.Utils;

public static class MessageHelper
{
    public static void Send(NetPeer peer, ushort messageId, ReturnCode code, object data)
    {
        Send(peer, messageId, code, data, DeliveryMethod.ReliableOrdered);
    }

    public static void Send(NetPeer peer, ushort messageId, ReturnCode code, object data, DeliveryMethod method)
    {
        peer.Send(BuildWriter(messageId, code, data), method);
    }

    public static void Send(NetPeer peer, ushort messageId, ReturnCode code)
    {
        Send(peer, messageId, code, (object)Array.Empty<byte>(), DeliveryMethod.ReliableOrdered);
    }

    public static void Send(NetPeer peer, byte[] frame, DeliveryMethod method)
    {
        var writer = new NetDataWriter();
        writer.Put(frame);
        peer.Send(writer, method);
    }

    public static byte[] SerializeFrame(ushort messageId, ReturnCode code, object data)
    {
        var writer = BuildWriter(messageId, code, data);
        return writer.Data[..writer.Length];
    }

    public static (ushort MessageId, byte Code, byte[] Payload) ReadFrame(NetPacketReader reader)
    {
        var messageId = reader.GetUShort();
        var code = reader.GetByte();
        var payload = reader.GetRemainingBytes();
        return (messageId, code, payload ?? []);
    }

    private static NetDataWriter BuildWriter(ushort messageId, ReturnCode code, object data)
    {
        var writer = new NetDataWriter();
        writer.Put(messageId);
        writer.Put((byte)code);
        writer.Put(MessagePackSerializer.Serialize(data));
        return writer;
    }
}
