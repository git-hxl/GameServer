using LiteNetLib;
using MessagePack;
using Serilog;

namespace SharedLib.Handlers;

public abstract class MessageHandler<TRequest> : IHandler where TRequest : class
{
    public abstract ushort MessageId { get; }

    public abstract void HandleMessage(NetPeer peer, TRequest request);

    public void Handle(NetPeer peer, byte[] payload)
    {
        TRequest? request;
        try
        {
            request = MessagePackSerializer.Deserialize<TRequest>(payload);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "反序列化失败 messageId={MessageId}", MessageId);
            OnDeserializeFailed(peer);
            return;
        }

        if (request == null)
        {
            OnDeserializeFailed(peer);
            return;
        }

        HandleMessage(peer, request);
    }

    protected virtual void OnDeserializeFailed(NetPeer peer)
    {
    }
}
