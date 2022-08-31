using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary;
using ShareLibrary.Utils;

namespace TestClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();

            while (!Console.KeyAvailable)
            {
                TestClientApplication.Instance.Update();
                Thread.Sleep(15);


                string command = Console.ReadLine();

                if(command == "auth")
                {
                    TestClientApplication.Instance.Auth();
                }

                if (command.Contains("join lobby"))
                {
                    string[] commands = command.Split(" ");
                    TestClientApplication.Instance.JoinLobby(commands[2]);
                }

                if (command.Contains("leave lobby"))
                {
                    string[] commands = command.Split(" ");
                    TestClientApplication.Instance.LeaveLobby(commands[2]);
                }


                if (command == "register")
                {
                    TestClientApplication.Instance.RegisterGame();
                }

            }
        }

    }
}