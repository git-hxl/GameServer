using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using SharedLib.Models;
using SharedLib.Protocol;
using SharedLib.Utils;

// ── 测试用户配置 ─────────────────────────────────────────────────────
const int ServerPort = 6001;
const string ConnectionKey = "Client@wasd9527";
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
string? pendingJoinRoomId = null;
long pendingJoinDeadline = 0;
long lastJoinAttempt = 0;
var roomServers = new Dictionary<string, (string Address, int Port)>();
string? pendingGameRoomId = null;
string? gameAddress = null;
int gamePort = 0;

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
        var frame = MessageHelper.ReadFrame(reader);

        switch (frame.MessageId)
        {
            case MessageIds.JoinLobby:
                var joinRes = MessageHelper.Deserialize<JoinLobbyResponse>(frame);
                if (frame.Code == ReturnCode.Success)
                    Log.Information("[TestClient] 加入大厅成功 userId={UserId} nickname={Nickname}",
                        joinRes?.Player.UserId, joinRes?.Player.Nickname);
                else
                    Log.Warning("[TestClient] 加入大厅失败 reason={Reason}", frame.Code);
                break;

            case MessageIds.LeaveLobby:
                var leaveRes = MessageHelper.Deserialize<LeaveLobbyResponse>(frame);
                if (frame.Code == ReturnCode.Success)
                    Log.Information("[TestClient] 离开大厅成功 userId={UserId}", leaveRes?.UserId);
                else
                    Log.Warning("[TestClient] 离开大厅失败 reason={Reason}", frame.Code);
                break;

            case MessageIds.ChatNotify:
                var chatNotify = MessageHelper.Deserialize<ChatNotify>(frame);
                Log.Information("[TestClient] 聊天 nickname={Nickname} content={Content}",
                    chatNotify?.Nickname, chatNotify?.Content);
                break;

            case MessageIds.CreateRoom:
                var createRes = MessageHelper.Deserialize<CreateRoomResponse>(frame);
                if (frame.Code == ReturnCode.Success)
                {
                    Log.Information("[TestClient] 房间创建成功 roomId={RoomId} GameServer={Addr}:{Port}",
                        createRes?.Room.RoomId, createRes?.Room.GameServerAddress, createRes?.Room.GameServerPort);
                    if (createRes?.Room != null)
                        RememberGameServer(createRes.Room);
                }
                else
                    Log.Warning("[TestClient] 房间创建失败 reason={Reason}", frame.Code);
                break;

            case MessageIds.JoinRoom:
                var joinRoomRes = MessageHelper.Deserialize<JoinRoomResponse>(frame);
                if (frame.Code == ReturnCode.Success)
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

                    if (r != null)
                        RememberGameServer(r);
                }
                else
                    Log.Warning("[TestClient] 房间加入失败 reason={Reason}", frame.Code);

                break;

            case MessageIds.LeaveRoom:
                var leaveRoomRes = MessageHelper.Deserialize<LeaveRoomResponse>(frame);
                if (frame.Code == ReturnCode.Success)
                    Log.Information("[TestClient] 房间已离开 roomId={RoomId}", leaveRoomRes?.RoomId);
                else
                    Log.Warning("[TestClient] 房间离开失败 reason={Reason}", frame.Code);
                break;

            case MessageIds.JoinRoomNotify:
                var joinNotify = MessageHelper.Deserialize<JoinRoomNotify>(frame);
                Log.Information("[TestClient] 玩家加入房间 nickname={Nickname} userId={UserId} roomId={RoomId}",
                    joinNotify?.Player.Nickname, joinNotify?.Player.UserId, joinNotify?.RoomId);
                break;

            case MessageIds.RoomOwnerChangedNotify:
                var ownerNotify = MessageHelper.Deserialize<RoomOwnerChangedNotify>(frame);
                Log.Information("[TestClient] 房主变更 roomId={RoomId} owner={Owner}",
                    ownerNotify?.RoomId, ownerNotify?.OwnerUserId);
                break;

            case MessageIds.RoomClosedNotify:
                var closedNotify = MessageHelper.Deserialize<RoomClosedNotify>(frame);
                Log.Information("[TestClient] 房间已关闭 roomId={RoomId} reason={Reason}",
                    closedNotify?.RoomId, closedNotify?.Reason);
                gameRoomId = null;
                pendingGameRoomId = null;
                break;

            case MessageIds.RoomList:
                var roomList = MessageHelper.Deserialize<RoomListResponse>(frame);
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
                Log.Information("[TestClient] 收到未知Lobby消息 messageId={MessageId}", frame.MessageId);
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

    if (pendingGameRoomId != null)
        SendJoinGame();
};

