

using Serilog;
using SharedLibrary;

namespace HotLibrary
{
    internal class HotLoadBridge : IHotLoadBridge
    {
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
