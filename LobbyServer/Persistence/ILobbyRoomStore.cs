namespace LobbyServer.Persistence;

public interface ILobbyRoomStore : IDisposable
{
    IReadOnlyList<LobbyRoomSnapshot> LoadAll();
    void Save(LobbyRoomSnapshot room);
    void Delete(string roomId);
}
