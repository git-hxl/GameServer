using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;

// ── 测试用户配置 ─────────────────────────────────────────────────────
const int ServerPort = 6001;
const string ConnectionKey = "Game@wasd9527";
var userId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var player = new PlayerInfo
{
    UserId = userId,
    Nickname = $"Test_{userId % 10000}",
    Gender = 1,
    Avatar = "default.png",
    Age = 20
};

// ── 初始化日志 ───────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("test_client_log.txt",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .CreateLogger();

Log.Information("[TestClient] 启动 userId={UserId} nickname={Nickname}", userId, player.Nickname);

// ── 创建 LobbyServer 客户端 ──────────────────────────────────────────
var listener = new EventBasedNetListener();
var client = new NetManager(listener);

// ── 创建 GameServer 客户端 ──────────────────────────────────────────
var gameListener = new EventBasedNetListener();
var gameClient = new NetManager(gameListener);
NetPeer? gamePeer = null;
string? gameRoomId = null;

// ── LobbyServer 消息处理 ─────────────────────────────────────────────

// 连接成功
listener.PeerConnectedEvent += peer => { Log.Information("[TestClient] 已连接到LobbyServer"); };

// 断开连接
listener.PeerDisconnectedEvent += (peer, info) => { Log.Information("[TestClient] LobbyServer连接断开 reason={Reason}", info.Reason); };

