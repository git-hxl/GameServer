

using LiteNetLib;
using MasterServer;
using MessagePack;
using Newtonsoft.Json;
using SharedLibrary;
using System.Collections.Specialized;

namespace HotLibrary
{
    public class CreateRoom : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            //throw new NotImplementedException();
            RoomInfo roomInfo = new RoomInfo();

            roomInfo.RoomID = Guid.NewGuid().ToString();

            roomInfo.RoomName = nameValueCollection["RoomName"];
            roomInfo.RoomType = int.Parse(nameValueCollection["RoomType"]) ;
            roomInfo.RoomDescription = "";
            roomInfo.RoomPassword = "";
            roomInfo.RoomMaxPlayers = -1;

            GamePeer? gamePeer = MasterServer.MasterServer.Instance .GetLowerLoadLevelingServer();

            //没有合适的服务器
            if (gamePeer == null)
            {
                ReturnMsg = "没有合适的服务器";
                return;
            }

            byte[] data = MessagePackSerializer.Serialize(roomInfo);

            //向Game服务器请求创建房间
            gamePeer.SendRequest(OperationCode.CreateRoom, data, DeliveryMethod.ReliableOrdered);

            CreateRoomResponse response = new CreateRoomResponse();
            response.RoomID = roomInfo.RoomID;
            response.GameServer = gamePeer.NetPeer.EndPoint.ToString();

            ReturnMsg = $"创建房间成功: {JsonConvert.SerializeObject(response)}";
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
