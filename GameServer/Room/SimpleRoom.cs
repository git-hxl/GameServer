
using SharedLibrary;

namespace GameServer.Room
{
    public class SimpleRoom : RoomBase
    {
        private long autoCleanTimer;

        public override void OnCreated(RoomInfo roomInfo)
        {
            base.OnCreated(roomInfo);
            autoCleanTimer = 0;
        }

        public override void OnUpdate(long deltaTime)
        {
            base.OnUpdate(deltaTime);

            AutoClean(deltaTime);
        }


        /// <summary>
        /// 自动清理
        /// </summary>
        /// <param name="deltaTime"></param>
        protected virtual void AutoClean(long deltaTime)
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
    }
}
