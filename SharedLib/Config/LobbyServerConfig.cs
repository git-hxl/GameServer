namespace SharedLib.Config
{

    /// <summary>
    /// 大厅服务器配置参数
    /// </summary>
    public class LobbyServerConfig
    {
        public int Port { get; set; } = 9050;
        public string ConnectionKey { get; set; } = "Game@wasd9527";
        public int UpdateTime { get; set; } = 15;
        public int PingInterval { get; set; } = 1000;
        public int DisconnectTimeout { get; set; } = 5000;
        public byte ChannelsCount { get; set; } = 1;
    }
}
