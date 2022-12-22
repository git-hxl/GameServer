using MasterServer.MySQL;
using Newtonsoft.Json;
using Serilog;

namespace MasterServer
{
    internal class MasterApplication
    {
        static void Main(string[] args)
        {
            try
            {
                MasterConfig MasterConfig = JsonConvert.DeserializeObject<MasterConfig>(File.ReadAllText("./MasterConfig.json"));
                MySQLTool.SQLConnectionStr = MasterConfig.SQLConnectionStr;
                MasterServer.Instance = new MasterServer(MasterConfig);
                MasterServer.Instance.Start();
                while (true)
                {
                    MasterServer.Instance.Update();
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