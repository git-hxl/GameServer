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
                GameServer.Instance = new GameServer(gameConfig);
                GameServer.Instance.Start();
                GameServer.Instance.RegisterToMasterServer(5000);
                while (true)
                {
                    GameServer.Instance.Update();
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
