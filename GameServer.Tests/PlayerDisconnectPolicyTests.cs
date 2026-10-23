using LiteNetLib;

namespace GameServer.Tests;

public class PlayerDisconnectPolicyTests
{
    [Theory]
    [InlineData(DisconnectReason.RemoteConnectionClose, false)]
    [InlineData(DisconnectReason.DisconnectPeerCalled, false)]
    [InlineData(DisconnectReason.Timeout, true)]
    [InlineData(DisconnectReason.ConnectionFailed, true)]
    [InlineData(DisconnectReason.HostUnreachable, true)]
    [InlineData(DisconnectReason.NetworkUnreachable, true)]
    [InlineData(DisconnectReason.ConnectionRejected, true)]
    [InlineData(DisconnectReason.InvalidProtocol, true)]
    [InlineData(DisconnectReason.UnknownHost, true)]
    [InlineData(DisconnectReason.Reconnect, true)]
    [InlineData(DisconnectReason.PeerToPeerConnection, true)]
    [InlineData(DisconnectReason.PeerNotFound, true)]
    public void ShouldKeepPlayer_MapsReasons(DisconnectReason reason, bool expected)
    {
        Assert.Equal(expected, LobbyServer.Player.PlayerDisconnectPolicy.ShouldKeepPlayer(reason));
    }
}
