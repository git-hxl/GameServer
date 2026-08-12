using LiteNetLib;
using Serilog;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Handlers;

public class GameHandlerRegistry
{
    private readonly Dictionary<ushort, IGameHandler> _handlers = new();

    public void Register(IGameHandler handler)
    {
        _handlers[handler.MessageId] = handler;
    }

    public void Register(params IGameHandler[] handlers)
    {
        foreach (var handler in handlers)
            _handlers[handler.MessageId] = handler;
    }

    public bool Handle(NetPeer peer, byte[] payload, ushort messageId)
    {
        if (!_handlers.TryGetValue(messageId, out var handler))
        {
            Log.Warning("[GameServer] 未知消息ID messageId={MessageId}", messageId);
            MessageHelper.Send(peer, messageId, ReturnCode.Error, new { });
            return false;
        }

        handler.Handle(peer, payload);
        return true;
    }
}
