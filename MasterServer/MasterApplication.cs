using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Utils;

namespace MasterServer
{
    internal class MasterApplication
    {
        static void Main(string[] args)
        {
            try
            {
                MasterConfig MasterConfig = JsonConvert.DeserializeObject<MasterConfig>(File.ReadAllText("./MasterConfig.json"));
                Server.MasterServer.Instance = new Server.MasterServer(MasterConfig);
                MySQLTool.SQLConnectionStr = MasterConfig.SQLConnectionStr;
                Server.MasterServer.Instance.Start();
                while (true)
                {
                    Server.MasterServer.Instance.Update();
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}