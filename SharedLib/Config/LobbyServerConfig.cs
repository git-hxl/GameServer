namespace SharedLib.Config
{

    /// <summary>
    /// 大厅服务器配置参数
    /// </summary>
    public class LobbyServerConfig
    {
        public int ClientPort { get; set; } = 6001;
        public int ServerPort { get; set; } = 6002;
        public string ClientConnectionKey { get; set; } = "Client@wasd9527";
        public string ServerConnectionKey { get; set; } = "Game@wasd9527";
        public int UpdateTime { get; set; } = 15;
        public int PingInterval { get; set; } = 1000;
        public int DisconnectTimeout { get; set; } = 5000;
        public byte ChannelsCount { get; set; } = 1;
    }
}
