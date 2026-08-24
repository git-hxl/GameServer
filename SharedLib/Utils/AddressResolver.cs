using System.Net;
using System.Net.Sockets;

namespace SharedLib.Utils
{

    /// <summary>
    /// 本机广告地址探测：优先取配置值，否则取第一个非回环 IPv4
    /// </summary>
    public static class AddressResolver
    {
        public static string Resolve(string? configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address))
                {
                    return address.ToString();
                }
            }

            return "127.0.0.1";
        }
    }
}
