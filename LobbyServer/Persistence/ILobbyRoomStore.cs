namespace LobbyServer.Persistence;

public interface ILobbyRoomStore
{
    IReadOnlyList<LobbyRoomSnapshot> LoadAll();
    void Save(LobbyRoomSnapshot room);
    void Delete(string roomId);
}
