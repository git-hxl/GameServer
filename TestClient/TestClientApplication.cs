using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer.Operations;
using Newtonsoft.Json;
using Serilog;
using ShareLibrary;
using ShareLibrary.Utils;

namespace TestClient
{
    internal class TestClientApplication
    {
        protected NetManager netManager;
        protected EventBasedNetListener netListener;

        public ServerConfig ServerConfig { get; protected set; }

        public static TestClientApplication Instance { get; private set; } = new TestClientApplication();

        private NetPeer client;
        public TestClientApplication()
        {
            ServerConfig = new ServerConfig();

            netListener = new EventBasedNetListener();
            netListener.PeerConnectedEvent += NetListener_PeerConnectedEvent;
            netListener.PeerDisconnectedEvent += NetListener_PeerDisconnectedEvent;
            netListener.NetworkReceiveEvent += NetListener_NetworkReceiveEvent;

            netManager = new NetManager(netListener);
            netManager.PingInterval = ServerConfig.pingInterval;
            netManager.DisconnectTimeout = ServerConfig.disconnectTimeout;
            netManager.ReconnectDelay = ServerConfig.reconnectDelay;
            netManager.MaxConnectAttempts = ServerConfig.maxConnectAttempts;

            netManager.UnsyncedEvents = true;
            netManager.Start();
            client = netManager.Connect("localhost", ServerConfig.port, ServerConfig.connectKey);
        }

        public virtual void Close()
        {
            if (netManager != null)
                netManager.Stop();
        }

        public virtual void Update()
        {
            if (netManager != null)
            {
                netManager.PollEvents();
            }
        }

        protected void NetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            OperationCode operationCode = (OperationCode)reader.GetByte();
            ReturnCode returnCode = (ReturnCode)reader.GetShort();
            Log.Information("{0} result: {1}", operationCode.ToString(), returnCode.ToString());
            if (returnCode != 0)
                return;

            switch (operationCode)
            {
                case OperationCode.Auth:
                    OnAuth(reader.GetRemainingBytes());
                    break;
                default:
                    break;
            }


        }

        protected void NetListener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("NetListener_PeerConnectedEvent: {0} id:{1}", peer.EndPoint, peer.Id);
        }

        protected void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("NetListener_PeerDisconnectedEvent: {0}", peer.EndPoint);
        }

        public void Auth()
        {
            Token token = new Token();
            token.UserID = "1111";
            token.NickName = "愤怒的阿凡提i";
            token.TimeStamp = DateTimeEx.TimeStamp.ToString();
            token.ExpiryTime = DateTime.Now.AddMinutes(30).ToString();

            string tokenJson = JsonConvert.SerializeObject(token);

            string tokenEncrypt = SecurityUtil.AESEncrypt(tokenJson, TestClientApplication.Instance.ServerConfig.encryptKey);

            AuthRequest authRequest = new AuthRequest();

            authRequest.Token = tokenEncrypt;

            Send(OperationCode.Auth, authRequest.Serialize<AuthRequest>());
        }

        public void OnAuth(byte[] data)
        {
            AuthResponse authResponse = AuthResponse.Deserialize<AuthResponse>(data);

            Log.Information("response:" + authResponse.UserID + " " + authResponse.NickName);
        }

        public void Send(OperationCode operationCode, byte[] data)
        {
            NetDataWriter netDataWriter = new NetDataWriter();
            netDataWriter.Put((byte)operationCode);
            if (data != null && data.Length > 0)
            {
                netDataWriter.Put(data);
            }
            client.Send(netDataWriter, DeliveryMethod.ReliableOrdered);

            Log.Information(operationCode.ToString());
        }
    }
}
