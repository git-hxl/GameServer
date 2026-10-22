using LiteNetLib;
using SharedLib.Protocol;
using SharedLib.Utils;

namespace GameServer.Tests;

public class MessageHelperTests
{
    [Fact]
    public void TrySend_ToUnconnectedPeer_ReturnsFalse()
    {
        var listener = new EventBasedNetListener();
        var manager = new NetManager(listener);
        Assert.True(manager.Start());

        try
        {
            var peer = manager.Connect("127.0.0.1", 1, "test");
            var frame = MessageHelper.CreateFrame(MessageIds.JoinLobby, ReturnCode.Success);
            Assert.False(MessageHelper.TrySend(peer, frame));
        }
        finally
        {
            manager.Stop();
        }
    }
}
