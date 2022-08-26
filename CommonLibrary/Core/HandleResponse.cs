using CommonLibrary.Core;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CommonLibrary.Core
{
    public class HandleResponse
    {
        public static void SendResponse(HandleRequest handleRequest, MsgPack msgPack)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)handleRequest.OperationCode);
            if (msgPack != null)
            {
                byte[] data = MessagePack.MessagePackSerializer.Serialize(msgPack);
                netDataWriter.Put(data);
            }
            handleRequest.NetPeer.Send(netDataWriter, handleRequest.DeliveryMethod);

            handleRequest.NetPeer.NetManager.TriggerUpdate();
        }

        public static void SendToPeer(NetPeer netPeer,OperationCode operationCode, MsgPack msgPack ,DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            if (msgPack != null)
            {
                byte[] data = MessagePack.MessagePackSerializer.Serialize(msgPack);
                netDataWriter.Put(data);
            }
            netPeer.Send(netDataWriter, deliveryMethod);

            netPeer.NetManager.TriggerUpdate();
        }
    }
}