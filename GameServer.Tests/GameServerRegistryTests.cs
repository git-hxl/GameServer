using LiteNetLib;
using LobbyServer.Cluster;
using SharedLib.Models;

namespace GameServer.Tests;

public class GameServerRegistryTests
{
    private const string Key = "registry-test";

    [Fact]
    public void Remove_OldPeer_DoesNotRemoveNewRegistration()
    {
        using var env = new ConnectedPeers(2);
        var registry = new GameServerRegistry();
        var info = new GameServerInfo { Address = "127.0.0.1", Port = 7001 };

        registry.Register(env.Peers[0], info);
        registry.Register(env.Peers[1], info);
        registry.Remove(env.Peers[0]);

        var entry = registry.GetByAddressPort(info.Address, info.Port);

        Assert.NotNull(entry);
        Assert.Same(env.Peers[1], entry!.Peer);
    }

    [Fact]
    public void Register_OldPeerChangingKey_DoesNotRemoveNewRegistration()
    {
        using var env = new ConnectedPeers(2);
        var registry = new GameServerRegistry();
        var infoA = new GameServerInfo { Address = "127.0.0.1", Port = 7001 };
        var infoB = new GameServerInfo { Address = "127.0.0.1", Port = 7002 };

        registry.Register(env.Peers[0], infoA);
        registry.Register(env.Peers[1], infoA);
        registry.Register(env.Peers[0], infoB);

        Assert.NotNull(registry.GetByAddressPort(infoA.Address, infoA.Port));
        Assert.NotNull(registry.GetByAddressPort(infoB.Address, infoB.Port));
    }

    private sealed class ConnectedPeers : IDisposable
    {
        private readonly NetManager _listener;
        private readonly List<NetManager> _clients = [];

        public List<NetPeer> Peers { get; } = [];

        public ConnectedPeers(int count)
        {
            var listener = new EventBasedNetListener();
            listener.ConnectionRequestEvent += request => request.AcceptIfKey(Key);
            _listener = new NetManager(listener) { UpdateTime = 5 };

            var port = TestPorts.NextFree(1)[0];
            Assert.True(_listener.Start(port));

            for (var i = 0; i < count; i++)
            {
                var clientListener = new EventBasedNetListener();
                NetPeer? peer = null;
                clientListener.PeerConnectedEvent += connected => peer = connected;

                var client = new NetManager(clientListener) { UpdateTime = 5 };
                Assert.True(client.Start());
                client.Connect("127.0.0.1", port, Key);
                _clients.Add(client);

                var deadline = Environment.TickCount64 + 3000;
                while (peer == null && Environment.TickCount64 < deadline)
                {
                    _listener.PollEvents();
                    client.PollEvents();
                    Thread.Sleep(5);
                }

                Assert.NotNull(peer);
                Peers.Add(peer!);
            }
        }

        public void Dispose()
        {
            foreach (var client in _clients)
                client.Stop();
            _listener.Stop();
        }
    }
}
