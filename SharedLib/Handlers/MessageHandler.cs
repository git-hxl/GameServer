using LiteNetLib;
using Serilog;
using SharedLib.Utils;

namespace SharedLib.Handlers;

public abstract class MessageHandler<TRequest> : IHandler where TRequest : class
{
    public abstract ushort MessageId { get; }

    public abstract void HandleMessage(NetPeer peer, TRequest request);

    public void Handle(NetPeer peer, ushort messageId, byte[] payload)
    {
        if (!TryAuthorize(peer))
        {
            OnUnauthorized(peer);
            return;
        }

        TRequest? request;
        try
        {
            request = MessageHelper.Deserialize<TRequest>(payload);
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

    protected virtual bool TryAuthorize(NetPeer peer) => true;

    protected virtual void OnUnauthorized(NetPeer peer)
    {
    }

    protected virtual void OnDeserializeFailed(NetPeer peer)
    {
    }
}
