using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace GameServer
{
    internal class GameApplication
    {
        static void Main(string[] args)
        {
            try
            {
                GameConfig? gameConfig = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText("./GameConfig.json"));

                if (gameConfig == null)
                {
                    throw new Exception("读取配置文件失败");
                }

                GameServer.Instance.Init(gameConfig);
                GameServer.Instance.Start();

                while (true)
                {
                    //Stopwatch stopwatch = Stopwatch.StartNew();
                    GameServer.Instance.Update();
                    Thread.Sleep(gameConfig.UpdateInterval);
                    //stopwatch.Stop();
                    //Console.WriteLine($"Slept for approximately: {stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
