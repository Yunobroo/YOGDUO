using UnityEngine;
using UnityEngine.SceneManagement;

// Progress belongs to the campaign, rather than to any particular dungeon scene.
public static class HeistProgress
{
    public const string HubScene = "ShopHub";
    public const string FirstLevelScene = "GoblinDungeon";
    public const string SecondLevelScene = "GoblinDungeon2";
    const string SaveKey = "GoblinHeist.Progress.v1";

    [System.Serializable]
    class SaveData
    {
        public int bank, boots, stamina, crowns;
        public bool softShoes, levelTwoUnlocked, campaignComplete;
    }

    static SaveData data;
    static SaveData Data
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
    public static bool LevelTwoUnlocked => Data.levelTwoUnlocked;
    public static bool CampaignComplete => Data.campaignComplete;
    public static int Crowns => Data.crowns;
    public static float StaminaDrain => .27f / (1 + StaminaUpgrades * .25f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => data = null;

    public static int EscapeReward(int level, int coins, int gems) => coins + gems * 3 + (level == 2 ? 10 : 5);

    public static void CompleteHeist(int level, int coins, int gems)
    {
        Data.bank += EscapeReward(level, coins, gems);
        Data.crowns++;
        if (level == 1) Data.levelTwoUnlocked = true;
        if (level == 2) Data.campaignComplete = true;
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
        if (level == 2 && !LevelTwoUnlocked) return;
        SceneManager.LoadScene(level == 2 ? SecondLevelScene : FirstLevelScene);
    }

    public static void ReturnToHub() => SceneManager.LoadScene(HubScene);

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }
}
