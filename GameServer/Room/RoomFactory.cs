

using SharedLibrary;

namespace GameServer
{
    public class RoomFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static IRoom CreatRoom(int roomType, string roomName = "")
        {
            return new RoomBase();
        }
    }
}
