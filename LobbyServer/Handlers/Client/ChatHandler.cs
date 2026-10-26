using LiteNetLib;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Handlers;
using LobbyServer.Player;
using Serilog;
using SharedLib.Utils;

namespace LobbyServer.Handlers.Client;

public class ChatHandler : MessageHandler<ChatRequest>
{
    public override ushort MessageId => MessageIds.Chat;
    
    private readonly PlayerManager _players;
    public ChatHandler(PlayerManager players)
    {
        _players = players;
    }

    public override void HandleMessage(NetPeer peer, ChatRequest request)
    {
        Log.Information("[LobbyManager] 聊天消息 userId={UserId}", request.UserId);

        var player = _players.Get(request.UserId);
        if (player == null)
        {
            Log.Warning("[LobbyManager] 聊天被拒绝：用户不在大厅中 userId={UserId}", request.UserId);
            return;
        }

        Log.Information("[LobbyManager] 聊天 userId={UserId} nickname={Nickname} content={Content}",
            request.UserId, request.Nickname, request.Content);

        Broadcast(MessageIds.ChatNotify, ReturnCode.Success, new ChatNotify
        {
            UserId = request.UserId,
            Nickname = request.Nickname,
            Content = request.Content
        });
    }
    
    private void Broadcast(ushort messageId, ReturnCode code, object data)
    {
        var frame = MessageHelper.CreateFrame(messageId, code, data);
        MessageHelper.SendToAll(_players.OnlinePeers, frame, DeliveryMethod.ReliableOrdered);
    }
}
