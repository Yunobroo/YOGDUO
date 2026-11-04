using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopHub : MonoBehaviour
{
    public HubStation[] stations;
    HubStation nearby;
    bool shopOpen, trading;
    string dialogue = "Looking to buy something?";
    GUIStyle text, speaker, button, purse;
    GoblinDungeon game;

    private void Awake()
    {
       game = GoblinDungeon.Instance;
    }

    void Update()
    {
        if (game == null) game = GoblinDungeon.Instance;

        if (game == null || !game.isHub) return;

        nearby = null;

        foreach (var station in stations)
            if (station != null && station.InRange(game.player)) { nearby = station; break; }

        if (nearby == null || nearby.destination != 0) Close();

        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (interact && nearby != null) Interact();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (trading) trading = false;
            else Close();
        }

        game.SetShopping(shopOpen);
    }

    void Close()
    {
        shopOpen = false;
        trading = false;
    }

    void Interact()
    {
        Debug.Log($"Interacted to go to destination: {nearby.destination}");
        if (nearby.destination == 0)
        {
            if (shopOpen) Close();
            else
            {
                shopOpen = true;
                dialogue = "Looking to buy something?";
            }
            GoblinDungeon.Instance.SetShopping(shopOpen);
        }
        else DataManager.EnterLevel((int)nearby.destination);
    }

    void Styles()
    {
        if (text != null) return;
        text = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
        text.normal.textColor = new Color(.9f, .88f, .82f);
        speaker = new GUIStyle(text) { fontSize = 16, fontStyle = FontStyle.Bold };
        speaker.normal.textColor = new Color(.83f, .69f, .43f);
        button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        purse = new GUIStyle(text) { alignment = TextAnchor.MiddleRight, fontSize = 18 };
    }

    static void Panel(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(.07f, .065f, .055f, .94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (GoblinDungeon.Instance == null || !GoblinDungeon.Instance.isHub) return;
        Styles();
        Matrix4x4 oldMatrix = GUI.matrix;
        bool oldEnabled = GUI.enabled;
        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
        if (scale <= 0) return;
        GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x + (safe.width - 1280 * scale) / 2,
            Screen.height - safe.yMax + (safe.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        try
        {
            GUI.Label(new Rect(1020, 24, 230, 34), DataManager.Data.bank + " coins", purse);
            if (shopOpen) DrawConversation();
            else if (nearby != null)
            {
                bool locked = !DataManager.CompletedLevel(nearby.destination--);
                if(nearby.destination == 1) locked = false; //Don't lock level 1
                string action = nearby.destination == 0 ? "Talk"
                    : nearby.destination == 1 ? "Enter dungeon"
                    : locked ? "Vault locked" : "Enter royal vault";
                string key = Gamepad.current != null ? "A / Cross" : "E";
                GUI.enabled = !locked;
                if (GUI.Button(new Rect(470, 638, 340, 48), locked ? action : key + "  " + action, button)) Interact();
            }
        }
        finally { GUI.matrix = oldMatrix; GUI.enabled = oldEnabled; }
    }

    void DrawConversation()
    {
        if (trading)
        {
            Panel(new Rect(350, 275, 580, 246));
            Offer(297, "Boots", DataManager.Data.boots + "/2", 5, DataManager.Data.boots >= 2,
                "These will put a little speed in your step.", BuyBoots);
            Offer(369, "Soft soles", DataManager.Data.softShoes ? "Owned" : "", 4, DataManager.Data.softShoes,
                "Quieter steps. Don't go running in them, though.", BuySoftShoes);
            Offer(441, "Endurance", DataManager.Data.stamina + "/2", 6, DataManager.Data.stamina >= 2,
                "You should be able to run a little longer now.", BuyStamina);
        }
        Panel(new Rect(230, 544, 820, 152));
        GUI.Label(new Rect(254, 559, 740, 24), "Fence", speaker);
        GUI.Label(new Rect(254, 593, 740, 40), dialogue, text);
        if (GUI.Button(new Rect(254, 643, 180, 34), trading ? "Back" : "Show me.", button)) trading = !trading;
        if (GUI.Button(new Rect(454, 643, 180, 34), "Any work?", button))
            dialogue = DataManager.Data.levels.Any(level => level.completed == false) ? "Still plenty of gold in those vaults. Help yourself."
                : DataManager.CompletedLevel(1) ? "The royal vault's open. Gate on your right."
                : "Bring back the dungeon crown. Then I'll open the royal vault.";
        if (GUI.Button(new Rect(844, 643, 180, 34), "Goodbye.", button))
        {
            Close();
            GoblinDungeon.Instance.SetShopping(false);
        }
    }

    void Offer(float y, string name, string rank, int cost, bool owned, string response, System.Func<bool> buy)
    {
        GUI.Label(new Rect(374, y + 9, 285, 30), name + (rank.Length > 0 ? "  " + rank : ""), text);
        GUI.enabled = !owned && DataManager.Data.bank >= cost;
        if (GUI.Button(new Rect(714, y, 190, 48), owned ? "Owned" : cost + " coins", button))
            dialogue = buy() ? response : "Come back with a few more coins.";
        GUI.enabled = true;
    }

    public bool BuyBoots()
    {
        if (DataManager.Data.bank < 5 || DataManager.Data.boots >= 2) return false;
        DataManager.Data.bank -= 5;
        DataManager.Data.boots++;
        DataManager.Save();
        return true;
    }

    public bool BuySoftShoes()
    {
        if (DataManager.Data.bank < 4 || DataManager.Data.softShoes) return false;
        DataManager.Data.bank -= 4;
        DataManager.Data.softShoes = true;
        DataManager.Save();
        return true;
    }

    public bool BuyStamina()
    {
        if (DataManager.Data.bank < 6 || DataManager.Data.stamina >= 2) return false;
        DataManager.Data.bank -= 6;
        DataManager.Data.stamina++;
        DataManager.Save();
        return true;
    }
}
