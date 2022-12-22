using GameServer.Operation;
using LiteNetLib;
using MasterServer.Utils;
using MessagePack;
using Serilog;
using SharedLibrary.Model;
using SharedLibrary.Operation;
using SharedLibrary.Server;
using System.Net;

namespace GameServer
{
    internal class GameServer : ServerBase
    {
        public static GameServer Instance { get; set; }
        public GameConfig GameConfig { get; private set; }
        public Dictionary<int, GamePeer> GamePeers = new Dictionary<int, GamePeer>();
        public OperationHandler OperationHandler { get; private set; } = new OperationHandler();

        public SystemInfo SystemInfo { get; private set; } = new SystemInfo();

        public GameServer(GameConfig serverConfig) : base(serverConfig)
        {
            GameConfig = serverConfig;
        }

        public async void RegisterToMasterServer(int timeout)
        {
            NetPeer peer = netManager.Connect(IPEndPoint.Parse(GameConfig.MasterIPEndPoint), GameConfig.ConnectKey);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);
            await Task.Run(() =>
            {
                while (peer.ConnectionState != ConnectionState.Connected && !cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
            }, cancellationTokenSource.Token);

            if (peer.ConnectionState == ConnectionState.Connected)
            {
                GamePeers[peer.Id].SendRequest(OperationCode.GameServerRegister, null, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Log.Error("server register failed");
                System.Environment.Exit(0);
            }
        }

        public void OnRegisterToMasterSuccess(GamePeer gamePeer)
        {
            Task.Run(() =>
            {
                while (gamePeer.Peer.ConnectionState == ConnectionState.Connected)
                {
                    ServerInfo serverInfo = new ServerInfo();
                    serverInfo.CPUPercent = SystemInfo.GetCPUPercent();
                    serverInfo.MemoryPercent = SystemInfo.GetMemoryPercent();

                    byte[] data = MessagePackSerializer.Serialize(serverInfo);

                    gamePeer.SendRequest(OperationCode.UpdateServerState, data, DeliveryMethod.ReliableOrdered);

                    Thread.Sleep(1000);
                }
            });
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            if (netManager.ConnectedPeersCount < ServerConfig.MaxPeers)
                request.AcceptIfKey(ServerConfig.ConnectKey);
            else
                request.Reject();
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            GamePeers[peer.Id] = new GamePeer(peer);
            Log.Information("peer connection: {0}", peer.EndPoint);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            GamePeers.Remove(peer.Id);
            Log.Information("peer disconnection: {0} info: {1}", peer.EndPoint, disconnectInfo.Reason.ToString());
        }

        protected override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                OperationType operationType = (OperationType)reader.GetByte();
                OperationCode operationCode = (OperationCode)reader.GetByte();
                switch (operationType)
                {
                    case OperationType.Request:
                        OperationHandler.OnRequest(operationCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
                        break;
                    case OperationType.Response:
                        ReturnCode returnCode = (ReturnCode)reader.GetByte();
                        OperationHandler.OnResponse(operationCode, returnCode, GamePeers[peer.Id], reader.GetRemainingBytes(), deliveryMethod);
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
