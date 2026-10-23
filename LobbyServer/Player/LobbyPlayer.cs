using SharedLib.Models;

namespace LobbyServer.Player;

public class LobbyPlayer
{
    public PlayerInfo Info { get; set; } = new();

    /// <summary>
    /// 当前连接的 PeerId；-1 表示未连接。
    /// </summary>
    public int PeerId { get; set; } = -1;

    public string? CurrentRoomId { get; set; }
}
