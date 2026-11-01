using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// Progress belongs to the campaign, rather than to any particular dungeon scene.
public static class HeistProgress
{
    public const string HubScene = "ShopHub";
    public static Dictionary<int, string> Levels = new Dictionary<int, string>{
    { 0, "Shop" },
    { 1, "Level_1" },
    { 2, "GoblinDungeon2" }
    };
    const string SaveKey = "GoblinHeist.Progress.v1";

    [System.Serializable]
    public class SaveData
    {
        public int bank, boots, stamina, crowns;
        public bool softShoes, campaignComplete;

        public List<int> completedLevels = new List<int>();
        public List<int> levelsWithOrbCollected = new List<int>();
    }

    public static SaveData data;
    public static SaveData Data
    {
        get
        {
            if (data == null)
            {
                try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "{}")); }
                catch { data = new SaveData(); }
                if (data == null) data = new SaveData();
                data.bank = Mathf.Max(0, data.bank);
                data.boots = Mathf.Clamp(data.boots, 0, 2);
                data.stamina = Mathf.Clamp(data.stamina, 0, 2);
            }
            return data;
        }
    }

    public static int Bank => Data.bank;
    public static int Boots => Data.boots;
    public static int StaminaUpgrades => Data.stamina;
    public static bool SoftShoes => Data.softShoes;
    public static bool CampaignComplete => Data.campaignComplete;
    public static int Crowns => Data.crowns;
    public static float StaminaDrain => .27f / (1 + StaminaUpgrades * .25f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => data = null;

    public static int EscapeReward(int level, int coins, int gems, int bars) => 
        coins + 
        gems * 3 + 
        bars * 5 +
        (level == 2 ? 10 : 5);

    public static void CompleteHeist(int level, int coins, int gems, int bars, int orb)
    {
        Data.bank += EscapeReward(level, coins, gems, bars);
        Data.crowns++;
        if (level == 2) Data.campaignComplete = true;
        Data.completedLevels.Add(level);
        if(orb > 0) Data.levelsWithOrbCollected.Add(level);
        Save();
    }

    public static bool BuyBoots()
    {
        if (Bank < 5 || Boots >= 2) return false;
        Data.bank -= 5;
        Data.boots++;
        Save();
        return true;
    }

    public static bool BuySoftShoes()
    {
        if (Bank < 4 || SoftShoes) return false;
        Data.bank -= 4;
        Data.softShoes = true;
        Save();
        return true;
    }

    public static bool BuyStamina()
    {
        if (Bank < 6 || StaminaUpgrades >= 2) return false;
        Data.bank -= 6;
        Data.stamina++;
        Save();
        return true;
    }

    public static void EnterLevel(int level)
    {
        SceneManager.LoadScene(Levels[level]);
    }

    public static void ReturnToHub() => SceneManager.LoadScene(HubScene);

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }
}
