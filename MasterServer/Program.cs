namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            MasterServer masterServer = new MasterServer();
            masterServer.Init();

            while(true)
            {
                masterServer.Update();
                Thread.Sleep(15);
            }

        }
    }
}