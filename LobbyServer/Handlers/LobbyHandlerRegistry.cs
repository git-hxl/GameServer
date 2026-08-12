using LiteNetLib;
using Serilog;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace LobbyServer.Handlers;

public class LobbyHandlerRegistry
{
    private readonly Dictionary<ushort, ILobbyHandler> _handlers = new();

    public void Register(ILobbyHandler handler)
    {
        _handlers[handler.MessageId] = handler;
    }

    public void Register(params ILobbyHandler[] handlers)
    {
        foreach (var handler in handlers)
            _handlers[handler.MessageId] = handler;
    }

    public bool Handle(NetPeer peer, byte[] payload, ushort messageId)
    {
        if (!_handlers.TryGetValue(messageId, out var handler))
        {
            Log.Warning("[LobbyServer] 未知消息ID messageId={MessageId}", messageId);
            MessageHelper.Send(peer, messageId, ReturnCode.Error, new { });
            return false;
        }

        handler.Handle(peer, payload);
        return true;
    }
}
