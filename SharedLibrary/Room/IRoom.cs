
using SharedLibrary;

namespace SharedLibrary
{
    public interface IRoom
    {
        public RoomInfo RoomInfo { get; set; }
        public List<BasePeer> ClientPeers { get; set; }

        public bool IsActive { get; set; }

        void OnCreated(RoomInfo roomInfo);
        /// <summary>
        /// 每帧更新
        /// </summary>
        /// <param name="deltaTime">单位时间（毫秒）</param>
        void OnUpdate(long deltaTime);

        bool OnPlayerEnter(BasePeer basePeer);

        void OnPlayerLeave(BasePeer basePeer);

        void Destroy();
    }
}
