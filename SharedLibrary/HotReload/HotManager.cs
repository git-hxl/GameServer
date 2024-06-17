
namespace SharedLibrary
{
    public class HotManager
    {
        private DateTime reloadTime;
        private bool doingHotload;

        private static volatile HotModule module = null;

        public static HotManager Instance { get; } = new HotManager();

        public async Task<bool> Load(string dllVersion = "", bool reload = false)
        {
            var dllPath = Path.Combine(Environment.CurrentDirectory, string.IsNullOrEmpty(dllVersion) ? "HotDlls/HotLibrary.dll" : $"{dllVersion}/HotLibrary.dll");
            var newModule = new HotModule(dllPath);

            // 起服时失败会有异常抛出
            var success = newModule.Init(reload);
            if (!success)
                return false;

            reloadTime = DateTime.Now;
            if (reload)
            {
                var oldModule = module;

                doingHotload = true;
                int oldModuleHash = oldModule.GetHashCode();

                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000 * 60 * 3);

                    oldModule.Unload();
                    doingHotload = false;
                });
            }

            module = newModule;

            if (module.HotLoadBridge != null)
                return await module.HotLoadBridge.OnLoadSuccess(reload);
            return true;
        }

        public Task Stop()
        {
            return module?.HotLoadBridge?.Stop() ?? Task.CompletedTask;
        }

        public OperationHandlerBase GetHandler(string name)
        {
            return module.GetServerHandler(name);
        }

        public BaseAction GetHttpAction(string name)
        {
            return module.GetHttpAction(name);
        }
    }
}