using GameServer;
using Serilog;

namespace GamerServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("log.txt").CreateLogger();

            Log.Information("Start GameServer");

            GameApplication.Instance.Start();

            try
            {
                while (true)
                {
                    GameApplication.Instance.Update();
                    Thread.Sleep(15);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            GameApplication.Instance.Close();

            Log.CloseAndFlush();
        }
    }
}

