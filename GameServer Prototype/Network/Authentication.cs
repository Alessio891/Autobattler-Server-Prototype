using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using Network;
using MongoDB.Driver;
using MongoDB.Bson;

namespace GameServer_Prototype
{
    public static class Authentication
    {
        public static void AuthenticationRequestHandler(AuthenticationRequest packet) {

            string user = packet.Username;
            string hashedPass = packet.HashedWord;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(hashedPass))
            {
                NetDataWriter nw = new NetDataWriter();
                nw.Put(ResponseCodes.BAD_LOGIN);
                nw.Put("User and password can't be empty");
                Server.instance.netManager.DisconnectPeer(Clients.GetPeer(packet.ClientID), nw);
                return;
            }

            MongoClient client = Database.GetClient();

            var filter = Builders<BsonDocument>.Filter.Eq("user", user);
            var coll = client.GetDatabase(Database.DB_NAME).GetCollection<BsonDocument>(Database.AUTH_TABLE);
            var doc = coll.Find(filter).FirstOrDefault();

            if (doc == null)
            {
                NetDataWriter nw = new NetDataWriter();
                nw.Put(ResponseCodes.BAD_LOGIN);
                nw.Put("User not found. Registered new user.");
                Server.instance.netManager.DisconnectPeer(Clients.GetPeer(packet.ClientID), nw);

                BsonDocument newEntry = new BsonDocument();
                newEntry.AddRange(new Dictionary<string,string>()
                {
                    {"user", user},
                    {"pwd", hashedPass }
                });

                coll.InsertOne(newEntry);

                return;
            }

            if (doc.GetElement("pwd").Value.AsString == hashedPass)
            {
                Client c = Clients.GetClient(packet.ClientID);
                c.Authenticated = true;
                c.Username = user;
                Server.instance.netProcessor.Send(Clients.GetPeer(packet.ClientID), new AuthenticationResult() { Result = true, Message = "Login success" }, DeliveryMethod.ReliableOrdered);
                
            } else
            {
                NetDataWriter nw = new NetDataWriter();
                nw.Put(ResponseCodes.BAD_LOGIN);
                nw.Put("Username or password incorrect");
                Server.instance.netManager.DisconnectPeer(Clients.GetPeer(packet.ClientID), nw);
            }            
        }
    }
}
