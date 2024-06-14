
namespace GameServer
{
    public class GameConfig
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port;
        /// <summary>
        /// 连接Key
        /// </summary>
        public string ConnectKey = "";
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxPeers;
        /// <summary>
        /// 延迟检测和检查连接的时间间隔
        /// </summary>
        public int PingInterval;
        /// <summary>
        /// 如果客户端或服务器在此期间未收到来自远程对等方的任何数据包，则连接将关闭
        /// </summary>
        public int DisconnectTimeout;
        /// <summary>
        /// 连接尝试之间的延迟
        /// </summary>
        public int ReconnectDelay;
        /// <summary>
        /// 客户端停止并调用断开连接事件之前的最大连接尝试
        /// </summary>
        public int MaxConnectAttempts;
        /// <summary>
        /// 服务器更新间隔
        /// </summary>
        public int UpdateInterval;
        /// <summary>
        /// Master服务器IP
        /// </summary>
        public string MasterIP = "";
        /// <summary>
        /// MySQL连接配置
        /// </summary>
        public string SQLConnectionStr = "";
        /// <summary>
        /// Redis连接配置
        /// </summary>
        public string RedisConnectionStr = "";
        /// <summary>
        /// 自动清理空余房间的时间，该时间内房间没有活动会自动清理 <=0不处理
        /// </summary>
        public long AutoCleanRoomTime;
    }
}