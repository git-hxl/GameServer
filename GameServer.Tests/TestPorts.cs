using System.Net;
using System.Net.Sockets;

namespace GameServer.Tests;

public static class TestPorts
{
    public static int[] NextFree(int count)
    {
        var listeners = new List<TcpListener>();
        var ports = new List<int>();

        try
        {
            for (var i = 0; i < count; i++)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                listeners.Add(listener);
                ports.Add(((IPEndPoint)listener.LocalEndpoint).Port);
            }
        }
        finally
        {
            foreach (var listener in listeners)
                listener.Stop();
        }

        return ports.ToArray();
    }
}
