using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static DungeonUI;

// Title screen of the TitleScreen scene (first in the build). Continue / Start loads the hub.
// Menus work with mouse, keyboard and controller through MenuNav.
public class TitleScreen : MonoBehaviour
{
    const string GameName = "SNEAKY SNATCHER";
    const string Tagline = "He's not stealing. He's borrowing. Forever.";

    enum Page { Main, Controls, ConfirmNewGame }

    readonly MenuNav nav = new MenuNav();
    Page page = Page.Main;
    float alpha = 1, closingSince = -1;
    GUIStyle logo;
    float logoUi;

    static bool HasProgress =>
        DataManager.Data.bank > 0 || DataManager.Data.boots > 0 || DataManager.Data.stamina > 0 ||
        DataManager.Data.softShoes || DataManager.Data.sprintWhileCarrying ||
        DataManager.Data.levels.Exists(l => l.completed || l.collectedCollectables.Count > 0);

    void Start() => nav.Open();

    void Update()
    {
        if (closingSince >= 0)
        {
            // Fade to black, then load the level. Stays black until the new scene takes over.
            if (closingSince == float.MaxValue) return;
            alpha = Mathf.Max(0, 1 - (Time.unscaledTime - closingSince) / .5f);
            if (alpha <= 0) { closingSince = float.MaxValue; DataManager.EnterLevel(destination); }
            return;
        }
        nav.Tick();
        if (nav.Back && page != Page.Main) Show(Page.Main);
    }

    void Show(Page next)
    {
        page = next;
        nav.Open();
    }

    // A new game starts straight in Level 1; continuing returns to the hub (level 0).
    int destination;
    void Close(int level)
    {
        destination = level;
        closingSince = Time.unscaledTime;
    }

    void OnGUI()
    {
        var skin = ForScreen();
        float ui = skin.ui, w = Screen.width, h = Screen.height;
        float column = Mathf.Min(w * .46f, 640 * ui), left = w * .05f;
        if (logo == null || logoUi != ui || logoColumn != column)
        {
            logoUi = ui;
            logoColumn = column;
            // Biggest title that still fits the column on one line, so it never wraps into the tagline.
            logo = new GUIStyle(skin.title) { fontSize = Mathf.RoundToInt(72 * ui), wordWrap = false };
            while (logo.fontSize > 18 && logo.CalcSize(new GUIContent(GameName)).x > column) logo.fontSize -= 2;
            tagline = new GUIStyle(skin.bodyCenter) { fontSize = Mathf.RoundToInt(18 * ui), wordWrap = true };
            menuButton = new GUIStyle(skin.button) { fontSize = Mathf.RoundToInt(22 * ui), wordWrap = false };
            dangerButton = new GUIStyle(skin.dangerButton) { fontSize = Mathf.RoundToInt(22 * ui), wordWrap = false };
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        try
        {
            // The goblin owns the right side of the scene; a soft dark gradient on the left keeps the menu readable.
            for (int i = 0; i < 10; i++) Fill(new Rect(0, 0, w * (.6f - i * .04f), h), new Color(0, 0, 0, .07f));

            // Everything below is drawn in a column on the left; the page layouts centre within it.
            GUI.matrix = Matrix4x4.Translate(new Vector3(left, 0, 0));

            // Stack title, tagline and menu by their real heights so nothing overlaps.
            float top = h * .1f;
            float logoH = logo.CalcSize(new GUIContent(GameName)).y;
            Text(new Rect(0, top, column, logoH), GameName, logo, Torch, alpha);
            float y = top + logoH + 6 * ui;
            float taglineH = tagline.CalcHeight(new GUIContent(Tagline), column);
            Text(new Rect(0, y, column, taglineH), Tagline, tagline, Parchment, alpha);
            y += taglineH + 14 * ui;
            Fill(new Rect(column * .5f - 90 * ui, y, 180 * ui, 1), A(Bronze, alpha));

            if (closingSince < 0)
            {
                GUI.color = new Color(1, 1, 1, alpha);
                nav.Begin();
                float menuTop = y + 24 * ui;
                if (page == Page.Main) MainMenu(skin, column, menuTop);
                else if (page == Page.Controls) ControlsPage(skin, column, menuTop);
                else ConfirmPage(skin, column, menuTop);
                nav.End();
            }

            // Fade the whole screen to black on the way out.
            GUI.matrix = Matrix4x4.identity;
            if (alpha < 1) Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, 1 - Mathf.Clamp01(alpha)));
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
    }

    GUIStyle centerSmall, tagline, menuButton, dangerButton, cell;
    float logoColumn, cellWidth;
    GUIStyle CenterSmall(Skin skin) => centerSmall ??= new GUIStyle(skin.small) { alignment = TextAnchor.MiddleCenter };

    // Big, chunky menu buttons: wide and tall, sized to the column.
    static void ButtonSize(Skin skin, float w, out float bw, out float bh, out float gap)
    {
        bw = Mathf.Min(w * .85f, 440 * skin.ui);
        bh = 62 * skin.ui;
        gap = 14 * skin.ui;
    }

