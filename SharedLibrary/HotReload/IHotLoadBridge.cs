

namespace SharedLibrary
{
    public interface IHotLoadBridge
    {
        //ServerType BridgeType { get; }

        Task<bool> OnLoadSuccess(bool reload);

        Task Stop();
    }
}
