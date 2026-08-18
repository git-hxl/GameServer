using LiteNetLib;
using Serilog;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace SharedLib.Handlers;

public class HandlerRegistry
{
    private readonly Dictionary<ushort, IHandler> _handlers = new();

    public void Register(IHandler handler)
    {
        _handlers[handler.MessageId] = handler;
    }

    public void Register(params IHandler[] handlers)
    {
        foreach (var handler in handlers)
            _handlers[handler.MessageId] = handler;
    }

    public bool Handle(NetPeer peer, ushort messageId, byte[] payload)
    {
        if (!_handlers.TryGetValue(messageId, out var handler))
        {
            Log.Warning("未知消息ID messageId={MessageId}", messageId);
            MessageHelper.Send(peer, messageId, ReturnCode.Error);
            return false;
        }

        handler.Handle(peer, messageId, payload);
        return true;
    }
}
