namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            MasterApplication.Instance.Init();
            while (true)
            {
                MasterApplication.Instance.Update();
                Thread.Sleep(15);
            }

        }
    }
}