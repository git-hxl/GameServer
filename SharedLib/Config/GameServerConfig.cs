namespace SharedLib.Config
{

    /// <summary>
    /// 游戏服务器配置参数
    /// </summary>
    public class GameServerConfig
    {
        public int Port { get; set; } = 7001;
        public string LobbyAddress { get; set; } = "127.0.0.1";
        public int LobbyPort { get; set; } = 6002;
        public string LobbyConnectionKey { get; set; } = "Game@wasd9527";
        public string ClientConnectionKey { get; set; } = "Client@wasd9527";
        public int UpdateTime { get; set; } = 15;
        public int PingInterval { get; set; } = 1000;
        public int DisconnectTimeout { get; set; } = 5000;
        public byte ChannelsCount { get; set; } = 1;
    }
}
