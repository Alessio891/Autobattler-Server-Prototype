using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer_Prototype
{
    public static class Database
    {
        public static string DB_NAME     = "AutoBattlerPrototype",
                             AUTH_TABLE  = "Auth";


        public static string DB_ADDRESS = "mongodb://localhost:27017";

        public static MongoClient GetClient()
        {
            MongoClient client = new MongoClient(DB_ADDRESS);

            return client;
        }

    }
}
