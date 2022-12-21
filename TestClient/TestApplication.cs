
using LiteNetLib;
using LiteNetLib.Utils;
using SharedLibrary.Operation;
using SharedLibrary.Operation.Request;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary.Operation;
using System.Net;

namespace TestClient
{
    internal class TestApplication
    {
        static void Main(string[] args)
        {
            try
            {
                LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
                loggerConfiguration.WriteTo.File("./TestLog.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
                loggerConfiguration.MinimumLevel.Information();
                Log.Logger = loggerConfiguration.CreateLogger();

                TestServer testServer = new TestServer();

                testServer.Start();

                Task.Run(() =>
                {

                    while(true)
                    {
                        string command = Console.ReadLine();
                        if (command.Contains("register"))
                        {
                            Register(testServer);
                        }

                        if (command.Contains("login"))
                        {
                            Login(testServer);
                        }

                        if (command.Contains("server register"))
                        {
                            ServerRegister(testServer);
                        }
                    }
                });

                while (true)
                {
                    testServer.Update();
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        static void Register(TestServer testServer)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((ushort)OperationCode.Register);
            RegisterRequest request = new RegisterRequest();
            request.Account = "hxl";
            request.Password = "123456";

            netDataWriter.Put(MessagePackSerializer.Serialize(request));

            testServer.MasterPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        static void Login(TestServer testServer)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((ushort)OperationCode.Login);
            RegisterRequest request = new RegisterRequest();
            request.Account = "hxl";
            request.Password = "123456";

            netDataWriter.Put(MessagePackSerializer.Serialize(request));
            testServer.MasterPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }
        static void ServerRegister(TestServer testServer)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((ushort)OperationCode.GameServerRegister);
            ServerRegisterRequest request = new ServerRegisterRequest();

            netDataWriter.Put(MessagePackSerializer.Serialize(request));
            testServer.MasterPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        async void Test()
        {
            //var userTable = await MySQLTool.QueryFirstOrDefaultAsync<UserTable>($"select * from user where Account='{"hxl"}' and Password={123456}");

            //if (userTable != null)
            //{
            //    Log.Information("select user info:" + userTable.ID);
            //}

            //string sql = $"INSERT INTO `user` (Account,`Password`) VALUES('{"qwe1"}',{123456})";

            //int result = await MySQLTool.ExecuteAsync(sql);

            //Log.Information("INSERT effect rows:" + result);

            //sql = $"update user set password={123456},nickname='{"xxxxx"}' where account='{"hxl"}'";

            //result = await MySQLTool.ExecuteAsync(sql);

            //Log.Information("update effect rows:" + result);

            //sql = $"delete from user where account='{"qwe1"}'";

            //result = await MySQLTool.ExecuteAsync(sql);

            //Log.Information("delete effect rows:" + result);
        }
    }
}