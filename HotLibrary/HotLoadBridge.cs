

using Serilog;
using SharedLibrary;

namespace HotLibrary
{
    internal class HotLoadBridge : IHotLoadBridge
    {
        /// <summary>
        /// dll加载成功
        /// </summary>
        /// <param name="reload"></param>
        /// <returns></returns>
        public async Task<bool> OnLoadSuccess(bool reload)
        {
            return true;
        }

        public async Task Stop()
        {
            try
            {

            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            finally
            {

            }
        }
    }
}
