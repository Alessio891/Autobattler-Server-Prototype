using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer_Prototype
{

    public class Client
    {
        public int ClientID;
        public NetPeer peer;
        public bool Authenticated = false;
        public string Username;
    }

    public static class Clients
    {
        static int CurrentClientID = 0;
        static Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public static int AddClient(NetPeer client, string user = "") {

            Client newClient = new Client();
            newClient.peer = client;
            newClient.Username = user;
            foreach (KeyValuePair<int, Client> pair in clients)
            {
                if (pair.Value == null)
                {
                    newClient.ClientID = pair.Key;
                    clients[pair.Key] = newClient;
                    return pair.Key;
                }
            }            
            newClient.ClientID = CurrentClientID;

            clients.Add(CurrentClientID, newClient);
            CurrentClientID++;

            return CurrentClientID-1;
        }

        public static Client GetClient(int index)
        {
            if (clients.ContainsKey(index))
                return clients[index];
            return null;
        }

        public static NetPeer GetPeer(int index)
        {
            if (clients.ContainsKey(index) && clients[index] != null)
                return clients[index].peer;

            return null;
        }

        public static void DisposeClient(NetPeer client)
        {
            int clientId = GetClientIndex(client);
            DisposeClient(clientId);
        }

        public static void DisposeClient(int clientId)
        {
            if (clientId != -1)
            {
                if (clients.ContainsKey(clientId))
                {
                    clients[clientId] = null;
                }
            }
        }

        public static int GetClientIndex(NetPeer client)
        {
            foreach(KeyValuePair<int, Client> pair in clients)
            {
                if (pair.Value != null && pair.Value.peer == client)
                    return pair.Key;
            }
            return -1;
        }
    }
}
