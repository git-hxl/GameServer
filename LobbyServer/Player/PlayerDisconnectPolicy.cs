using LiteNetLib;

namespace LobbyServer.Player;

/// <summary>
/// 大厅玩家断线处理策略：主动断开移除，异常断线保留等待重连
/// </summary>
public static class PlayerDisconnectPolicy
{
    public static bool ShouldKeepPlayer(DisconnectReason reason)
    {
        return reason is DisconnectReason.ConnectionFailed
            or DisconnectReason.Timeout
            or DisconnectReason.HostUnreachable
            or DisconnectReason.NetworkUnreachable
            or DisconnectReason.ConnectionRejected
            or DisconnectReason.InvalidProtocol
            or DisconnectReason.UnknownHost
            or DisconnectReason.Reconnect
            or DisconnectReason.PeerToPeerConnection
            or DisconnectReason.PeerNotFound;
    }
}
