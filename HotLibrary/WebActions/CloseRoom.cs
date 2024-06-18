

using GameServer;
using LiteNetLib;
using SharedLibrary;
using System.Collections.Specialized;

namespace HotLibrary
{
    public class CloseRoom : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            //throw new NotImplementedException();

            IRoom? room = RoomManager.Instance.GetRoom(nameValueCollection["RoomID"]);

            if (room != null)
            {
                room.Destroy();

                ReturnMsg = "关闭房间成功";
            }
            else
            {
                ReturnMsg = "房间不存在";
            }
        }

        public override void OnPost(string content)
        {
            //throw new NotImplementedException();
        }

        public override string OnResponse()
        {
            return ReturnMsg;
        }
    }
}