    void MainMenu(Skin skin, float w, float y)
    {
        float ui = skin.ui;
        ButtonSize(skin, w, out float bw, out float bh, out float gap);
        bool progress = HasProgress;
        int rows = progress ? 4 : 3;
        var panel = new Rect((w - bw) * .5f - 24 * ui, y, bw + 48 * ui, rows * (bh + gap) + 36 * ui);
        Plaque(panel, ui, .95f);
        float x = (w - bw) * .5f, row = y + 18 * ui;

        if (nav.Button(new Rect(x, row, bw, bh), progress ? "Continue" : "Start the heist", menuButton, ui)) Close(progress ? 0 : 1);
        row += bh + gap;
        if (progress)
        {
            if (nav.Button(new Rect(x, row, bw, bh), "New game", menuButton, ui)) Show(Page.ConfirmNewGame);
            row += bh + gap;
        }
        if (nav.Button(new Rect(x, row, bw, bh), "Controls", menuButton, ui)) Show(Page.Controls);
        row += bh + gap;
        if (nav.Button(new Rect(x, row, bw, bh), "Quit", menuButton, ui)) Quit();
    }

    void ConfirmPage(Skin skin, float w, float y)
    {
        float ui = skin.ui;
        ButtonSize(skin, w, out float bw, out float bh, out float gap);
        var panel = new Rect((w - bw) * .5f - 24 * ui, y, bw + 48 * ui, 2 * (bh + gap) + 110 * ui);
        Plaque(panel, ui, .95f, Blood);
        Text(new Rect(panel.x, y + 16 * ui, panel.width, 30 * ui), "Start over?", tagline, Blood);
        Text(new Rect(panel.x + 16 * ui, y + 50 * ui, panel.width - 32 * ui, 36 * ui),
            "All coins, loot and upgrades will be lost.", CenterSmall(skin), Parchment);
        float x = (w - bw) * .5f, row = y + 96 * ui;
        if (nav.Button(new Rect(x, row, bw, bh), "No, keep my progress", menuButton, ui)) Show(Page.Main);
        row += bh + gap;
        if (nav.Button(new Rect(x, row, bw, bh), "Yes, wipe and start over", dangerButton, ui))
        {
            DataManager.ResetAllProgress();
            Close(1); // fresh save: straight into the first heist
        }
    }

    void ControlsPage(Skin skin, float w, float y)
    {
        float ui = skin.ui;
        (string action, string keys, string pad)[] rows =
        {
            ("Move", "W A S D / arrows", "Left stick"),
            ("Sprint", "Hold Space", "Hold R1 / RB"),
            ("Crouch / sneak", "C (toggle) / hold Ctrl", "Circle / B"),
            ("Hide", "Crouch in a green spot", "Crouch in a green spot"),
            ("Talk / enter door", "E", "Cross / A"),
            ("Back / close", "Esc", "Circle / B"),
            ("Restart run", "R", "-"),
            ("Controls card", "H", "View"),
        };
        float width = Mathf.Min(640 * ui, w), rowH = 32 * ui, bh = 56 * ui;
        var panel = new Rect((w - width) * .5f, y, width, 60 * ui + rows.Length * rowH + bh + 32 * ui);
        Plaque(panel, ui, .95f);
        // Three columns of fixed width; the cell font shrinks until the longest entry fits its column,
        // so entries never run into the next column.
        float pad0 = 20 * ui, colW = (width - pad0 * 2) / 3f - 8 * ui;
        float c1 = panel.x + pad0, c2 = c1 + colW + 12 * ui, c3 = c2 + colW + 12 * ui;
        if (cell == null || cellWidth != colW)
        {
            cellWidth = colW;
            cell = new GUIStyle(skin.body) { fontSize = Mathf.RoundToInt(17 * ui), wordWrap = false, clipping = TextClipping.Clip };
            while (cell.fontSize > 10 && System.Array.Exists(rows, r =>
                       cell.CalcSize(new GUIContent(r.action)).x > colW || cell.CalcSize(new GUIContent(r.keys)).x > colW ||
                       cell.CalcSize(new GUIContent(r.pad)).x > colW))
                cell.fontSize--;
        }
        float row = y + 18 * ui;
        Text(new Rect(c2, row, colW, 24 * ui), "KEYBOARD", skin.heading, Torch);
        Text(new Rect(c3, row, colW, 24 * ui), "CONTROLLER", skin.heading, Torch);
        row += 38 * ui;
        foreach (var (action, keys, pad) in rows)
        {
            Text(new Rect(c1, row, colW, rowH), action, cell, Parchment);
            Text(new Rect(c2, row, colW, rowH), keys, cell, Faded);
            Text(new Rect(c3, row, colW, rowH), pad, cell, Faded);
            row += rowH;
        }
        float bw = Mathf.Min(width * .6f, 260 * ui);
        if (nav.Button(new Rect(panel.center.x - bw * .5f, panel.yMax - bh - 16 * ui, bw, bh), "Back", menuButton, ui)) Show(Page.Main);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
