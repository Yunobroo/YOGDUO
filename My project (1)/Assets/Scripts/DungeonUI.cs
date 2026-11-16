using UnityEngine;

// Shared look for every IMGUI screen: soot-black stone plaques with bronze trim,
// torch-lit serif text, moss for safety and blood for danger.
public static class DungeonUI
{
    public static readonly Color Stone = new Color(.07f, .058f, .045f, .9f);
    public static readonly Color Bronze = new Color(.46f, .33f, .17f, .95f);
    public static readonly Color Torch = new Color(.95f, .67f, .28f);
    public static readonly Color Parchment = new Color(.91f, .85f, .71f);
    public static readonly Color Faded = new Color(.63f, .56f, .45f);
    public static readonly Color Moss = new Color(.56f, .8f, .36f);
    public static readonly Color Blood = new Color(.92f, .3f, .22f);
    public static readonly Color CoinColor = new Color(.95f, .78f, .3f);
    public static readonly Color GemColor = new Color(.35f, .85f, .85f);
    public static readonly Color BarColor = new Color(.86f, .58f, .22f);
    public static readonly Color OrbColor = new Color(.68f, .45f, .95f);
    public static readonly Color CrownColor = new Color(1f, .85f, .35f);

    public class Skin
    {
        public float ui;
        public GUIStyle tiny, small, body, bodyCenter, heading, title, keyCap, button, dangerButton;
    }

    static Font font;
    static bool fontTried;
    static Texture2D buttonTex, buttonHoverTex, buttonOffTex;
    static Skin screenSkin, fixedSkin;
    static int screenSkinHeight;

    // Sized for native pixels; grows a little on tall screens.
    public static Skin ForScreen()
    {
        if (screenSkin == null || screenSkinHeight != Screen.height)
        {
            screenSkinHeight = Screen.height;
            screenSkin = Build(Mathf.Clamp(Screen.height / 900f, 1f, 1.5f));
        }
        return screenSkin;
    }

    // For screens drawn on a fixed canvas that GUI.matrix already scales.
    public static Skin Fixed() => fixedSkin ??= Build(1f);

    static Skin Build(float ui)
    {
        if (!fontTried)
        {
            fontTried = true;
            try { font = Font.CreateDynamicFontFromOSFont(new[] { "Book Antiqua", "Palatino Linotype", "Georgia", "Times New Roman" }, 16); }
            catch { font = null; }
        }
        GUIStyle Make(float size, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(size * ui), alignment = anchor, fontStyle = style,
                wordWrap = true, clipping = TextClipping.Overflow, padding = new RectOffset(0, 0, 0, 0)
            };
            if (font != null) s.font = font;
            s.normal.textColor = Color.white; // tinted per call through GUI.color
            return s;
        }

        if (buttonTex == null) buttonTex = Solid(new Color(.16f, .11f, .07f));
        if (buttonHoverTex == null) buttonHoverTex = Solid(new Color(.3f, .19f, .09f));
        if (buttonOffTex == null) buttonOffTex = Solid(new Color(.1f, .085f, .07f));
        var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15 * ui), wordWrap = true };
        if (font != null) button.font = font;
        button.normal.background = buttonTex;
        button.hover.background = button.active.background = button.focused.background = buttonHoverTex;
        button.onNormal.background = buttonOffTex;
        button.normal.textColor = Parchment;
        button.hover.textColor = button.active.textColor = button.focused.textColor = Torch;
        var danger = new GUIStyle(button);
        danger.normal.textColor = Blood;
        danger.hover.textColor = danger.active.textColor = danger.focused.textColor = new Color(1f, .5f, .4f);

        return new Skin
        {
            ui = ui,
            tiny = Make(11), small = Make(13), body = Make(15),
            bodyCenter = Make(15, TextAnchor.MiddleCenter),
            heading = Make(17, TextAnchor.MiddleLeft, FontStyle.Bold),
            title = Make(30, TextAnchor.MiddleCenter, FontStyle.Bold),
            keyCap = Make(12, TextAnchor.MiddleCenter, FontStyle.Bold),
            button = button, dangerButton = danger
        };
    }

    static Texture2D Solid(Color color)
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    public static Color A(Color c, float alpha) { c.a *= alpha; return c; }

    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Outline(Rect r, Color c, float t = 1)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    // A stone plaque with a double bronze trim and torch-lit corner rivets.
    public static void Plaque(Rect r, float ui, float alpha = 1, Color? trim = null)
    {
        Color edge = trim ?? Bronze;
        Fill(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, .4f * alpha));
        Fill(r, A(Stone, alpha));
        Outline(r, A(edge, alpha));
        Outline(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), A(edge, .3f * alpha));
        float rivet = Mathf.Round(3 * ui);
        Fill(new Rect(r.x + 5, r.y + 5, rivet, rivet), A(Torch, .55f * alpha));
        Fill(new Rect(r.xMax - 5 - rivet, r.y + 5, rivet, rivet), A(Torch, .55f * alpha));
        Fill(new Rect(r.x + 5, r.yMax - 5 - rivet, rivet, rivet), A(Torch, .55f * alpha));
        Fill(new Rect(r.xMax - 5 - rivet, r.yMax - 5 - rivet, rivet, rivet), A(Torch, .55f * alpha));
    }

    public static void Text(Rect r, string s, GUIStyle style, Color c, float alpha = 1)
    {
        Color old = GUI.color;
        GUI.color = new Color(0, 0, 0, .85f * alpha * c.a);
        GUI.Label(new Rect(r.x + 1, r.y + 1.5f, r.width, r.height), s, style);
        GUI.color = A(c, alpha);
        GUI.Label(r, s, style);
        GUI.color = old;
    }

    public static void KeyCap(Rect r, string key, Skin skin, float alpha = 1)
    {
        Fill(r, A(new Color(.16f, .11f, .07f), alpha));
        Outline(r, A(Bronze, alpha));
        Text(r, key, skin.keyCap, Torch, alpha);
    }

    // "[E] Talk to the Fence" style prompt, centred near the bottom of the screen.
    public static void Prompt(float screenWidth, float y, string key, string action, Skin skin, Color? trim = null)
    {
        float ui = skin.ui;
        float keyWidth = Mathf.Max(30 * ui, skin.keyCap.CalcSize(new GUIContent(key)).x + 16 * ui);
        float textWidth = skin.body.CalcSize(new GUIContent(action)).x;
        float width = keyWidth + textWidth + 40 * ui;
        var r = new Rect((screenWidth - width) * .5f, y, width, 40 * ui);
        Plaque(r, ui, .92f, trim);
        KeyCap(new Rect(r.x + 12 * ui, r.y + 9 * ui, keyWidth, 22 * ui), key, skin);
        Text(new Rect(r.x + 24 * ui + keyWidth, r.y + 10 * ui, textWidth + 4, 22 * ui), action, skin.body, Parchment);
    }
}