gameListener.PeerDisconnectedEvent += (peer, info) =>
{
    if (gamePeer == peer)
        gamePeer = null;
    Log.Information("[TestClient] GameServer连接断开 reason={Reason}", info.Reason);
};

gameListener.NetworkReceiveEvent += (peer, reader, channel, method) =>
{
    try
    {
        var frame = MessageHelper.ReadFrame(reader);

        switch (frame.MessageId)
        {
            case MessageIds.JoinGame:
                var joinGameRes = MessageHelper.Deserialize<JoinGameResponse>(frame);
                if (frame.Code == ReturnCode.Success)
                {
                    pendingJoinRoomId = null;
                    pendingGameRoomId = null;
                    Log.Information("[TestClient] 游戏房间加入成功 roomId={RoomId}", joinGameRes?.RoomId);
                }
                else if (frame.Code == ReturnCode.RoomNotFound)
                {
                    Log.Warning("[TestClient] 游戏房间尚未创建，稍后重试 roomId={RoomId}", gameRoomId);
                    pendingJoinRoomId = gameRoomId;
                    pendingJoinDeadline = Environment.TickCount64 + 1000;
                }
                else
                {
                    pendingJoinRoomId = null;
                    Log.Warning("[TestClient] 游戏房间加入失败 reason={Reason}", frame.Code);
                }
                break;

            case MessageIds.LeaveGame:
                if (frame.Code == ReturnCode.Success)
                    Log.Information("[TestClient] 游戏房间离开成功");
                else
                    Log.Warning("[TestClient] 游戏房间离开失败");
                break;

            case MessageIds.JoinGameNotify:
                var gameJoinNotify = MessageHelper.Deserialize<JoinGameNotify>(frame);
                Log.Information("[TestClient] 玩家加入游戏房间 nickname={Nickname} userId={UserId}",
                    gameJoinNotify?.Player.Nickname, gameJoinNotify?.Player.UserId);
                break;

            case MessageIds.RoomClosedNotify:
                var gameClosed = MessageHelper.Deserialize<RoomClosedNotify>(frame);
                Log.Information("[TestClient] 游戏房间已关闭 roomId={RoomId} reason={Reason}",
                    gameClosed?.RoomId, gameClosed?.Reason);
                gameRoomId = null;
                pendingGameRoomId = null;
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

    MessageHelper.Send(peer, messageId, ReturnCode.Success, data);
}

void SendGameMessage(ushort messageId, object data)
{
    if (gamePeer?.ConnectionState != ConnectionState.Connected)
    {
        Log.Warning("[TestClient] 未连接到GameServer");
        return;
    }

    MessageHelper.Send(gamePeer, messageId, ReturnCode.Success, data);
}

void RememberGameServer(RoomInfo room)
{
    roomServers[room.RoomId] = (room.GameServerAddress, room.GameServerPort);
    Log.Information("[TestClient] 房间 {RoomId} 的 GameServer 为 {Addr}:{Port}，执行 joingame {RoomId} 进入游戏房间",
        room.RoomId, room.GameServerAddress, room.GameServerPort);
}

void SendJoinGame()
{
    if (gameRoomId == null)
        return;

    SendGameMessage(MessageIds.JoinGame, new JoinGameRequest
    {
        RoomId = gameRoomId,
        Player = player
    });
    Log.Information("[TestClient] 手动加入游戏房间 roomId={RoomId}", gameRoomId);
}

void JoinGameRoom(string roomId)
{
    if (!roomServers.TryGetValue(roomId, out var server))
    {
        Log.Warning("[TestClient] 未知房间的游戏服地址，请先 createroom/joinroom roomId={RoomId}", roomId);
        return;
    }

    if (gameRoomId != null && gameRoomId != roomId &&
        gamePeer?.ConnectionState == ConnectionState.Connected)
    {
        SendGameMessage(MessageIds.LeaveGame, new LeaveGameRequest());
    }

    gameRoomId = roomId;
    pendingGameRoomId = roomId;
    pendingJoinRoomId = null;

    if (gamePeer?.ConnectionState == ConnectionState.Connected &&
        gameAddress == server.Address && gamePort == server.Port)
    {
        SendJoinGame();
        return;
    }

    if (gamePeer?.ConnectionState == ConnectionState.Connected)
        gameClient.DisconnectPeer(gamePeer);

    gameAddress = server.Address;
    gamePort = server.Port;
    gameClient.Connect(server.Address, server.Port, ConnectionKey);
}

// ── 主循环 + 命令行交互 ────────────────────────────────────────────
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(15));
var cts = new CancellationTokenSource();
var quitFlag = false;

Log.Information("[TestClient] 可用命令: joinlobby | leavelobby | chat <内容> | createroom [maxPlayers] [roomId] | joinroom <roomId> | leaveroom | rooms | joingame <roomId> | leavegame | status | quit");

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
                if (gamePeer?.ConnectionState == ConnectionState.Connected)
                    SendGameMessage(MessageIds.LeaveGame, new LeaveGameRequest());
                gameRoomId = null;
                pendingGameRoomId = null;
                pendingJoinRoomId = null;
                SendMessage(MessageIds.LeaveLobby, new LeaveLobbyRequest
                {
                    UserId = userId
                });
                break;

            case "createroom":
                var cArgs = parts.Length > 1 ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
                var maxPlayers = 0;
                var roomId = null as string;
                if (cArgs.Length > 0)
                {
                    if (int.TryParse(cArgs[0], out var parsed))
                        maxPlayers = parsed;
                    else
                        roomId = cArgs[0];
                }
                if (cArgs.Length > 1)
                    roomId = cArgs[1];
                SendMessage(MessageIds.CreateRoom, new CreateRoomRequest
                {
                    RoomId = roomId,
                    RoomType = RoomType.Default,
                    MaxPlayers = maxPlayers
                });
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
                if (gamePeer?.ConnectionState == ConnectionState.Connected)
                    SendGameMessage(MessageIds.LeaveGame, new LeaveGameRequest());
                gameRoomId = null;
                pendingGameRoomId = null;
                pendingJoinRoomId = null;
                SendMessage(MessageIds.LeaveRoom, new LeaveRoomRequest());
                break;

            case "rooms":
                SendMessage(MessageIds.RoomList, new RoomListRequest());
                break;

            case "joingame":
                if (parts.Length < 2)
                {
                    Log.Information("[TestClient] 用法: joingame <RoomId>");
                    break;
                }
                JoinGameRoom(parts[1]);
                break;

            case "leavegame":
                SendGameMessage(MessageIds.LeaveGame, new LeaveGameRequest());
                gameRoomId = null;
                pendingGameRoomId = null;
                pendingJoinRoomId = null;
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

        if (pendingJoinRoomId != null)
        {
            if (Environment.TickCount64 >= pendingJoinDeadline)
            {
                pendingJoinRoomId = null;
            }
            else if (Environment.TickCount64 - lastJoinAttempt >= 100)
            {
                lastJoinAttempt = Environment.TickCount64;
                SendGameMessage(MessageIds.JoinGame, new JoinGameRequest
                {
                    RoomId = pendingJoinRoomId,
                    Player = player
                });
            }
        }
    }
}
catch (OperationCanceledException)
{
}

// ── 关闭 ────────────────────────────────────────────────────────────
gameClient.Stop();
client.Stop();
Log.Information("[TestClient] 已关闭");