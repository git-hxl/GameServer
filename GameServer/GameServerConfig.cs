namespace GameServer
{
    public class GameServerConfig
    {
        public string? MasterIP;

        public int MasterPort;

        public int Port;

        public string? ConnectKey;

        public int MaxPeers;
        //ping 间隔
        public int PingInterval;
        //断开超时
        public int DisconnectTimeout;
        //重连延迟
        public int ReconnectDelay;
        //尝试重连次数
        public int MaxConnectAttempts;
    }
}
