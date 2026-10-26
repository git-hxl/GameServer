using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Player;

public class LobbyPlayer
{
    public PlayerInfo Info { get; set; } = new();
    
    public NetPeer? Peer { get; private set; }

    public string? CurrentRoomId { get; private set; }

    public void SetPeer(NetPeer peer)
    {
        Peer = peer;
    }
    
    public void ResetPeer()
    {
        Peer = null;
    }

    public void SetRoom(string roomId)
    {
        if (!string.IsNullOrEmpty(roomId))
        {
            CurrentRoomId =  roomId;
        }
    }
    
    public void ResetRoom()
    {
        CurrentRoomId = null;
    }
}
