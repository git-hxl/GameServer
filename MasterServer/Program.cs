namespace MasterServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Server!");
            MasterApplication.Instance.Start();
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

            MasterApplication.Instance.Close();
        }
    }
}