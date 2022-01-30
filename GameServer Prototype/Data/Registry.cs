using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Network;
using System.Threading.Tasks;

namespace GameServer_Prototype
{
    public static class Registry
    {

        static Dictionary<string, MinionDataStructure> MinionData;
        static Dictionary<string, GeneralDataStructure> GeneralData;
        static Dictionary<string, ResearchDataStructure> ResearchData;

        public static void Initialize()
        {
            ServerConsole.Log("Initializing Registry");

            MinionData = new Dictionary<string, MinionDataStructure>();
            GeneralData = new Dictionary<string, GeneralDataStructure>();
            ResearchData = new Dictionary<string, ResearchDataStructure>();

            string json = System.IO.File.ReadAllText("JSON Data/Minions/DebugMinions.json");
            Dictionary<string, object> minions = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach(KeyValuePair<string, object> minion in minions)
            {
                MinionData.Add(minion.Key, MinionDataStructure.FromJson(minion.Value.ToString(), minion.Key));
            }
            ServerConsole.Log(minions.Keys.Count.ToString() + " minions loaded.");
            json = System.IO.File.ReadAllText("JSON Data/Generals/DebugGenerals.json");
            Dictionary<string, object> generals = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach(KeyValuePair<string, object> general in generals)
            {
                GeneralData.Add(general.Key, GeneralDataStructure.FromJson(general.Value.ToString(), general.Key));
            }
            ServerConsole.Log(generals.Keys.Count.ToString() + " generals loaded.");

            json = System.IO.File.ReadAllText("JSON Data/Research/DebugResearch.json");
            Dictionary<string, object> research = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (KeyValuePair<string, object> r in research)
            {
                ResearchData.Add(r.Key, ResearchDataStructure.FromJson(r.Value.ToString(), r.Key));
            }
            ServerConsole.Log(generals.Keys.Count.ToString() + " research loaded.");


            ServerConsole.LogWarning("Registry initialized");
        }

       public static MinionDataStructure GetMinion(string id)
        {
            if (MinionData.ContainsKey(id))
                return MinionData[id];

            return null;
        }

        public static List<MinionDataStructure> GetAllMinions()
        {
            return MinionData.Values.ToList();
        }

    }
}
