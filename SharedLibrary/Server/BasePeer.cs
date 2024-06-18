
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;

namespace SharedLibrary
{
    public abstract class BasePeer
    {
        public NetPeer NetPeer { get; protected set; }

        public UserInfo? UserInfo { get; set; }

        public BasePeer(NetPeer netPeer)
        {
            NetPeer = netPeer;
        }

        public abstract void OnRequest(OperationCode operationCode, byte[] data, DeliveryMethod deliveryMethod);
        public abstract void OnResponse(OperationCode operationCode, ReturnCode returnCode, byte[] data, DeliveryMethod deliveryMethod);

        public virtual void SendRequest(OperationCode operationCode, byte[]? data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((ushort)operationCode);
            if (data != null)
                netDataWriter.Put(data);

            if (NetPeer == null)
            {
                Log.Warning("消息{0} 发送目标 is Null！！", operationCode);
                return;
            }
            NetPeer.Send(netDataWriter, 0, deliveryMethod);
        }

        public virtual void SendResponse(OperationCode operationCode, ReturnCode returnCode, byte[]? data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((ushort)operationCode);
            netDataWriter.Put((ushort)returnCode);
            if (data != null)
                netDataWriter.Put(data);

            if (NetPeer == null)
            {
                Log.Warning("消息{0} 发送目标 is Null！！", operationCode);
                return;
            }
            NetPeer.Send(netDataWriter, 1, deliveryMethod);
        }
    }
}
