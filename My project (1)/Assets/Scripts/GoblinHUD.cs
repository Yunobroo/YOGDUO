using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static Collectible;
using static DungeonUI;

// Dungeon-styled HUD: a loot plaque, a small objective banner, a stealth status widget,
// a stamina bar under the goblin while it recovers, and a controls card shown on level entry
// (H / View brings it back). Mechanics are taught by one-time contextual hints.
public class GoblinHUD : MonoBehaviour
{
    public GoblinDungeon game;
    [Tooltip("Seconds the controls card stays up after entering a level.")]
    public float introDuration = 7f;

    GUIStyle tiny, small, body, bodyCenter, heading, title, button;
    DungeonUI.Skin skin;
    float ui = 1;

    float staminaAlpha;
    bool introDismissed, showControls;
    string lastObjective;
    float objectiveChangedAt = -10;
    bool hasHidden, spottedBefore, crownBefore;
    string hintText;
    float hintUntil;

    bool Pad => Gamepad.current != null;
    bool IntroVisible => !introDismissed && Time.timeSinceLevelLoad < introDuration + 1f;

    void Update()
    {
        if (game == null || game.isHub) return;
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        if ((keys != null && keys.hKey.wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame))
        {
            if (IntroVisible) introDismissed = true;
            else showControls = !showControls;
        }
        if (!game.Playing) { introDismissed = true; showControls = false; }

        var player = game.player;
        float stamina = player == null ? 1 : player.Stamina;
        // Appears as soon as sprinting drains stamina, fades once it has refilled.
        bool draining = stamina < .995f && game.Playing;
        staminaAlpha = Mathf.MoveTowards(staminaAlpha, draining ? 1 : 0, Time.unscaledDeltaTime * (draining ? 6f : 1.5f));

        UpdateHints(player);
    }

    void UpdateHints(Player player)
    {
        if (player == null || !game.Playing || IntroVisible) return;
        if (player.Hidden) hasHidden = true;
        else if (!hasHidden && game.InHidingSpot(player.transform.position))
            Hint(Pad ? "Click R3 to crouch and hide here" : "Crouch (Ctrl / C) to hide here", .2f);

        if (!spottedBefore && game.guards.Any(g => g != null && g.CanSeeTarget))
        {
            spottedBefore = true;
            Hint("Spotted! Break their line of sight, then hide in a green spot", 4.5f);
        }
        if (!crownBefore && game.CarryingCrown)
        {
            crownBefore = true;
            Hint(DataManager.Data.sprintWhileCarrying
                ? "Got the crown! It shields you from one blow - reach the green exit"
                : "The crown is heavy - no sprinting. It shields you from one blow", 5f);
        }
    }

    void Hint(string text, float seconds)
    {
        hintText = text;
        hintUntil = Mathf.Max(hintUntil, Time.time + seconds);
    }

    // ---------- styling ----------

    void Styles()
    {
        skin = DungeonUI.ForScreen();
        ui = skin.ui;
        tiny = skin.tiny; small = skin.small; body = skin.body; bodyCenter = skin.bodyCenter;
        heading = skin.heading; title = skin.title; button = skin.button;
    }

    void Plaque(Rect r, float alpha = 1, Color? trim = null) => DungeonUI.Plaque(r, ui, alpha, trim);

    // ---------- drawing ----------

    void OnGUI()
    {
        if (game == null || game.isHub) return;
        Styles();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        bool oldEnabled = GUI.enabled;
        try
        {
            // The stamina bar follows the goblin in raw screen space.
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            DrawStamina();

            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            // Anchor panels to the safe area at native pixel size.
            GUI.matrix = Matrix4x4.Translate(new Vector3(safe.x, Screen.height - safe.yMax, 0));
            float w = safe.width, h = safe.height;
            float margin = (w < 600 ? 8 : 16) * ui;

            Rect loot = DrawLoot(margin);
            DrawObjective(w, margin, loot);
            DrawStatus(w, h, margin);
            DrawHint(w, h, margin);
            DrawControls(w, h, margin);
            if (!game.Playing) EndScreen(w, h);
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; }
    }

