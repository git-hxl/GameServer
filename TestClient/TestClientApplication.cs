using LiteNetLib;
using LiteNetLib.Utils;
using MasterServer;
using MessagePack;
using Newtonsoft.Json;
using Serilog;
namespace TestClient
{
    internal class TestClientApplication
    {
        protected NetManager netManager;
        protected EventBasedNetListener netListener;
        public static TestClientApplication Instance { get; private set; } = new TestClientApplication();

        private NetPeer client;
        public TestClientApplication()
        {
            netListener = new EventBasedNetListener();
            netListener.PeerConnectedEvent += NetListener_PeerConnectedEvent;
            netListener.PeerDisconnectedEvent += NetListener_PeerDisconnectedEvent;
            netListener.NetworkReceiveEvent += NetListener_NetworkReceiveEvent;

            netManager = new NetManager(netListener);

            netManager.UnsyncedEvents = true;
            netManager.Start();
            client = netManager.Connect("127.0.0.1", 6666, "yoyo");
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
            Log.Information("{0} result: {1} ping {2}", operationCode.ToString(), returnCode.ToString(), peer.Ping);
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

            string tokenEncrypt = SecurityUtil.AESEncrypt(tokenJson, "@qwertyuiop123#$");

            AuthRequest authRequest = new AuthRequest();

            authRequest.Token = tokenEncrypt;

            Send(OperationCode.Auth, MessagePackSerializer.Serialize<AuthRequest>(authRequest));
        }

        public void OnAuth(byte[] data)
        {
            AuthResponse authResponse = MessagePackSerializer.Deserialize<AuthResponse>(data);

            Log.Information("response:" + authResponse.UserID + " " + authResponse.NickName);
        }


        public void JoinLobby(string lobbyname)
        {
            JoinLobbyRequest joinLobbyRequest = new JoinLobbyRequest();
            joinLobbyRequest.UserID = "1111";
            joinLobbyRequest.LobbyName = lobbyname;

            Send(OperationCode.JoinLobby, MessagePackSerializer.Serialize<JoinLobbyRequest>(joinLobbyRequest));
        }

        public void LeaveLobby(string loobyname)
        {
            LeaveLobbyRequest leaveLobbyRequest = new LeaveLobbyRequest();
            leaveLobbyRequest.UserID = "1111";
            leaveLobbyRequest.LobbyName = loobyname;

            Send(OperationCode.LeaveLobby, MessagePackSerializer.Serialize<LeaveLobbyRequest>(leaveLobbyRequest));
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
