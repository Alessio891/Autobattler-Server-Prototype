#define DISABLE_TRYCATCH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using Network;

namespace GameServer_Prototype
{
    public class Server
    {
        public static Server instance;

        EventBasedNetListener Listener;
        public NetManager netManager;
        public NetPacketProcessor netProcessor;

        public Server()
        {
            ServerConsole.Log("Setting up server...");
            Listener = new EventBasedNetListener();
            netManager = new NetManager(Listener);
            netProcessor = new NetPacketProcessor();

            netProcessor.RegisterNestedType<PlayerMatchData>(PlayerMatchData.Serialize, PlayerMatchData.Deserialize);
            netProcessor.RegisterNestedType<MinionMatchData>(MinionMatchData.Serialize, MinionMatchData.Deserialize);

            Listener.ConnectionRequestEvent += OnConnectionRequest;
            Listener.PeerConnectedEvent += OnConnectionAccepted;
            Listener.PeerDisconnectedEvent += OnClientDisconnect;
            Listener.NetworkReceiveEvent += OnNetworkReceive;
            SetupPacketProcessor();
            ServerConsole.Log("Server set up done.");
            instance = this;
        }

        private void SetupPacketProcessor() {
            netProcessor.SubscribeReusable<AuthenticationRequest>(Authentication.AuthenticationRequestHandler);
            netProcessor.SubscribeReusable<JoinLobbyRequestPacket>(OnJoinLobbyRequest);

            netProcessor.SubscribeReusable<PlayerReadyRequest>(MatchData.OnPlayerReady);
            netProcessor.SubscribeReusable<MinionBuyRequest>(MatchData.BuyMinionRequest);
        }

        #region Event Callbacks
        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            netProcessor.ReadAllPackets(reader);
        }

        private void OnClientDisconnect(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ServerConsole.Log(string.Format("{0} disconnected", peer.EndPoint));
            Clients.DisposeClient(peer);
        }

        private void OnConnectionAccepted(NetPeer peer)
        {
            ServerConsole.Log(string.Format("Connection accepted for {0}", peer.EndPoint));
            int clientId = Clients.AddClient(peer);
            netProcessor.Send(peer, new ConnectionAttemptResult() { ClientID = clientId, Accepted = true, Message = "Accepted" }, DeliveryMethod.ReliableOrdered);
            ServerConsole.Log(string.Format("Assigned ID {0} to {1}", clientId, peer.EndPoint));
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {            
            request.Accept();
        }

        private void OnJoinLobbyRequest(JoinLobbyRequestPacket packet) {
            Client client = Clients.GetClient(packet.ClientID);
            ServerConsole.Log("Requested match from user " + client.Username + " [id: " + client.ClientID.ToString() + "]");
            MatchData assignedMatch = MatchData.GetFirstAvailableMatch(client);
        }
        #endregion

        public void Start(int port)
        {
            netManager.SimulateLatency = true;
            netManager.SimulationMinLatency = 1000;
            netManager.SimulationMaxLatency = 3500;            
            netManager.Start(port);
            ServerConsole.Log("Server started on port " + port.ToString());
        }

        public void Stop()
        {
            netManager.Stop();
        }

        public void PollEvents()
        {
            netManager.PollEvents();
        }

        public static void SendPacket<T>(int clientId, T packet) where T : class, new()
        {
#if DISABLE_TRYCATCH
            Client c = Clients.GetClient(clientId);
            instance.netProcessor.Send<T>(c.peer, packet, DeliveryMethod.ReliableOrdered);
#else
            try
            {
                Client c = Clients.GetClient(clientId);
                instance.netProcessor.Send<T>(c.peer, packet, DeliveryMethod.ReliableOrdered);
            } catch (System.Exception e)
            {
                string message = string.Format("Couldn't send packet {0} to client with id {1}.\n {2} {3}",
                    packet.GetType().ToString(), clientId, e.Message, e.StackTrace);
                ServerConsole.LogError(message);
            }
#endif
        }
    }
}
