using LobbyServer.Room;

namespace LobbyServer.Persistence;

/// <summary>
/// 大厅房间持久化存储；直接存取 LobbyRoom。
/// </summary>
public interface ILobbyRoomStore
{
    /// <summary>
    /// 读取全部大厅房间。
    /// </summary>
    /// <returns>大厅房间列表。</returns>
    IReadOnlyList<LobbyRoom> LoadAll();

    /// <summary>
    /// 保存单个大厅房间。
    /// </summary>
    /// <param name="room">大厅房间。</param>
    void Save(LobbyRoom room);

    /// <summary>
    /// 删除大厅房间。
    /// </summary>
    /// <param name="roomId">房间 ID。</param>
    void Delete(string roomId);
}
