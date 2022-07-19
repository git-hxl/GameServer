using System.Diagnostics;

namespace GameServer.Operations
{
    internal class OperationHandleBase
    {
        public void HandleRequest(HandleRequest handleRequest)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (handleRequest.OperationCode)
            {
                case GameOperationCode.JoinGame:
                   
                    break;
                case GameOperationCode.ExitGame:
                    
                    break;
                case GameOperationCode.RPC:
                    
                    break;

                default:
                    
                    break;
            }
            Console.WriteLine("{0}：{1} 代码耗时：{2}", DateTime.Now.ToLongTimeString(), handleRequest.OperationCode.ToString(), stopwatch.ElapsedMilliseconds);
        }


        private void JoinGame(HandleRequest handleRequest)
        {
            //GameApplication.Instance.GetOrCreateGame()
        }
    }
}
