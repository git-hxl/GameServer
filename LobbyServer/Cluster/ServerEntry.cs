using LiteNetLib;
using SharedLib.Models;

namespace LobbyServer.Cluster;

public record ServerEntry(NetPeer Peer, GameServerInfo Info);
