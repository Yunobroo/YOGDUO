using UnityEngine;
using UnityEngine.InputSystem;

public class ShopHub : MonoBehaviour
{
    public HubStation[] stations;
    HubStation nearby;
    bool shopOpen, trading;
    string dialogue = "Looking to buy something?";
    GUIStyle text, speaker, button, purse;

    void Update()
    {
        var game = GoblinDungeon.Instance;
        if (game == null || !game.isHub) return;
        nearby = null;
        foreach (var station in stations)
            if (station != null && station.InRange(game.player)) { nearby = station; break; }
        if (nearby == null || nearby.destination != HubStation.Destination.Shop) Close();
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
        if (nearby.destination == HubStation.Destination.Shop)
        {
            if (shopOpen) Close();
            else
            {
                shopOpen = true;
                dialogue = "Looking to buy something?";
            }
            GoblinDungeon.Instance.SetShopping(shopOpen);
        }
        else HeistProgress.EnterLevel(nearby.destination == HubStation.Destination.SecondHeist ? 2 : 1);
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
            GUI.Label(new Rect(1020, 24, 230, 34), HeistProgress.Bank + " coins", purse);
            if (shopOpen) DrawConversation();
            else if (nearby != null)
            {
                bool locked = nearby.destination == HubStation.Destination.SecondHeist && !HeistProgress.LevelTwoUnlocked;
                string action = nearby.destination == HubStation.Destination.Shop ? "Talk"
                    : nearby.destination == HubStation.Destination.FirstHeist ? "Enter dungeon"
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
            Offer(297, "Boots", HeistProgress.Boots + "/2", 5, HeistProgress.Boots >= 2,
                "These will put a little speed in your step.", HeistProgress.BuyBoots);
            Offer(369, "Soft soles", HeistProgress.SoftShoes ? "Owned" : "", 4, HeistProgress.SoftShoes,
                "Quieter steps. Don't go running in them, though.", HeistProgress.BuySoftShoes);
            Offer(441, "Endurance", HeistProgress.StaminaUpgrades + "/2", 6, HeistProgress.StaminaUpgrades >= 2,
                "You should be able to run a little longer now.", HeistProgress.BuyStamina);
        }
        Panel(new Rect(230, 544, 820, 152));
        GUI.Label(new Rect(254, 559, 740, 24), "Fence", speaker);
        GUI.Label(new Rect(254, 593, 740, 40), dialogue, text);
        if (GUI.Button(new Rect(254, 643, 180, 34), trading ? "Back" : "Show me.", button)) trading = !trading;
        if (GUI.Button(new Rect(454, 643, 180, 34), "Any work?", button))
            dialogue = HeistProgress.CampaignComplete ? "Still plenty of gold in those vaults. Help yourself."
                : HeistProgress.LevelTwoUnlocked ? "The royal vault's open. Gate on your right."
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
        GUI.enabled = !owned && HeistProgress.Bank >= cost;
        if (GUI.Button(new Rect(714, y, 190, 48), owned ? "Owned" : cost + " coins", button))
            dialogue = buy() ? response : "Come back with a few more coins.";
        GUI.enabled = true;
    }
}
