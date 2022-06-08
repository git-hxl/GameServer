using System.Collections.Concurrent;
using System.Text;

namespace GameServer
{
    internal class MsgManager
    {
        private static MsgManager instance;
        public static MsgManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MsgManager();
                return instance;
            }
        }

        private ConcurrentQueue<byte[]> msgQueue = new ConcurrentQueue<byte[]>();

        public void Enqueue(byte[] bytes)
        {
            msgQueue.Enqueue(bytes);
        }

        public byte[] Dequeue()
        {
            byte[] bytes;
            msgQueue.TryDequeue(out bytes);
            return bytes;
        }


        public void Update()
        {
            while (msgQueue.Count > 0)
            {
                byte[] bytes = Dequeue();
                if (bytes != null)
                {
                    Console.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            }
        }
    }
}
