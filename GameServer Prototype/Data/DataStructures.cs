using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class MinionDataStructure
{
    public string ID;
    public string Name, Description, Sprite, Graphic, Type;    

    public int Attack, MaxHP, Tier;

    public static MinionDataStructure FromJson(string json, string id)
    {
        MinionDataStructure retVal = JsonConvert.DeserializeObject<MinionDataStructure>(json);
        retVal.ID = id;
        return retVal;
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class GeneralDataStructure
{
    public string ID;
    public string Name, Description, Sprite;

    public static GeneralDataStructure FromJson(string json, string id)
    {
        GeneralDataStructure retVal = JsonConvert.DeserializeObject<GeneralDataStructure>(json);
        retVal.ID = id;
        return retVal;
    }
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class ResearchDataStructure
{
    public string ID;
    public string Name, Sprite;
    public int Tier;

    public static ResearchDataStructure FromJson(string json, string id)
    {
        ResearchDataStructure retVal = JsonConvert.DeserializeObject<ResearchDataStructure>(json);
        retVal.ID = id;
        return retVal;
    }
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}