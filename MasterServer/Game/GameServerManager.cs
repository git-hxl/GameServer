
using SharedLibrary.Model;

namespace MasterServer.Game
{
    internal class GameServerManager
    {
        public static GameServerManager Instance { get; private set; } = new GameServerManager();
        public Dictionary<string, ServerInfo> ServerInfos { get; private set; } = new Dictionary<string, ServerInfo>();
        public void RegisterOrUpdate(string ipEndPoint, ServerInfo serverInfo)
        {
            ServerInfos[ipEndPoint] = serverInfo;
        }

        public void UnRegisterServer(string ipEndPoint, ServerInfo serverInfo)
        {
            ServerInfos.Remove(ipEndPoint);
        }
    }
}
