using SharedLib.Protocol;

namespace SharedLib.Utils;

/// <summary>
/// 一条完整消息帧：元数据 + 一次序列化好的完整字节（含 messageId + code + payload）
/// </summary>
public readonly struct MessageFrame
{
    public ushort MessageId { get; }
    public ReturnCode Code { get; }
    public byte[] Payload { get; }
    public byte[] Bytes { get; }

    public MessageFrame(ushort messageId, ReturnCode code, byte[] payload, byte[] bytes)
    {
        MessageId = messageId;
        Code = code;
        Payload = payload;
        Bytes = bytes;
    }
}
