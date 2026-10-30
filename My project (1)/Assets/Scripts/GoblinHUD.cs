using UnityEngine;

public class GoblinHUD : MonoBehaviour
{
    public GoblinDungeon game;
    GUIStyle text, hint, right, center, title, button;
    int styledHeight;

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

    static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Panel(Rect rect)
    {
        Fill(new Rect(rect.x + 2, rect.y + 3, rect.width, rect.height), new Color(0, 0, 0, .2f));
        Fill(rect, new Color(.10f, .11f, .12f, .92f));
        Fill(new Rect(rect.x, rect.y, rect.width, 1), new Color(.35f, .37f, .36f, .55f));
    }

    void OnGUI()
    {
        if (game == null || game.isHub) return;
        Styles();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        bool oldEnabled = GUI.enabled;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        // Anchor each panel to a safe-area corner at native pixel size.
        GUI.matrix = Matrix4x4.Translate(new Vector3(safe.x, Screen.height - safe.yMax, 0));
        GUI.color = Color.white;
        try
        {
            float w = safe.width, h = safe.height;
            float margin = w < 600 ? 8 : 16;
            float side = Mathf.Min(Screen.height > 1000 ? 320 : 280, (w - margin * 3) * .5f);
            float controlsX = w - margin - side;
            var player = game.player;

            Panel(new Rect(margin, margin, side, 100));

            float columnWidth = (side - 24) / 4f;

            GUI.Label(new Rect(margin + 12, margin + 8, columnWidth, 22), "Coins", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth, margin + 8, columnWidth, 22), "Gems", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth * 2, margin + 8, columnWidth, 22), "Gold Bars", hint);
            GUI.Label(new Rect(margin + 12 + columnWidth * 3, margin + 8, columnWidth, 22), "Orb", hint);

            GUI.Label(new Rect(margin + 12, margin + 31, columnWidth, 26), game.Coins + " / " + game.TotalCoins, title);
            GUI.Label(new Rect(margin + 12 + columnWidth, margin + 31, columnWidth, 26), game.Gems + " / " + game.TotalGems, title);
            GUI.Label(new Rect(margin + 12 + columnWidth * 2, margin + 31, columnWidth, 26), game.Bars + " / " + game.TotalBars, title);
            GUI.Label(new Rect(margin + 12 + columnWidth * 3, margin + 31, columnWidth, 26), game.Orbs + " / " + game.TotalOrbs, title);

            Fill(new Rect(margin + 12, margin + 63, side - 24, 1), new Color(.3f, .32f, .32f, .6f));

            GUI.Label(new Rect(margin + 12, margin + 72, side - 24, 22), "Purse: " + game.Bank + " coins", hint);

            Panel(new Rect(controlsX, margin, side, 100));
            bool pad = UnityEngine.InputSystem.Gamepad.current != null;
            GUI.Label(new Rect(controlsX + 12, margin + 8, side - 24, 22), "CONTROLS", hint);
            GUI.Label(new Rect(controlsX + 12, margin + 33, side - 24, 62), pad
                ? "Left stick    Move\nL3    Sprint   /   R3    Crouch"
                : "WASD    Move   /   Space    Sprint\nCtrl / C    Sneak   /   R    Restart", hint);

