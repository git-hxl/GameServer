using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
using SharedLibrary;
namespace MasterServer
{
    public class GamePeer : BasePeer
    {
        public GameInfo GameInfo { get; set; }

        public List<RoomInfo> Rooms { get; private set; }

        public GamePeer(NetPeer peer) : base(peer)
        {
            GameInfo = new GameInfo();

            Rooms = new List<RoomInfo>();
        }

        public override void OnRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //base.OnRequest(operationCode, data, deliveryMethod);

            switch (operationCode)
            {
                case OperationCode.UpdateGameServerInfo:

                    GameInfo = MessagePackSerializer.Deserialize<GameInfo>(data);

                    string gameInfo = JsonConvert.SerializeObject(GameInfo);

                    Log.Information("GamerServer Info {0}", gameInfo);

                    RedisManager.Instance.StringSet($"GameInfo_{GameInfo.IPEndPoint}", gameInfo);

                    break;
                case OperationCode.UpdateRoomList:
                    Rooms = MessagePackSerializer.Deserialize<List<RoomInfo>>(data);

                    //Log.Information("GamerServer房间数量：{0}", Rooms.Count);
                    break;
            }

        }


        public override void OnResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod)
        {
            //base.OnResponse(operationCode, returnCode, data, deliveryMethod);

            Log.Information($"OperationCode：{operationCode} ReturnCode: {returnCode}");

            switch (operationCode)
            {
                case OperationCode.CreateRoom:

                    if (returnCode == ReturnCode.Success)
                    {

                    }

                    break;
            }
        }


    }
}