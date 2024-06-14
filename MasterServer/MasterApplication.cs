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
                MasterConfig? masterConfig = JsonConvert.DeserializeObject<MasterConfig>(File.ReadAllText("./MasterConfig.json"));

                if (masterConfig == null)
                {
                    throw new Exception("读取配置文件失败");
                }

                MasterServer.Instance.Init(masterConfig);
                MasterServer.Instance.Start();

                while (true)
                {
                    MasterServer.Instance.Update();
                    Thread.Sleep(masterConfig.UpdateInterval);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}