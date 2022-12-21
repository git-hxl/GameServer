using SharedLibrary.Server;

namespace MasterServer
{
    internal class MasterConfig : ServerConfig
    {
        /// <summary>
        /// MySQL连接配置
        /// </summary>
        public string SQLConnectionStr = "";
    }
}