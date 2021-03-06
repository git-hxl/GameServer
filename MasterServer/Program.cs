using Serilog;

namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("log.txt").CreateLogger();

            MasterApplication.Instance.Start();

            while (true)
            {
                try
                {
                    MasterApplication.Instance.Update();
                    Thread.Sleep(15);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            MasterApplication.Instance.Close();

            Log.CloseAndFlush();
        }
    }
}