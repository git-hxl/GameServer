using Newtonsoft.Json;
using Serilog;

namespace GameServer
{
    internal class GameApplication
    {
        static void Main(string[] args)
        {
            try
            {
                GameConfig gameConfig = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText("./GameConfig.json"));
                GameServer.Server.GameServer.Instance = new Server.GameServer(gameConfig);

                Server.GameServer.Instance.Start();
                while (true)
                {
                    Server.GameServer.Instance.Update();
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
