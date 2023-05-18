using GameServer.Master;
using GameServer.Operation;
using LiteNetLib;
using Serilog;
using SharedLibrary.Operation;
using SharedLibrary.Room;
using SharedLibrary.Server;
using System.Net;

namespace GameServer.Server
{
    internal class GameServer : ServerBase
    {
        public static GameServer Instance { get; set; }

        public Dictionary<int, GamePeer> GamePeers { get; private set; }

        public MasterPeer MasterPeer { get; private set; }

        public GameConfig GameConfig { get; private set; }

        public HashSet<Room> Rooms { get; private set; }

        private OperationHandler operationHandler;

        public GameServer(GameConfig gameConfig) : base(gameConfig)
        {
            GameConfig = gameConfig;
            GamePeers = new Dictionary<int, GamePeer>();
            Rooms = new HashSet<Room>();

            operationHandler = new OperationHandler();
        }

        public override void Start()
        {
            base.Start();
            _ = RegisterToMaster();
        }

        /// <summary>
        /// 注册游戏服务器
        /// </summary>
        private async Task RegisterToMaster()
        {
            IPEndPoint iPEndPoint = IPEndPoint.Parse(GameConfig.MasterIPEndPoint);
            NetPeer netPeer = netManager.Connect(iPEndPoint, GameConfig.ConnectKey);

            while (netPeer.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(100);
            }

            MasterPeer = new MasterPeer(netPeer);

            MasterPeer.RegisterToMaster();

            Log.Information("注册到Master服务器：" + GameConfig.MasterIPEndPoint);
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            GamePeers[peer.Id] = new GamePeer(peer);
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (GamePeers.ContainsKey(peer.Id))
            {
                GamePeers.Remove(peer.Id);
            }
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                ServerOperationCode serverOperationCode;
                OperationCode operationCode;
                ReturnCode returnCode;
                switch (channel)
                {
                    case 0:
                        serverOperationCode = (ServerOperationCode)reader.GetByte();
                        operationHandler.OnServerRequest(serverOperationCode, MasterPeer, reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 1:
                        serverOperationCode = (ServerOperationCode)reader.GetByte();
                        returnCode = (ReturnCode)reader.GetByte();
                        operationHandler.OnServerResponse(serverOperationCode, returnCode, MasterPeer, reader.GetRemainingBytes(), deliveryMethod);
                        break;

                    case 2:
                        operationCode = (OperationCode)reader.GetByte();
                        operationHandler.OnClientRequest(operationCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case 3:
                        operationCode = (OperationCode)reader.GetByte();
                        returnCode = (ReturnCode)reader.GetByte();
                        operationHandler.OnClientResponse(operationCode, returnCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("peer receive error: {0}", ex.Message);
            }
        }
    }
}
