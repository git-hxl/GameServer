using MasterServer.MySQL;
using MasterServer.MySQL.Table;
using Serilog;
using SharedLibrary.Utils;

namespace MasterServer
{
    internal class MasterApplication
    {
        static void Main(string[] args)
        {
            try
            {
                LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
                loggerConfiguration.WriteTo.File("./MasterLog.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
                loggerConfiguration.MinimumLevel.Information();
                Log.Logger = loggerConfiguration.CreateLogger();

                MasterServer masterServer = new MasterServer();

                masterServer.Start();

                SystemInfo systemInfo = new SystemInfo();

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        Log.Information(systemInfo.ToString());
                    }
                });

                while (true)
                {
                    masterServer.Update();
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