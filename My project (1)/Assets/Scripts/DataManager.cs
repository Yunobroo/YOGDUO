using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class DataManager
{
    private const string SaveKey = "GoblinHeist.Progress.v1";

    public static Dictionary<int, string> Levels = new Dictionary<int, string>
    {
        { 0, "ShopHub_0" },
        { 1, "Level_1" },
        { 2, "Level_2" }
    };

    private static SaveData data;

    public static SaveData Data { 
        get 
        {
            if (data == null) 
                Load();

            return data;
        }
    }

    // Load the save data the first time it is needed.
    private static void EnsureLoaded()
    {
        if (Data != null)
            return;

        Load();
    }

    public static void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);

            data = JsonUtility.FromJson<SaveData>(json);
        }

        if (data == null)
            data = new SaveData();

        if (data.levels == null)
            data.levels = new List<LevelData>();

        var changed = false;

        foreach (var level in Levels)
        {
            LevelData levelData = data.levels.FirstOrDefault(item => item.level == level.Key);

            if (levelData == null)
            {
                levelData = new LevelData
                {
                    level = level.Key,
                    name = level.Value,
                    collectedCollectables = new List<CollectedItem>()
                };

                data.levels.Add(levelData);

                changed = true;
            }

            if (levelData.collectedCollectables == null)
            {
                levelData.collectedCollectables =
                    new List<CollectedItem>();

                changed = true;
            }
        }

        if (changed)
            Save();
    }

    public static void Save()
    {
        EnsureLoaded();

        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static LevelData GetLevelData(int level)
    {
        EnsureLoaded();

        return data.levels.FirstOrDefault(
            levelData => levelData.level == level
        );
    }

    private static LevelData GetOrCreateLevelData(int level)
    {
        EnsureLoaded();

        LevelData levelData = GetLevelData(level);

        if (levelData == null)
        {
            levelData = new LevelData
            {
                level = level
            };

            data.levels.Add(levelData);
        }

        return levelData;
    }

    public static bool IsCollected(int level, Collectible collectible)
    {
        LevelData levelData = GetLevelData(level);

        if (levelData == null)
            return false;

        return levelData.collectedCollectables.Any(data => data.id == collectible.Id);
    }

    public static void Collect(int levelId, Collectible collectible)
    {
        LevelData levelData = GetOrCreateLevelData(levelId);

        // Avoid adding the same collectible multiple times.
        if (levelData.collectedCollectables.Any(data => data.id == collectible.Id))
            return;

        levelData.collectedCollectables.Add(new CollectedItem { id = collectible.Id, kind = collectible.kind });
        Debug.Log($"Collect {collectible.kind} {collectible.Id}");
    }

    public static void CompleteLevel(int level, List<Collectible> collected)
    {
        Debug.Log("Level complete (Free play unlocked)");
        LevelData levelData = GetOrCreateLevelData(level);

        levelData.completed = true;

        foreach (Collectible collectable in collected)
        {
            switch (collectable.kind)
            {
                case Collectible.Kind.Coin:
                    data.bank += 1; break;
                case Collectible.Kind.Gem:
                    data.bank += 3; break;
                case Collectible.Kind.Bar:
                    data.bank += 5; break;
            }

            if (collectable.kind == Collectible.Kind.Coin) continue; //Don't track coins
            DataManager.Collect(level, collectable);
        }
        Save();
    }

    public static void EnterLevel(int level)
    {
        if (!Levels.TryGetValue(level, out string sceneName))
        {
            Debug.LogError($"Level {level} does not exist.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void ResetLevel(int levelId)
    {
        EnsureLoaded();

        data.levels.RemoveAll(level => level.level == levelId);

        Save();
    }

    public static void ResetAllProgress()
    {
        data = new SaveData();

        Save();
    }

    public static bool CompletedLevel(int levelNr)
    {
        return GetLevelData(levelNr)?.completed ?? false;
    }
}
