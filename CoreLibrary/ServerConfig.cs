﻿namespace CoreLibrary
{
    public class ServerConfig
    {
        public int port;

        public string connectKey = "yoyo";
        //最大连接数
        public int maxPeers = 5000;
        //ping 间隔
        public int pingInterval = 1000;
        //断开超时
        public int disconnectTimeout = 5000;
        //重连延迟
        public int reconnectDelay = 500;
        //尝试重连次数
        public int maxConnectAttempts = 10;
    }
}
