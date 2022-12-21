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
                GameServer gameServer = new GameServer(gameConfig);
                gameServer.Start();
                while (true)
                {
                    gameServer.Update();
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
