using System.Net;
using System.Net.Sockets;

namespace GameServer.Tests;

public static class TestPorts
{
    /// <summary>
    /// 预留指定数量的空闲 UDP 端口；避免用 TCP 探端口后实际 UDP 端口被占用。
    /// </summary>
    /// <param name="count">需要的端口数量。</param>
    /// <returns>互不相同的空闲 UDP 端口。</returns>
    public static int[] NextFree(int count)
    {
        var clients = new List<UdpClient>();
        var ports = new List<int>();

        try
        {
            for (var i = 0; i < count; i++)
            {
                var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                clients.Add(udp);
                ports.Add(((IPEndPoint)udp.Client.LocalEndPoint!).Port);
            }
        }
        finally
        {
            foreach (var udp in clients)
                udp.Dispose();
        }

        return ports.ToArray();
    }
}
