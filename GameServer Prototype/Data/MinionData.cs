using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer_Prototype
{
    public class MinionData
    {
        public string UID, Name, Description;

        public int Attack, MaxHP, Tier;

        public BsonDocument Serialize()
        {
            return null;
        }

        public void Deserialize(BsonDocument doc)
        { }
    }
}
