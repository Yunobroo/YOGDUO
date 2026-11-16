using System.Collections.Generic;
using UnityEngine;

[System.Serializable]                   
public class SaveData                           //This actually gets saved
{
    public int bank, boots, stamina;
    public bool softShoes, campaignComplete, sprintWhileCarrying;

    public List<LevelData> levels;
}

[System.Serializable]
public class LevelData                          //This is a container to help store data for the SaveData class
{
    public int level;
    public string name;
    public List<ItemData> collectedCollectables = new List<ItemData>();
    public List<ItemData> totalCollectables = new List<ItemData>();
    public bool completed = false;
}


[System.Serializable]
public class ItemData                      //This is also a container to help te LevelData class
{
    public string id;
    public Collectible.Kind kind;
}
