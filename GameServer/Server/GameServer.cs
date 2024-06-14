using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using SharedLibrary;
using SharedLibrary.Utils;
using System.Net;

namespace GameServer
{
    internal class GameServer
    {
        public static GameServer Instance { get; private set; } = new GameServer();

        public Dictionary<int, ClientPeer> ClientPeers { get; private set; } = new Dictionary<int, ClientPeer>();
        public MasterPeer? MasterPeer { get; private set; }
        public GameConfig? GameConfig { get; private set; }

        public SystemInfo? SystemInfo { get; private set; }

        protected NetManager? netManager;
        protected EventBasedNetListener? eventBasedNetListener;


        public void Init(GameConfig serverConfig)
        {
            GameConfig = serverConfig;

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

            netManager.ChannelsCount = 4;

            MySqlManager.Instance.Init(GameConfig.SQLConnectionStr);

            SystemInfo = new SystemInfo();

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
            if (GameConfig == null)
            {
                Log.Error("MasterConfig is Null!!!");

                return;
            }

            Log.Information("连接Redis");

            RedisManager.Instance = new RedisManager(GameConfig.RedisConnectionStr);

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
                netManager.Start(GameConfig.Port);
                Log.Information("start server:{0}", netManager.LocalPort);


                IPEndPoint iPEndPoint = IPEndPoint.Parse(GameConfig.MasterIP);
                netManager.Connect(iPEndPoint, GameConfig.ConnectKey);
                Log.Information("connect master server:{0}", GameConfig.MasterIP);
            }
        }


        public virtual void Update()
        {
            if (netManager != null)
            {
                netManager.PollEvents();

                RoomManager.Instance.Update();
            }
        }

        protected virtual void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager == null)
            {
                return;
            }

            if (GameConfig == null)
            {
                return;
            }

            if (netManager.ConnectedPeersCount < GameConfig.MaxPeers)
            {
                request.AcceptIfKey(GameConfig.ConnectKey);
            }
        }

        protected virtual void OnPeerConnected(NetPeer peer)
        {
            if (GameConfig == null)
            {
                return;
            }

            if (GameConfig.MasterIP == peer.EndPoint.ToString())
            {
                MasterPeer = new MasterPeer(peer);
                Log.Information("MasterServer connection: {0}", peer.EndPoint);
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
            if (MasterPeer != null && MasterPeer.NetPeer == peer)
            {
                MasterPeer = null;
                Log.Information("MasterServer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
            }
            else
            {
                if (ClientPeers.ContainsKey(peer.Id))
                {
                    ClientPeer clientPeer = ClientPeers[peer.Id];

                    RoomManager.Instance.RemoveOfflinePlayer(clientPeer);

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

                if (MasterPeer != null && MasterPeer.NetPeer == peer)
                {
                    basePeer = MasterPeer;
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

    }
}