    void DrawStamina()
    {
        var player = game.player;
        var cam = Camera.main;
        if (player == null || cam == null || staminaAlpha <= .01f) return;
        Vector3 screen = cam.WorldToScreenPoint(player.transform.position);
        if (screen.z < 0) return;
        float stamina = Mathf.Clamp01(player.Stamina);
        float bw = 60 * ui, bh = 6 * ui;
        var bar = new Rect(screen.x - bw * .5f, Screen.height - screen.y + 12 * ui, bw, bh);
        float a = staminaAlpha;
        Fill(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), new Color(0, 0, 0, .75f * a));
        Fill(bar, A(new Color(.18f, .12f, .08f), a));
        bool low = stamina < .3f;
        Color fill = low ? Color.Lerp(Blood, Torch, .5f + .5f * Mathf.Sin(Time.time * 12)) : Moss;
        Fill(new Rect(bar.x, bar.y, bar.width * stamina, bar.height), A(fill, a));
        // Notch where sprint becomes available again after running dry.
        Fill(new Rect(bar.x + bar.width * .3f, bar.y, 1, bar.height), A(Parchment, .5f * a));
    }

    Rect DrawLoot(float margin)
    {
        // An empty record keeps the HUD drawing even if the save has no entry for this level.
        var levelData = DataManager.GetLevelData(game.levelNumber) ?? new LevelData();
        int Count(Kind kind) => game.collectedCollectables.Count(c => c.kind == kind) +
            (kind == Kind.Coin ? 0 : levelData.collectedCollectables.Count(c => c.kind == kind));
        int Total(Kind kind) => kind == Kind.Coin ? game.TotalCoins
            : kind == Kind.Orb ? 1 : levelData.totalCollectables.Count(c => c.kind == kind);

        var entries = new[] { (Kind.Coin, "coins", CoinColor), (Kind.Gem, "gems", GemColor),
                              (Kind.Bar, "bars", BarColor), (Kind.Orb, "orb", OrbColor) };
        float cell = 62 * ui;
        var r = new Rect(margin, margin, cell * entries.Length + cell * 1.1f + 16 * ui, 52 * ui);
        Plaque(r);
        float x = r.x + 12 * ui;
        foreach (var (kind, label, color) in entries)
        {
            float pip = 8 * ui;
            Fill(new Rect(x, r.y + 15 * ui, pip, pip), color);
            Text(new Rect(x + pip + 5 * ui, r.y + 8 * ui, cell, 20 * ui), $"{Count(kind)}/{Total(kind)}", body, Parchment);
            Text(new Rect(x, r.y + 29 * ui, cell, 16 * ui), label, tiny, Faded);
            x += cell;
        }
        Fill(new Rect(x - 6 * ui, r.y + 10 * ui, 1, r.height - 20 * ui), A(Bronze, .6f));
        Text(new Rect(x + 4 * ui, r.y + 8 * ui, cell, 20 * ui), DataManager.Data.bank.ToString(), body, CoinColor);
        Text(new Rect(x + 4 * ui, r.y + 29 * ui, cell, 16 * ui), "purse", tiny, Faded);
        return r;
    }

    void DrawObjective(float w, float margin, Rect loot)
    {
        string objective = game.Objective;
        if (objective != lastObjective) { lastObjective = objective; objectiveChangedAt = Time.time; }
        if (IntroVisible) return; // the intro card already states it

        float width = Mathf.Min(460 * ui, w - 2 * (loot.xMax + margin));
        float y = margin;
        if (width < 240 * ui) { width = w - 2 * margin; y = loot.yMax + 8 * ui; }
        var r = new Rect((w - width) * .5f, y, width, 34 * ui);
        // Flash the trim when the objective changes.
        float flash = Mathf.Clamp01(1 - (Time.time - objectiveChangedAt) / 2.5f);
        Plaque(r, .9f, Color.Lerp(Bronze, Torch, flash));
        Text(r, objective, bodyCenter, Color.Lerp(Parchment, Torch, flash));
    }

    void DrawStatus(float w, float h, float margin)
    {
        var player = game.player;
        string status = "UNSEEN";
        Color color = Faded;
        if (game.guards.Any(g => g != null && g.Investigating)) { status = "GUARDS SUSPICIOUS"; color = Torch; }
        if (game.guards.Any(g => g != null && g.CanSeeTarget))
        {
            status = "SPOTTED!";
            color = Color.Lerp(Blood, Parchment, .25f + .25f * Mathf.Sin(Time.time * 10));
        }
        if (player != null && player.Hidden) { status = "HIDDEN"; color = Moss; }

        string sub = game.HitProtected ? "Struck! The crown fell - grab it or run!"
            : game.CarryingCrown ? "The crown shields you from one blow" : null;

        float width = 300 * ui;
        float height = (sub == null ? 40 : 60) * ui;
        var r = new Rect((w - width) * .5f, h - margin - height, width, height);
        Plaque(r, .9f, status == "SPOTTED!" ? Blood : status == "HIDDEN" ? Moss : Bronze);

        float eye = 10 * ui;
        Fill(new Rect(r.x + 14 * ui, r.y + 15 * ui, eye, eye), color);
        Text(new Rect(r.x + 32 * ui, r.y + 10 * ui, 180 * ui, 22 * ui), status, heading, color);

        // Noise meter: four rising bars, like a sound wave.
        float noise = player == null ? 0 : player.NoiseRadius;
        for (int i = 0; i < 4; i++)
        {
            float bh = (5 + i * 4) * ui;
            var bar = new Rect(r.xMax - 70 * ui + i * 9 * ui, r.y + 28 * ui - bh, 6 * ui, bh);
            Fill(bar, noise > i * 1.75f ? Torch : new Color(.22f, .18f, .14f));
        }
        Text(new Rect(r.xMax - 70 * ui, r.y + 28 * ui, 60 * ui, 14 * ui), noise == 0 ? "silent" : "noise", tiny, Faded);

        if (sub != null)
            Text(new Rect(r.x + 14 * ui, r.y + 38 * ui, r.width - 28 * ui, 18 * ui), sub, small,
                game.HitProtected ? Blood : Torch);
    }

    void DrawHint(float w, float h, float margin)
    {
        if (string.IsNullOrEmpty(hintText) || !game.Playing) return;
        float alpha = Mathf.Clamp01((hintUntil - Time.time) * 3);
        if (alpha <= 0) return;
        float width = Mathf.Min(520 * ui, w - 2 * margin);
        var r = new Rect((w - width) * .5f, h - margin - 100 * ui, width, 30 * ui);
        Fill(new Rect(r.x + width * .15f, r.y, width * .7f, r.height), new Color(0, 0, 0, .45f * alpha));
        Text(r, hintText, bodyCenter, Parchment, alpha);
    }

    void DrawControls(float w, float h, float margin)
    {
        float alpha = showControls ? 1 : 0;
        if (IntroVisible) alpha = Mathf.Clamp01(introDuration + 1f - Time.timeSinceLevelLoad);

        if (alpha <= 0)
        {
            // A quiet reminder of where the controls went.
            Text(new Rect(w - margin - 120 * ui, h - margin - 18 * ui, 120 * ui, 18 * ui),
                Pad ? "View  ·  Controls" : "H  ·  Controls", small, Faded, .7f);
            return;
        }

        (string key, string action)[] rows = Pad
            ? new[] { ("Left stick", "Sneak through the dark"), ("L3", "Sprint (drains stamina)"),
                      ("R3", "Crouch - quieter, harder to spot"), ("Crouch in green", "Hide from guards"),
                      ("View", "Show / hide controls") }
            : new[] { ("W A S D", "Move"), ("Space", "Sprint (drains stamina)"),
                      ("Ctrl / C", "Crouch - quieter, harder to spot"), ("Crouch in green", "Hide from guards"),
                      ("R", "Restart run"), ("H", "Show / hide controls") };

        float width = Mathf.Min(420 * ui, w - 2 * margin);
        float row = 26 * ui;
        float top = 118 * ui;
        var r = new Rect((w - width) * .5f, Mathf.Max(margin, h * .18f), width, top + rows.Length * row + 40 * ui);
        Plaque(r, .95f * alpha, Torch);

        Text(new Rect(r.x, r.y + 12 * ui, r.width, 16 * ui), "LEVEL " + game.levelNumber, bodyCenter, Faded, alpha);
        Text(new Rect(r.x, r.y + 30 * ui, r.width, 38 * ui), game.levelTitle, title, Torch, alpha);
        Text(new Rect(r.x + 20 * ui, r.y + 72 * ui, r.width - 40 * ui, 22 * ui), game.Objective, bodyCenter, Parchment, alpha);
        Fill(new Rect(r.x + r.width * .2f, r.y + 102 * ui, r.width * .6f, 1), A(Bronze, alpha));

        float keyWidth = 118 * ui;
        for (int i = 0; i < rows.Length; i++)
        {
            float y = r.y + top + i * row;
            var cap = new Rect(r.x + 24 * ui, y, keyWidth, row - 6 * ui);
            KeyCap(cap, rows[i].key, skin, alpha);
            Text(new Rect(cap.xMax + 14 * ui, y + 2 * ui, r.width - keyWidth - 60 * ui, row), rows[i].action, body, Parchment, alpha);
        }
        string footer = IntroVisible ? (Pad ? "Press View to start sneaking" : "Press H to start sneaking")
                                     : (Pad ? "Press View to close" : "Press H to close");
        Text(new Rect(r.x, r.yMax - 30 * ui, r.width, 20 * ui), footer, bodyCenter, Faded, alpha);
    }

    void EndScreen(float width, float height)
    {
        Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, .7f));
        float panelWidth = Mathf.Min(420 * ui, width - 16);
        float panelHeight = Mathf.Min(300 * ui, height - 16);
        var panel = new Rect((width - panelWidth) * .5f, (height - panelHeight) * .5f, panelWidth, panelHeight);
        Plaque(panel, 1, game.CaughtPlayer ? Blood : Torch);
        float x = panel.x + 20 * ui, contentWidth = panel.width - 40 * ui;
        Text(new Rect(panel.x, panel.y + 16 * ui, panel.width, 38 * ui),
            game.CaughtPlayer ? "Caught!" : "Escaped with the crown!", title, game.CaughtPlayer ? Blood : Torch);
        string result = game.CaughtPlayer ? "Your unbanked loot was lost. Upgrades and banked coins are safe."
                : "Banked " +
                //game.EscapeReward +
                " coins (including crown bounty). "
                + (game.levelNumber == 1 ? "Level 2 unlocked!" : "Both crowns stolen! Replay either heist from the hub.");
        Text(new Rect(x, panel.y + 62 * ui, contentWidth, 44 * ui), result, bodyCenter, Parchment);
        float rowHeight = Mathf.Min(40 * ui, (panel.height - 125 * ui) / 3);
        float y = panel.y + 112 * ui;
        GUI.color = Color.white;
        if (GUI.Button(new Rect(x, y, contentWidth, rowHeight), "Return to the shop hub", button)) DataManager.EnterLevel(0);
        if (game.Won && game.levelNumber == 1 &&
            GUI.Button(new Rect(x, y + rowHeight + 8 * ui, contentWidth, rowHeight), "Enter level 2 - The Royal Vault", button))
            DataManager.EnterLevel(2);
        if (GUI.Button(new Rect(x, panel.yMax - rowHeight - 18 * ui, contentWidth, rowHeight), "Try another run", button)) game.Restart();
    }
}
