using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using static Collectible;

public class LevelDoor : MonoBehaviour
{
    [SerializeField]
    private int level;
    public float interactionRange;
    private LevelData levelData;


    GUIStyle text, hint, right, center, title, button;
    int styledHeight;


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
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && InRange())
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

    void OnGUI()
    {
        if (InRange())
        {
            Styles();
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            GUI.matrix = Matrix4x4.Translate(new Vector3(safe.x, Screen.height - safe.yMax, 0));
            GUI.color = Color.white;
            float w = safe.width, h = safe.height;
            float margin = w < 600 ? 8 : 16;
            float side = Mathf.Min(Screen.height > 1000 ? 320 : 280, (w - margin * 3) * .5f);
            float controlsX = w - margin - side;
            var player = FindAnyObjectByType<Player>().transform.position;

            Panel(new Rect(margin, margin, side, 100));

            float columnWidth = (side - 24) / 4f;

            GUI.Label(new Rect(margin + 12, margin + 8, columnWidth, 22), "Gems", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth, margin + 8, columnWidth, 22), "Gold Bars", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth * 2, margin + 8, columnWidth, 22), "Orb", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth * 3, margin + 8, columnWidth, 22), "Crown", hint);

            GUI.Label(new Rect(margin + 12, margin + 31, columnWidth, 26),
                $"{levelData.collectedCollectables.Count(collectible => collectible.kind == Kind.Gem)}" +
                $" / {levelData.totalCollectables.Count(collectible => collectible.kind == Kind.Gem)}"
                , title);

            GUI.Label(new Rect(margin + 12 + columnWidth, margin + 31, columnWidth, 26),
                $"{levelData.collectedCollectables.Count(collectible => collectible.kind == Kind.Bar)}" +
                $" / {levelData.totalCollectables.Count(collectible => collectible.kind == Kind.Bar)}"
                , title);

            GUI.Label(new Rect(margin + 12 + columnWidth * 2, margin + 31, columnWidth, 26),
                $"{levelData.collectedCollectables.Count(collectible => collectible.kind == Kind.Orb)}" +
                $" / 1"
                , title);

            GUI.Label(new Rect(margin + 12 + columnWidth * 3, margin + 31, columnWidth, 26),
                $"{levelData.collectedCollectables.Count(collectible => collectible.kind == Kind.Crown)}" +
                " / 1"
                , title);

            GUI.Label(new Rect(margin + 12, margin + 72, side - 24, 22), "Level: " + level, title);
        }
    }

    

    private void Panel(Rect rect)
    {
        Fill(new Rect(rect.x + 2, rect.y + 3, rect.width, rect.height), new Color(0, 0, 0, .2f));
        Fill(rect, new Color(.10f, .11f, .12f, .92f));
        Fill(new Rect(rect.x, rect.y, rect.width, 1), new Color(.35f, .37f, .36f, .55f));
    }

    private void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Styles()
    {
        if (text != null && styledHeight == Screen.height) return;
        styledHeight = Screen.height;
        float fontScale = Mathf.Clamp(Screen.height / 900f, 1f, 1.25f);
        text = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(16 * fontScale) };
        text.normal.textColor = Color.white;
        hint = new GUIStyle(text) { fontSize = Mathf.RoundToInt(13 * fontScale), wordWrap = true };
        hint.normal.textColor = new Color(.78f, .8f, .8f);
        right = new GUIStyle(hint) { alignment = TextAnchor.UpperRight };
        center = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        title = new GUIStyle(text) { fontSize = Mathf.RoundToInt(18 * fontScale), fontStyle = FontStyle.Bold };
        button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14 * fontScale), wordWrap = true };
    }
    #endregion
}
