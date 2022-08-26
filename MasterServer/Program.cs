using Serilog;

namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
#if DEBUG
            loggerConfiguration.MinimumLevel.Information();
#else
            loggerConfiguration.MinimumLevel.Warning();
#endif
            loggerConfiguration.WriteTo.Console();
            loggerConfiguration.WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
            Log.Logger = loggerConfiguration.CreateLogger();

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
                    break;
                }
            }

            MasterApplication.Instance.Close();

            Log.CloseAndFlush();
        }
    }
}