using CommonLibrary.Core;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CommonLibrary.Core
{
    public class HandleResponse
    {
        public static void SendResponse(NetPeer netPeer, ReturnCode returnCode, MsgPack? msgPack, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)returnCode);
            if (msgPack != null)
            {
                byte[] data = MessagePack.MessagePackSerializer.Serialize(msgPack);
                netDataWriter.Put(data);
            }
            netPeer.Send(netDataWriter, deliveryMethod);
        }
    }
}