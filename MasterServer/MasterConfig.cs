using SharedLibrary.Server;

namespace MasterServer
{
    public class MasterConfig : ServerConfig
    {
        /// <summary>
        /// MySQL连接配置
        /// </summary>
        public string SQLConnectionStr = "";
    }
}