            float bottom = h - margin - 82;
            float meterWidth = side;
            float meterX = margin;
            float stamina = player == null ? 1 : player.Stamina;
            Panel(new Rect(meterX, bottom, meterWidth, 82));
            GUI.Label(new Rect(meterX + 12, bottom + 8, meterWidth - 24, 22), "Stamina", hint);
            GUI.Label(new Rect(meterX + 12, bottom + 8, meterWidth - 24, 22), Mathf.RoundToInt(stamina * 100) + "%", right);
            Fill(new Rect(meterX + 12, bottom + 35, meterWidth - 24, 7), new Color(.22f, .24f, .24f));
            Fill(new Rect(meterX + 12, bottom + 35, (meterWidth - 24) * Mathf.Clamp01(stamina), 7), stamina < .3f ? new Color(.8f, .57f, .28f) : new Color(.55f, .75f, .4f));
            float noise = player == null ? 0 : player.NoiseRadius;
            string protection = game.HitProtected ? "Hit! Crown dropped - get away!"
                : game.CarryingCrown ? "2 lives - crown protection ready" : "1 life - steal the crown for protection";
            GUI.Label(new Rect(meterX + 12, bottom + 51, meterWidth - 24, 23), protection, hint);
            Panel(new Rect(controlsX, bottom, side, 82));
            GUI.Label(new Rect(controlsX + 12, bottom + 51, side - 74, 23), noise == 0 ? "Silent footsteps" : "Footsteps: " + noise.ToString("0.0") + "m", hint);
            for (int i = 0; i < 4; i++)
                Fill(new Rect(controlsX + side - 60 + i * 11, bottom + 57, 7, 8), noise > i * 1.75f ? new Color(.85f, .65f, .32f) : new Color(.25f, .27f, .27f));

            string status = "";
            foreach (var guard in game.guards)
                if (guard != null && guard.Investigating) status = "Guards investigating";
            foreach (var guard in game.guards)
                if (guard != null && guard.CanSeeTarget) status = "SPOTTED - RUN!";
            if (player != null && player.Hidden) status = "Hidden";
            var statusRect = new Rect(controlsX + 12, bottom + 8, side - 24, 32);
            Color statusColor = status == "SPOTTED - RUN!" ? new Color(1, .5f, .4f) : status == "Hidden" ? new Color(.65f, .85f, .5f) : Color.white;
            GUI.color = statusColor;
            GUI.Label(statusRect, status.Length > 0 ? status : "Stay out of sight", center);
            GUI.color = Color.white;
            GUI.Label(new Rect(margin, margin + 110, w - margin * 2, 30), "LEVEL " + game.levelNumber + " / " + game.levelTitle + "   -   " + game.Objective, center);
            GUI.color = Color.white;
            if (!game.Playing)
            {
                GUI.matrix = Matrix4x4.Translate(new Vector3(safe.x, Screen.height - safe.yMax, 0));
                EndScreen(safe.width, safe.height);
            }
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; }
    }

    void EndScreen(float width, float height)
    {
        Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, .65f));
        float panelWidth = Mathf.Min(390, width - 16);
        float panelHeight = Mathf.Min(290, height - 16);
        var panel = new Rect((width - panelWidth) * .5f, (height - panelHeight) * .5f, panelWidth, panelHeight);
        Fill(panel, new Color(.09f, .1f, .11f, .98f));
        float x = panel.x + 16, contentWidth = panel.width - 32;
        GUI.Label(new Rect(x, panel.y + 12, contentWidth, 28), game.CaughtPlayer ? "Caught" : "Escaped with the crown!", title);
        string result = game.CaughtPlayer ? "Your unbanked loot was lost. Upgrades and banked coins are safe."
            : "Banked " + game.EscapeReward + " coins (including crown bounty). "
                + (game.levelNumber == 1 ? "Level 2 unlocked!" : "Both crowns stolen! Replay either heist from the hub.");
        GUI.Label(new Rect(x, panel.y + 44, contentWidth, 44), result, hint);
        float rowHeight = Mathf.Min(42, (panel.height - 115) / 3);
        if (GUI.Button(new Rect(x, panel.y + 88, contentWidth, rowHeight), "Return to the shop hub", button)) game.OpenShop();
        if (game.Won && game.levelNumber == 1 && GUI.Button(new Rect(x, panel.y + 96 + rowHeight, contentWidth, rowHeight), "Enter level 2 - The Royal Vault", button)) HeistProgress.EnterLevel(2);
        if (GUI.Button(new Rect(x, panel.yMax - rowHeight - 16, contentWidth, rowHeight), "Try another run", button)) game.Restart();
    }
}
