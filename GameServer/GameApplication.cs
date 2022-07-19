﻿using CommonLibrary.Utils;
using GameServer.Operations;
using LiteNetLib;
using Newtonsoft.Json;
using Serilog;

namespace GameServer
{
    internal class GameApplication : Singleton<GameApplication>
    {
        private NetManager server;
        private EventBasedNetListener listener;
        private OperationHandleBase operationHandle;
        public GameServerConfig? ServerConfig { get; }

        public NetPeer? MasterServer;

        private Dictionary<string, NetPeer> clientPeers = new Dictionary<string, NetPeer>();
        private List<Game> games = new List<Game>();
        public GameApplication()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            operationHandle = new OperationHandleBase();
            Log.Information("Load Config");
            try
            {
                string config = File.ReadAllText("./GameServerConfig.json");
                if (!string.IsNullOrEmpty(config))
                    ServerConfig = JsonConvert.DeserializeObject<GameServerConfig>(config);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            };
        }

        public void Start()
        {
            if (ServerConfig == null)
            {
                Log.Error("No Config Loaded!");
                return;
            }
            server.Start(ServerConfig.Port);
            server.PingInterval = 1000;
            server.DisconnectTimeout = 5000;
            server.ReconnectDelay = 500;
            //最大连接尝试次数
            server.MaxConnectAttempts = 10;
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            Log.Information("Start listener Successed");
            Log.Information("Connect To Master");

            MasterServer = server.Connect(ServerConfig.MasterIP, ServerConfig.MasterPort,"");
        }

        public void Close()
        {
            if (server != null)
                server.Stop(true);
        }

        public void Update()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (server != null && server.ConnectedPeersCount < 5000)
            {
                //TODO:Token 
                request.Accept();
            }
            else
            {
                request.Reject();
                Log.Information("Reject Connect:{0}", request.RemoteEndPoint.ToString());
            }
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Log.Information("We got connection: {0} id:{1}", peer.EndPoint, peer.Id);
            clientPeers[peer.EndPoint.ToString()] = peer;
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Information("We got disconnection: {0}", peer.EndPoint);

            if (clientPeers.ContainsKey(peer.EndPoint.ToString()))
            {
                clientPeers.Remove(peer.EndPoint.ToString());
            }
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (clientPeers.ContainsKey(peer.EndPoint.ToString()))
            {
                //ClientPeer clientPeer = clientPeers[peer.EndPoint.ToString()];
                GameOperationCode operationCode = (GameOperationCode)reader.GetByte();
                HandleRequest handleRequest = new HandleRequest(peer, operationCode, reader.GetRemainingBytes());
                operationHandle.HandleRequest(handleRequest);
            }
            else
            {
                Log.Information("Client {0} not connected!", peer.EndPoint.ToString());
            }
        }

        public Game GetOrCreateGame(string id)
        {
            Game? game = games.FirstOrDefault((a) => a.GameID == id);
            if (game == null)
            {
                game = new Game(id);
                games.Add(game);
            }
            return game;
        }
    }
}
