
using MessagePack;
using LiteNetLib;

namespace SharedLibrary
{
    public class RoomBase : IRoom
    {
        public RoomInfo RoomInfo { get; set; }

        public List<BasePeer> ClientPeers { get; set; } = new List<BasePeer>();

        public bool IsActive { get; set; }

        private static object locker = new object();

        public virtual void OnCreated(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            IsActive = true;
        }
        /// <summary>
        /// 每帧更新
        /// </summary>
        /// <param name="deltaTime">单位时间（毫秒）</param>
        public virtual void OnUpdate(long deltaTime)
        {
        }

        public virtual bool OnPlayerEnter(BasePeer basePeer)
        {
            lock (locker)
            {
                if (IsActive == false) return false;

                if (basePeer == null) return false;

                if (ClientPeers.Contains(basePeer))
                {
                    return false;
                }

                if (RoomInfo.RoomMaxPlayers > 0 && ClientPeers.Count >= RoomInfo.RoomMaxPlayers)
                {
                    return false;
                }

                ClientPeers.Add(basePeer);
 
                byte[] data = MessagePackSerializer.Serialize(basePeer.UserInfo);

                foreach (var item in ClientPeers)
                {
                    if (item != basePeer)
                        item.SendRequest(OperationCode.OtherJoinedRoom, data, DeliveryMethod.ReliableOrdered);
                }

                return true;
            }

        }

        public virtual void OnPlayerLeave(BasePeer basePeer)
        {
            lock (locker)
            {
                if (basePeer != null && ClientPeers.Contains(basePeer))
                {
                    ClientPeers.Remove(basePeer);

                    byte[] data = MessagePackSerializer.Serialize(basePeer.UserInfo);

                    foreach (var item in ClientPeers)
                    {
                        item.SendRequest(OperationCode.OtherLeaveRoom, data, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        public virtual void Destroy()
        {
            foreach (var basePeer in ClientPeers)
            {
                if (basePeer != null)
                {
                    basePeer.SendRequest(OperationCode.CloseRoom, null, DeliveryMethod.ReliableOrdered);
                }
            }

            ClientPeers.Clear();
            IsActive = false;
        }

     
    }
}
