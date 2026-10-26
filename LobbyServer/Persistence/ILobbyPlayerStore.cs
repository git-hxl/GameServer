using SharedLib.Models;

namespace LobbyServer.Persistence;

/// <summary>
/// 大厅玩家资料的持久化存储。
/// </summary>
public interface ILobbyPlayerStore
{
    /// <summary>
    /// 读取全部玩家资料。
    /// </summary>
    /// <returns>玩家资料列表。</returns>
    IReadOnlyList<PlayerInfo> LoadAll();

    /// <summary>
    /// 保存玩家资料。
    /// </summary>
    /// <param name="player">玩家资料。</param>
    void Save(PlayerInfo player);

    /// <summary>
    /// 删除玩家资料。
    /// </summary>
    /// <param name="userId">玩家 ID。</param>
    void Delete(long userId);
}
