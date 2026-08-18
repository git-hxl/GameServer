using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using MessagePack.Resolvers;
using SharedLib.Protocol;

namespace SharedLib.Utils;

public static class MessageHelper
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard
            .WithResolver(StandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray);

    // ── 构建 ─────────────────────────────────────────────────────────

    public static MessageFrame CreateFrame(ushort messageId, ReturnCode code, object? data = null)
    {
        var payload = data == null ? [] : MessagePackSerializer.Serialize(data, Options);

        var writer = new NetDataWriter();
        writer.Put(messageId);
        writer.Put((byte)code);
        writer.Put(payload);

        return new MessageFrame(messageId, code, payload, writer.Data[..writer.Length]);
    }

    // ── 发送（自动组装） ──────────────────────────────────────────────

    public static void Send(NetPeer peer, ushort messageId, ReturnCode code,
        object? data = null, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        Send(peer, CreateFrame(messageId, code, data), method);
    }

    // ── 发送（复用已构建帧） ──────────────────────────────────────────

    public static void Send(NetPeer peer, MessageFrame frame,
        DeliveryMethod method = DeliveryMethod.ReliableOrdered)
    {
        var writer = new NetDataWriter();
        writer.Put(frame.Bytes);
        peer.Send(writer, method);
    }

    /// <summary>向多个 peer 广播同一帧，序列化一次、复用字节</summary>
    public static void SendToAll(IEnumerable<NetPeer> peers, MessageFrame frame, DeliveryMethod method)
    {
        var writer = new NetDataWriter();
        writer.Put(frame.Bytes);
        foreach (var peer in peers)
            peer.Send(writer, method);
    }

    // ── 解析 ─────────────────────────────────────────────────────────

    public static MessageFrame ReadFrame(NetPacketReader reader)
    {
        var messageId = reader.GetUShort();
        var code = reader.GetByte();
        var payload = reader.GetRemainingBytes() ?? [];

        return new MessageFrame(messageId, (ReturnCode)code, payload, payload);
    }

    public static T? Deserialize<T>(MessageFrame frame) where T : class
    {
        return Deserialize<T>(frame.Payload);
    }

    public static T? Deserialize<T>(byte[] payload) where T : class
    {
        return MessagePackSerializer.Deserialize<T>(payload, Options);
    }
}
