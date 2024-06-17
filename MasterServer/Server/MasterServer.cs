using LiteNetLib;
using Serilog;
using SharedLibrary;
namespace MasterServer
{
    public class MasterServer
    {
        public static MasterServer Instance { get; private set; } = new MasterServer();

        public Dictionary<int, ClientPeer> ClientPeers { get; private set; } = new Dictionary<int, ClientPeer>();
        public Dictionary<int, GamePeer> GamePeers { get; private set; } = new Dictionary<int, GamePeer> { };
        public MasterConfig? MasterConfig { get; private set; }

        protected NetManager? netManager;

        protected EventBasedNetListener? eventBasedNetListener;

        public void Init(MasterConfig serverConfig)
        {
            MasterConfig = serverConfig;

            eventBasedNetListener = new EventBasedNetListener();

            eventBasedNetListener.ConnectionRequestEvent += OnConnectionRequest;
            eventBasedNetListener.PeerConnectedEvent += OnPeerConnected;
            eventBasedNetListener.PeerDisconnectedEvent += OnPeerDisconnected;
            eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceive;

            netManager = new NetManager(eventBasedNetListener);
            netManager.PingInterval = serverConfig.PingInterval;
            netManager.DisconnectTimeout = serverConfig.DisconnectTimeout;
            netManager.ReconnectDelay = serverConfig.ReconnectDelay;
            netManager.MaxConnectAttempts = serverConfig.MaxConnectAttempts;

            netManager.ChannelsCount = 8;

            MySqlManager.Instance.Init(MasterConfig.SQLConnectionStr);

            InitLog();
        }

        protected virtual void InitLog()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.File("./Log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).WriteTo.Console();
            loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
        }

        public virtual void Start()
        {
            if (MasterConfig == null)
            {
                Log.Error("MasterConfig is Null!!!");

                return;
            }

            HttpServer.Instance.Start();

            Log.Information("连接Redis");

            RedisManager.Instance = new RedisManager(MasterConfig.RedisConnectionStr);

            if (RedisManager.Instance.IsConnected)
            {
                Log.Information("连接Redis成功！！！");
            }
            else
            {
                throw new Exception("Redis连接失败！！！");
            }
            if (netManager != null)
            {
                netManager.Start(MasterConfig.Port);
                Log.Information("start server:{0}", netManager.LocalPort);
            }
        }


        public virtual void Update()
        {
            if (netManager != null)
            {
                netManager.PollEvents();
            }
        }

        protected virtual void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager == null)
            {
                return;
            }

            if (MasterConfig == null)
            {
                return;
            }

            if (netManager.ConnectedPeersCount < MasterConfig.MaxPeers)
            {
                request.AcceptIfKey(MasterConfig.ConnectKey);
            }
        }

        protected virtual void OnPeerConnected(NetPeer peer)
        {
            if (MasterConfig == null)
            {
                return;
            }

            if (MasterConfig.GameIP.Contains(peer.EndPoint.ToString()))
            {
                if (GamePeers.ContainsKey(peer.Id))
                {
                    GamePeers.Remove(peer.Id);
                }

                GamePeer serverPeer = new GamePeer(peer);
                GamePeers.Add(peer.Id, serverPeer);
                Log.Information("GameServer connection: {0}", peer.EndPoint);
            }
            else
            {
                if (ClientPeers.ContainsKey(peer.Id))
                {
                    ClientPeers.Remove(peer.Id);
                }
                ClientPeer clientPeer = new ClientPeer(peer);
                ClientPeers.Add(peer.Id, clientPeer);
                Log.Information("Client connection: {0}", peer.EndPoint);
            }
        }

        protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (GamePeers.ContainsKey(peer.Id))
            {
                GamePeers.Remove(peer.Id);
                Log.Information("GameServer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
            }
            else
            {
                if (ClientPeers.ContainsKey(peer.Id))
                {
                    ClientPeers.Remove(peer.Id);
                    Log.Information("Client disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
                }
            }
        }

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                BasePeer? basePeer = null;

                if (GamePeers.ContainsKey(peer.Id))
                {
                    basePeer = GamePeers[peer.Id];
                }

                if (ClientPeers.ContainsKey(peer.Id))
                {
                    basePeer = ClientPeers[peer.Id];
                }

                if (basePeer != null)
                {
                    OperationCode operationCode = (OperationCode)reader.GetUShort();
                    if (channel == 0)//request
                    {
                        basePeer.OnRequest(operationCode, reader.GetRemainingBytes(), deliveryMethod);

                    }
                    else if (channel == 1)//response
                    {
                        ReturnCode returnCode = (ReturnCode)reader.GetUShort();
                        basePeer.OnResponse(operationCode, returnCode, reader.GetRemainingBytes(), deliveryMethod);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }


        public GamePeer? GetLowerLoadLevelingServer()
        {
            var servers = GamePeers.Values;
            return servers.OrderBy((a) => a.GameInfo.Players).ThenBy((a) => a.GameInfo.Memory).FirstOrDefault();
        }

        public void HotLoad(string version)
        {
            HotManager.Instance.Load(version, true);
            HotLoadRequest request = new HotLoadRequest();
            request.Version = version;

            byte[] data = MessagePack.MessagePackSerializer.Serialize(request);

            for (int i = 0; i < GamePeers.Count; i++)
            {

                GamePeers[i].SendRequest(OperationCode.HotLoad, data, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}