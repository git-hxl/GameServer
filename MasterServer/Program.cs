using Newtonsoft.Json;
using Serilog;
using ShareLibrary;

namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            while (true)
            {
                MasterApplication.Instance.Update();
                Thread.Sleep(15);
            }
        }
    }
}