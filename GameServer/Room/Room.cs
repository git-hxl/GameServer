
using MessagePack;
using SharedLibrary;
using LiteNetLib;

namespace GameServer
{
    public class Room
    {
        public RoomInfo RoomInfo { get; private set; }

        public List<ClientPeer> ClientPeers { get; private set; } = new List<ClientPeer>();

        public bool IsActive { get; private set; }

        private long autoCleanTimer;

        private static object locker = new object();
        public Room(RoomInfo roomInfo)
        {
            RoomInfo = roomInfo;
            autoCleanTimer = 0;

            IsActive = true;
        }

        public bool AddClient(ClientPeer clientPeer)
        {
            lock (locker)
            {
                if (IsActive == false) return false;

                if (ClientPeers.Contains(clientPeer))
                {
                    return false;
                }

                if (ClientPeers.Count >= RoomInfo.RoomMaxPlayers)
                {
                    return false;
                }

                ClientPeers.Add(clientPeer);


                byte[] data = MessagePackSerializer.Serialize(clientPeer.UserInfo);

                foreach (var item in ClientPeers)
                {
                    if (item != clientPeer)
                        item.SendRequest(OperationCode.OtherJoinedRoom, data, DeliveryMethod.ReliableOrdered);
                }

                return true;
            }

        }

        public void RemoveClient(ClientPeer clientPeer)
        {
            lock (locker)
            {
                if (ClientPeers.Contains(clientPeer))
                {
                    ClientPeers.Remove(clientPeer);

                    byte[] data = MessagePackSerializer.Serialize(clientPeer.UserInfo);

                    foreach (var item in ClientPeers)
                    {
                        item.SendRequest(OperationCode.OtherLeaveRoom, data, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        /// <param name="deltaTime">单位时间（毫秒）</param>
        public void Update(long deltaTime)
        {
            AutoClean(deltaTime);
        }

        /// <summary>
        /// 自动清理
        /// </summary>
        /// <param name="deltaTime"></param>
        public void AutoClean(long deltaTime)
        {
            if (GameServer.Instance.GameConfig == null)
            {
                return;
            }

            if (GameServer.Instance.GameConfig.AutoCleanRoomTime <= 0)
            {
                return;
            }

            if (ClientPeers.Count <= 0)
            {
                autoCleanTimer += deltaTime;

                if (autoCleanTimer >= GameServer.Instance.GameConfig.AutoCleanRoomTime)
                {
                    IsActive = false;
                }
            }
            else
            {
                autoCleanTimer = 0;
                IsActive = true;
            }
        }

        public void Dispose()
        {
            ClientPeers.Clear();
            IsActive = false;
        }
    }
}
