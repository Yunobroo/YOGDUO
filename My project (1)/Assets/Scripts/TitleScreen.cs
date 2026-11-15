using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static DungeonUI;

// Title screen of the TitleScreen scene (first in the build). Continue / Start loads the hub.
// Menus work with mouse, keyboard and controller through MenuNav.
public class TitleScreen : MonoBehaviour
{
    const string GameName = "SNEAKY SNATCHER";
    const string Tagline = "Sneak in. Steal the crown. Get out.";

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
            // Fade out, then load the hub.
            alpha = 1 - (Time.unscaledTime - closingSince) / .4f;
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
        if (logo == null || logoUi != ui)
        {
            logoUi = ui;
            logo = new GUIStyle(skin.title) { fontSize = Mathf.RoundToInt(64 * ui) };
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        try
        {
            // Darken the hub behind, heavier at the top and bottom like torchlight fading out.
            Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, .62f * alpha));
            Fill(new Rect(0, 0, w, h * .18f), new Color(0, 0, 0, .25f * alpha));
            Fill(new Rect(0, h * .82f, w, h * .18f), new Color(0, 0, 0, .25f * alpha));

            float top = h * .14f;
            Text(new Rect(0, top, w, 80 * ui), GameName, logo, Torch, alpha);
            Text(new Rect(0, top + 82 * ui, w, 24 * ui), Tagline, skin.bodyCenter, Parchment, alpha);
            Fill(new Rect(w * .5f - 90 * ui, top + 116 * ui, 180 * ui, 1), A(Bronze, alpha));

            if (closingSince >= 0) return; // no menu while fading out
            GUI.color = new Color(1, 1, 1, alpha);
            nav.Begin();
            float menuTop = top + 150 * ui;
            if (page == Page.Main) MainMenu(skin, w, menuTop);
            else if (page == Page.Controls) ControlsPage(skin, w, menuTop);
            else ConfirmPage(skin, w, menuTop);
            nav.End();

            bool pad = Gamepad.current != null;
            Text(new Rect(0, h - 40 * ui, w, 20 * ui),
                pad ? "D-pad / stick to choose  ·  A to select  ·  B to go back"
                    : "Arrows or mouse to choose  ·  Enter to select  ·  Esc to go back",
                CenterSmall(skin), Faded, alpha);
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
    }

    GUIStyle centerSmall;
    GUIStyle CenterSmall(Skin skin) => centerSmall ??= new GUIStyle(skin.small) { alignment = TextAnchor.MiddleCenter };

    void MainMenu(Skin skin, float w, float y)
    {
        float ui = skin.ui, bw = 300 * ui, bh = 44 * ui, gap = 12 * ui;
        bool progress = HasProgress;
        int rows = progress ? 4 : 3;
        var panel = new Rect((w - bw) * .5f - 24 * ui, y, bw + 48 * ui, rows * (bh + gap) + 36 * ui);
        Plaque(panel, ui, .95f);
        float x = (w - bw) * .5f, row = y + 18 * ui;

        if (nav.Button(new Rect(x, row, bw, bh), progress ? "Continue" : "Start the heist", skin.button, ui)) Close(progress ? 0 : 1);
        row += bh + gap;
        if (progress)
        {
            if (nav.Button(new Rect(x, row, bw, bh), "New game", skin.button, ui)) Show(Page.ConfirmNewGame);
            row += bh + gap;
        }
        if (nav.Button(new Rect(x, row, bw, bh), "Controls", skin.button, ui)) Show(Page.Controls);
        row += bh + gap;
        if (nav.Button(new Rect(x, row, bw, bh), "Quit", skin.button, ui)) Quit();
    }

    void ConfirmPage(Skin skin, float w, float y)
    {
        float ui = skin.ui, bw = 300 * ui, bh = 44 * ui, gap = 12 * ui;
        var panel = new Rect((w - bw) * .5f - 24 * ui, y, bw + 48 * ui, 2 * (bh + gap) + 96 * ui);
        Plaque(panel, ui, .95f, Blood);
        Text(new Rect(panel.x, y + 16 * ui, panel.width, 24 * ui), "Start over?", skin.bodyCenter, Blood);
        Text(new Rect(panel.x + 16 * ui, y + 42 * ui, panel.width - 32 * ui, 36 * ui),
            "All coins, loot and upgrades will be lost.", CenterSmall(skin), Parchment);
        float x = (w - bw) * .5f, row = y + 84 * ui;
        if (nav.Button(new Rect(x, row, bw, bh), "No, keep my progress", skin.button, ui)) Show(Page.Main);
        row += bh + gap;
        if (nav.Button(new Rect(x, row, bw, bh), "Yes, wipe and start over", skin.dangerButton, ui))
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
        float width = Mathf.Min(620 * ui, w - 32), rowH = 26 * ui;
        float bh = 40 * ui;
        var panel = new Rect((w - width) * .5f, y, width, 56 * ui + rows.Length * rowH + bh + 28 * ui);
        Plaque(panel, ui, .95f);
        float c1 = panel.x + 24 * ui, c2 = panel.x + width * .36f, c3 = panel.x + width * .68f;
        float row = y + 16 * ui;
        Text(new Rect(c2, row, width * .3f, 22 * ui), "KEYBOARD", skin.heading, Torch);
        Text(new Rect(c3, row, width * .3f, 22 * ui), "CONTROLLER", skin.heading, Torch);
        row += 34 * ui;
        foreach (var (action, keys, pad) in rows)
        {
            Text(new Rect(c1, row, width * .3f, rowH), action, skin.body, Parchment);
            Text(new Rect(c2, row, width * .32f, rowH), keys, skin.body, Faded);
            Text(new Rect(c3, row, width * .3f, rowH), pad, skin.body, Faded);
            row += rowH;
        }
        float bw = 200 * ui;
        if (nav.Button(new Rect(panel.center.x - bw * .5f, panel.yMax - bh - 16 * ui, bw, bh), "Back", skin.button, ui)) Show(Page.Main);
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
