using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using static Collectible;
using static DungeonUI;

public class LevelDoor : MonoBehaviour
{
    [SerializeField]
    private int level;
    public float interactionRange;
    private LevelData levelData;


    public bool InRange()
    {
        var playerPos = FindAnyObjectByType<Player>().transform.position;
        Vector3 offset = playerPos - transform.position;
        offset.y = 0;
        return isActiveAndEnabled && offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    private void Awake()
    {
        levelData = DataManager.GetLevelData(level);
    }

    private void Update()
    {
        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        if (interact && InRange())
        {
            Debug.Log($"Interacted to go to Level {level}. Unlocked: {Unlocked()}");
            if (Unlocked())
            {
                Debug.Log("Entering Level...");
                DataManager.EnterLevel(level);
            }
        }


    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    private bool Unlocked()
    {
        if (level == 1 || DataManager.CompletedLevel(level - 1))
        {
            return true;
        }

        return false;
    }

    #region GUI

    // Standing at a door shows that level's loot record and an enter prompt.
    void OnGUI()
    {
        if (!InRange()) return;
        var skin = DungeonUI.ForScreen();
        float ui = skin.ui;
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        GUI.matrix = Matrix4x4.Translate(new Vector3(safe.x, Screen.height - safe.yMax, 0));
        try
        {
            float w = safe.width, h = safe.height;
            float margin = (w < 600 ? 8 : 16) * ui;
            float promptY = h - margin - 40 * ui;
            bool exists = DataManager.Levels.ContainsKey(level);

            float width = Mathf.Min(340 * ui, w - 2 * margin);
            var plaque = new Rect((w - width) * .5f, promptY - 112 * ui, width, 100 * ui);
            Plaque(plaque, ui, .95f, exists && Unlocked() ? Torch : Bronze);
            Text(new Rect(plaque.x, plaque.y + 10 * ui, plaque.width, 26 * ui), "LEVEL " + level, skin.title, Torch);

            if (!exists)
            {
                Text(new Rect(plaque.x, plaque.y + 56 * ui, plaque.width, 22 * ui), "Coming soon...", skin.bodyCenter, Faded);
                return;
            }

            int Got(Kind kind) => levelData.collectedCollectables.Count(c => c.kind == kind);
            int Total(Kind kind) => kind == Kind.Orb || kind == Kind.Crown ? 1 : levelData.totalCollectables.Count(c => c.kind == kind);
            var entries = new[] { (Kind.Gem, "gems", GemColor), (Kind.Bar, "bars", BarColor),
                                  (Kind.Orb, "orb", OrbColor), (Kind.Crown, "crown", CrownColor) };
            float cell = (plaque.width - 24 * ui) / entries.Length;
            float x = plaque.x + 18 * ui;
            foreach (var (kind, label, color) in entries)
            {
                float pip = 8 * ui;
                Fill(new Rect(x, plaque.y + 55 * ui, pip, pip), color);
                Text(new Rect(x + pip + 5 * ui, plaque.y + 48 * ui, cell, 20 * ui), $"{Got(kind)}/{Total(kind)}", skin.body, Parchment);
                Text(new Rect(x, plaque.y + 70 * ui, cell, 16 * ui), label, skin.tiny, Faded);
                x += cell;
            }

            string key = Gamepad.current != null ? "A" : "E";
            if (Unlocked()) Prompt(w, promptY, key, "Enter the level", skin, Torch);
            else
            {
                var locked = new Rect((w - width) * .5f, promptY, width, 40 * ui);
                Plaque(locked, ui, .92f, Blood);
                Text(locked, $"Locked - steal the crown from level {level - 1} first", skin.bodyCenter, Blood);
            }
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
    }
    #endregion
}
