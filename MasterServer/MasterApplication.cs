using Newtonsoft.Json;
using Serilog;
using SharedLibrary;
using System.Runtime.Loader;

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

                HotManager.Instance.Load();

                while (true)
                {
                    MasterServer.Instance.Update();
                    Thread.Sleep(masterConfig.UpdateInterval);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);

                HotManager.Instance.Stop();
            }
        }

        private static void LoadContext_Unloading(AssemblyLoadContext obj)
        {
            Console.WriteLine("当前卸载的程序集：" + string.Join(',', obj.Assemblies.Select(x => x.FullName)));
        }

    }
}