// 收到消息
listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
{
    try
    {
        var messageId = reader.GetUShort();
        var code = reader.GetByte();
        var payload = reader.GetRemainingBytes();

        switch (messageId)
        {
            case MessageIds.JoinLobby:
                var joinRes = MessagePackSerializer.Deserialize<JoinLobbyResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 加入大厅成功 userId={UserId} nickname={Nickname}",
                        joinRes?.Player.UserId, joinRes?.Player.Nickname);
                else
                    Log.Warning("[TestClient] 加入大厅失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.LeaveLobby:
                var leaveRes = MessagePackSerializer.Deserialize<LeaveLobbyResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 离开大厅成功 userId={UserId}", leaveRes?.UserId);
                else
                    Log.Warning("[TestClient] 离开大厅失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.ChatNotify:
                var chatNotify = MessagePackSerializer.Deserialize<ChatNotify>(payload);
                Log.Information("[TestClient] 聊天 nickname={Nickname} content={Content}",
                    chatNotify?.Nickname, chatNotify?.Content);
                break;

            case MessageIds.CreateRoom:
                var createRes = MessagePackSerializer.Deserialize<CreateRoomResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 房间创建成功 roomId={RoomId} GameServer={Addr}:{Port}",
                        createRes?.Room.RoomId, createRes?.Room.GameServerAddress, createRes?.Room.GameServerPort);
                else
                    Log.Warning("[TestClient] 房间创建失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.JoinRoom:
                var joinRoomRes = MessagePackSerializer.Deserialize<JoinRoomResponse>(payload);
                if (code == 0)
                {
                    var r = joinRoomRes?.Room;
                    Log.Information("[TestClient] 房间加入成功 roomId={RoomId} GameServer={Addr}:{Port} 房主={Owner}",
                        r?.RoomId, r?.GameServerAddress, r?.GameServerPort, r?.OwnerUserId);
                    if (r?.Players is { Count: > 0 })
                    {
                        Log.Information("[TestClient] 房间现有成员");
                        foreach (var p in r.Players)
                            Log.Information("[TestClient] 成员 nickname={Nickname} userId={UserId}", p.Nickname, p.UserId);
                    }
                }
                else
                    Log.Warning("[TestClient] 房间加入失败 reason={Reason}", (ReturnCode)code);

                break;

            case MessageIds.LeaveRoom:
                var leaveRoomRes = MessagePackSerializer.Deserialize<LeaveRoomResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 房间已离开 roomId={RoomId}", leaveRoomRes?.RoomId);
                else
                    Log.Warning("[TestClient] 房间离开失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.JoinRoomNotify:
                var joinNotify = MessagePackSerializer.Deserialize<JoinRoomNotify>(payload);
                Log.Information("[TestClient] 玩家加入房间 nickname={Nickname} userId={UserId} roomId={RoomId}",
                    joinNotify?.Player.Nickname, joinNotify?.Player.UserId, joinNotify?.RoomId);
                break;

            case MessageIds.GameReady:
                var readyRes = MessagePackSerializer.Deserialize<GameReadyResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 准备状态更新 ready={Ready}/{Total} allReady={AllReady}",
                        readyRes?.ReadyCount, readyRes?.TotalCount, readyRes?.AllReady);
                else
                    Log.Warning("[TestClient] 准备失败：不在房间中");
                break;

            case MessageIds.GameUnready:
                var unreadyRes = MessagePackSerializer.Deserialize<GameUnreadyResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 取消准备 ready={Ready}/{Total}",
                        unreadyRes?.ReadyCount, unreadyRes?.TotalCount);
                else
                    Log.Warning("[TestClient] 取消准备失败：不在房间中");
                break;

            case MessageIds.GameStart:
                if (code == 0)
                    Log.Information("[TestClient] 游戏开始请求成功，等待GameServer通知");
                else
                    Log.Warning("[TestClient] 游戏开始失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.GameStartNotify:
                var startNotify = MessagePackSerializer.Deserialize<GameStartNotify>(payload);
                Log.Information("[TestClient] 游戏开始通知 address={Addr}:{Port} roomId={RoomId}",
                    startNotify?.GameServerAddress, startNotify?.GameServerPort, startNotify?.RoomId);
                if (startNotify != null)
                {
                    gameRoomId = startNotify.RoomId;
                    gameClient.Connect(startNotify.GameServerAddress, startNotify.GameServerPort, ConnectionKey);
                }
                break;

            case MessageIds.RoomList:
                var roomList = MessagePackSerializer.Deserialize<RoomListResponse>(payload);
                if (roomList?.Rooms.Count > 0)
                {
                    Log.Information("[TestClient] 房间列表");
                    foreach (var r in roomList.Rooms)
                        Log.Information("[TestClient] 房间 roomId={RoomId} type={RoomType} playerCount={PlayerCount}", r.RoomId, r.RoomType, r.PlayerCount);
                }
                else
                {
                    Log.Information("[TestClient] 房间列表为空");
                }

                break;

            default:
                Log.Information("[TestClient] 收到未知Lobby消息 messageId={MessageId}", messageId);
                break;
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "[TestClient] Lobby消息解析失败");
    }
    finally
    {
        reader.Recycle();
    }
};

// 网络错误
listener.NetworkErrorEvent += (endPoint, error) => { Log.Error("[TestClient] Lobby网络错误 error={Error} endpoint={EndPoint}", error, endPoint); };

// ── GameServer 消息处理 ─────────────────────────────────────────────
gameListener.PeerConnectedEvent += peer =>
{
    gamePeer = peer;
    Log.Information("[TestClient] GameServer已连接");

    if (gameRoomId != null)
    {
        var writer = new NetDataWriter();
        writer.Put(MessageIds.JoinGame);
        writer.Put((byte)ReturnCode.Success);
        writer.Put(MessagePackSerializer.Serialize(new JoinGameRequest
        {
            RoomId = gameRoomId,
            Player = player
        }));
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
        Log.Information("[TestClient] 自动加入游戏房间 roomId={RoomId}", gameRoomId);
    }
};

gameListener.PeerDisconnectedEvent += (peer, info) =>
{
    gamePeer = null;
    Log.Information("[TestClient] GameServer连接断开 reason={Reason}", info.Reason);
};

gameListener.NetworkReceiveEvent += (peer, reader, channel, method) =>
{
    try
    {
        var messageId = reader.GetUShort();
        var code = reader.GetByte();
        var payload = reader.GetRemainingBytes();

        switch (messageId)
        {
            case MessageIds.JoinGame:
                var joinGameRes = MessagePackSerializer.Deserialize<JoinGameResponse>(payload);
                if (code == 0)
                    Log.Information("[TestClient] 游戏房间加入成功 roomId={RoomId} 房主={Owner}",
                        joinGameRes?.RoomId, joinGameRes?.OwnerUserId);
                else
                    Log.Warning("[TestClient] 游戏房间加入失败 reason={Reason}", (ReturnCode)code);
                break;

            case MessageIds.LeaveGame:
                if (code == 0)
                    Log.Information("[TestClient] 游戏房间离开成功");
                else
                    Log.Warning("[TestClient] 游戏房间离开失败");
                break;

            case MessageIds.JoinGameNotify:
                var gameJoinNotify = MessagePackSerializer.Deserialize<JoinGameNotify>(payload);
                Log.Information("[TestClient] 玩家加入游戏房间 nickname={Nickname} userId={UserId}",
                    gameJoinNotify?.Player.Nickname, gameJoinNotify?.Player.UserId);
                break;
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "[TestClient] GameServer消息解析失败");
    }
    finally
    {
        reader.Recycle();
    }
};

// ── LobbyServer: 启动并连接 ──────────────────────────────────────────
client.Start();
client.Connect("localhost", ServerPort, ConnectionKey);
gameClient.Start();

// ── 发送消息帮助方法 ────────────────────────────────────────────────
void SendMessage(ushort messageId, object data)
{
    var peer = client.FirstPeer;
    if (peer?.ConnectionState != ConnectionState.Connected)
    {
        Log.Warning("[TestClient] 未连接到LobbyServer");
        return;
    }

    var writer = new NetDataWriter();
    writer.Put(messageId);
    writer.Put((byte)ReturnCode.Success);
    writer.Put(MessagePackSerializer.Serialize(data));
    peer.Send(writer, DeliveryMethod.ReliableOrdered);
}

void SendGameMessage(ushort messageId, object data)
{
    if (gamePeer?.ConnectionState != ConnectionState.Connected)
    {
        Log.Warning("[TestClient] 未连接到GameServer");
        return;
    }

    var writer = new NetDataWriter();
    writer.Put(messageId);
    writer.Put((byte)ReturnCode.Success);
    writer.Put(MessagePackSerializer.Serialize(data));
    gamePeer.Send(writer, DeliveryMethod.ReliableOrdered);
}

// ── 主循环 + 命令行交互 ────────────────────────────────────────────
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(15));
var cts = new CancellationTokenSource();
var quitFlag = false;

