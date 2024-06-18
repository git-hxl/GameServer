

using GameServer;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary;

namespace HotLibrary.Rooms
{
    internal class HotTestRoom : RoomBase
    {
        public override void OnUpdate(long deltaTime)
        {
            base.OnUpdate(deltaTime);

            Log.Error("qqqqqqqqqqqqqqqq" + JsonConvert.SerializeObject(RoomInfo));
        }
    }
}
