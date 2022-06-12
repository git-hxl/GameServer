namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            MasterApplication.Instance.Init();
            try
            {
                while (true)
                {
                    MasterApplication.Instance.Update();
                    Thread.Sleep(15);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}