Log.Information("[TestClient] 可用命令: joinlobby | leavelobby | chat <内容> | createroom | joinroom <roomId> | leaveroom | rooms | ready | unready | start | joingame <roomId> | leavegame | status | quit");

_ = Task.Run(async () =>
{
    while (!quitFlag)
    {
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var parts = line.Split(' ', 2);
        var cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "joinlobby":
                SendMessage(MessageIds.JoinLobby, new JoinLobbyRequest { Player = player });
                break;

            case "leavelobby":
                SendMessage(MessageIds.LeaveLobby, new LeaveLobbyRequest
                {
                    UserId = userId
                });
                break;

            case "createroom":
                SendMessage(MessageIds.CreateRoom, new CreateRoomRequest());
                break;

            case "joinroom":
                if (parts.Length < 2)
                {
                    Log.Information("[TestClient] 用法: joinroom <RoomId>");
                    break;
                }

                SendMessage(MessageIds.JoinRoom, new JoinRoomRequest
                {
                    RoomId = parts[1]
                });
                break;

            case "leaveroom":
                SendMessage(MessageIds.LeaveRoom, new LeaveRoomRequest());
                break;

            case "rooms":
                SendMessage(MessageIds.RoomList, new RoomListRequest());
                break;

            case "ready":
                SendMessage(MessageIds.GameReady, new GameReadyRequest());
                break;

            case "start":
                SendMessage(MessageIds.GameStart, new GameStartRequest());
                break;

            case "unready":
                SendMessage(MessageIds.GameUnready, new { });
                break;

            case "joingame":
                if (parts.Length < 2)
                {
                    Log.Information("[TestClient] 用法: joingame <RoomId>");
                    break;
                }
                gameRoomId = parts[1];
                SendGameMessage(MessageIds.JoinGame, new JoinGameRequest
                {
                    RoomId = parts[1],
                    Player = player
                });
                break;

            case "leavegame":
                SendGameMessage(MessageIds.LeaveGame, new { });
                gameRoomId = null;
                break;

            case "chat":
                if (parts.Length < 2)
                {
                    Log.Information("[TestClient] 用法: chat <内容>");
                    break;
                }

                SendMessage(MessageIds.Chat, new ChatRequest
                {
                    UserId = userId,
                    Nickname = player.Nickname,
                    Content = parts[1]
                });
                break;

            case "status":
                var p = client.FirstPeer;
                Log.Information("[TestClient] 连接状态 state={State} latency={Latency}ms",
                    p?.ConnectionState, p?.Ping);
                break;

            case "quit":
                var lobbyPeer = client.FirstPeer;
                if (lobbyPeer != null)
                {
                    client.DisconnectPeer(lobbyPeer);
                    Log.Information("[TestClient] 已断开LobbyServer");
                }
                if (gamePeer != null)
                {
                    gameClient.DisconnectPeer(gamePeer);
                    Log.Information("[TestClient] 已断开GameServer");
                }

                quitFlag = true;
                cts.Cancel();
                break;
        }
    }
});

// 网络轮询循环
try
{
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        client.PollEvents();
        gameClient.PollEvents();
    }
}
catch (OperationCanceledException)
{
}

// ── 关闭 ────────────────────────────────────────────────────────────
gameClient.Stop();
client.Stop();
Log.Information("[TestClient] 已关闭");