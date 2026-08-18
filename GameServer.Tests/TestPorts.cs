using System.Net;
using System.Net.Sockets;

namespace GameServer.Tests;

public static class TestPorts
{
    public static int NextFree()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
