
using Serilog;
using System.Reflection;
namespace SharedLibrary
{
    public class HotModule
    {
        public string HotDllPath { get; private set; }

        private Assembly hotfixAssembly = null;

        private HotAssemblyContextRef hotAssemblyContext;

        private HotAssemblyContextRef newHotAssembly;

        internal IHotLoadBridge HotLoadBridge { get; private set; }

        readonly Dictionary<string, OperationHandlerBase> serverHandlerMap = new();
        readonly Dictionary<string, Type> httpActions = new();
        public HotModule(string dllPath)
        {
            HotDllPath = dllPath;
        }

        public bool Init(bool isReload)
        {
            bool success = false;

            try
            {
                hotAssemblyContext = new HotAssemblyContextRef(HotDllPath);

                hotfixAssembly = hotAssemblyContext.HotLoadDll;

                if (!isReload)
                {
                    //初始化时 加载关联的程序集
                    LoadRefAssemblies();
                }

                ParseDll();

                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dllPath.txt"), HotDllPath);

                Log.Information($"hotfix dll init success: {HotDllPath}");
                success = true;
            }
            catch (Exception e)
            {
                Log.Error($"hotfix dll init failed...\n{e}");
            }

            return success;
        }

        public void Unload()
        {
            if (hotAssemblyContext != null)
            {
                var weak = hotAssemblyContext.Unload();
                //if (Settings.IsDebug)
                {
                    //检查hotfix dll是否已经释放
                    Task.Run(async () =>
                    {
                        int tryCount = 0;
                        while (weak.IsAlive && tryCount++ < 10)
                        {
                            await Task.Delay(100);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                        Log.Warning($"hotfix dll unloaded {(weak.IsAlive ? "failed" : "success")}");
                    });
                }
            }
        }


        private void LoadRefAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var nameSet = new HashSet<string>(assemblies.Select(t => t.GetName().Name));
            var hotfixRefAssemblies = hotfixAssembly.GetReferencedAssemblies();
            foreach (var refAssembly in hotfixRefAssemblies)
            {
                if (nameSet.Contains(refAssembly.Name))
                    continue;

                var refPath = $"{Environment.CurrentDirectory}/{refAssembly.Name}.dll";
                if (File.Exists(refPath))
                    Assembly.LoadFrom(refPath);
            }
        }

        private void ParseDll()
        {
            foreach (var type in hotfixAssembly.GetTypes())
            {
                if (!AddServerHandler(type) && !AddHttpAction(type))
                {
                    if (HotLoadBridge == null && type.GetInterface(typeof(IHotLoadBridge).FullName) != null)
                    {
                        var bridge = (IHotLoadBridge)Activator.CreateInstance(type);
                        HotLoadBridge = bridge;
                    }
                }
            }
        }

        private bool AddServerHandler(Type type)
        {
            if (!type.IsSubclassOf(typeof(OperationHandlerBase)))
                return false;

            string name = type.Name;
            if (!serverHandlerMap.ContainsKey(name))
            {
                var handler = (OperationHandlerBase)Activator.CreateInstance(type);
                serverHandlerMap.Add(name, handler);
            }
            else
            {
                Log.Error("重复添加{0}", name);
            }
            return true;
        }

        private bool AddHttpAction(Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseAction)))
                return false;

            string name = "/" + type.Name;
            if (!httpActions.ContainsKey(name))
            {
                httpActions.Add(name, type);
            }
            else
            {
                Log.Error("重复添加{0}", name);
            }
            return true;
        }

        internal OperationHandlerBase GetServerHandler(string name)
        {
            if (serverHandlerMap.TryGetValue(name, out var handler))
            {
                return handler;
            }
            return null;
        }

        internal BaseAction GetHttpAction(string name)
        {
            if (httpActions.TryGetValue(name, out var type))
            {
                var handler = (BaseAction)Activator.CreateInstance(type);
                return handler;
            }
            return null;
        }
    }
}
