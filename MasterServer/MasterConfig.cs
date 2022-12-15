namespace MasterServer
{
    internal class MasterConfig
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port = 6000;
        /// <summary>
        /// 连接Key
        /// </summary>
        public string ConnectKey = "yoyo";
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxPeers = 5000;
        /// <summary>
        /// 延迟检测和检查连接的时间间隔
        /// </summary>
        public int PingInterval = 1000;
        /// <summary>
        /// 如果客户端或服务器在此期间未收到来自远程对等方的任何数据包，则连接将关闭
        /// </summary>
        public int DisconnectTimeout = 5000;
        /// <summary>
        /// 连接尝试之间的延迟
        /// </summary>
        public int ReconnectDelay = 500;
        /// <summary>
        /// 客户端停止并调用断开连接事件之前的最大连接尝试
        /// </summary>
        public int MaxConnectAttempts = 10;
        /// <summary>
        /// MySQL连接配置
        /// </summary>
        public string SQLConnectionStr = "";
    }
}