using System.Reflection;
using System.Runtime.Loader;

namespace SharedLibrary
{

    public class HotAssemblyContextRef
    {
        public Assembly? HotLoadDll { get; }

        private CustomAssemblyLoadContext? customContext { get; }

        public HotAssemblyContextRef(string dllPath)
        {
            customContext = new CustomAssemblyLoadContext();

            HotLoadDll = customContext.LoadFromAssemblyPath(dllPath);
        }

        public WeakReference Unload()
        {
            if (customContext != null)
            {
                customContext.Unload();
            }

            return new WeakReference(customContext);
        }
    }

    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public CustomAssemblyLoadContext() : base(true) { }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
