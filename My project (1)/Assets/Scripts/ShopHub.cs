using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopHub : MonoBehaviour
{
    public string stationName;
    public float interactionRadius = 2.4f;
    public string dialogue = "Looking to buy something?";
    bool shopOpen, trading;
    GUIStyle text, speaker, button, purse;
    GoblinDungeon game;
    public UpgradeItem[] items = new UpgradeItem[5];
    [SerializeField] private float offerStartY = 297f;
    [SerializeField] private float offerSpacing = 72f;

    private void Awake()
    {
       game = GoblinDungeon.Instance;
    }

    void Update()
    {
        if (game == null) game = GoblinDungeon.Instance;

        if (game == null || !game.isHub) return;

        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (interact && InRange()) Interact();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (trading) trading = false;
            else Close();
        }
    }

    #region Interaction Range
    private bool InRange()
    {
        var playerPos = FindAnyObjectByType<Player>().transform.position;
        Vector3 offset = playerPos - transform.position;
        offset.y = 0;
        return isActiveAndEnabled && offset.sqrMagnitude <= interactionRadius * interactionRadius;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    #endregion

    void Close()
    {
        shopOpen = false;
        trading = false;
    }

    void Interact()
    {
        Debug.Log($"Interacted with shop");
        if (shopOpen) Close();
        else
        {
            shopOpen = true;
            dialogue = "Looking to buy something?";
        }
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
        }
        finally { GUI.matrix = oldMatrix; GUI.enabled = oldEnabled; }
    }

    void DrawConversation()
    {
        if (trading)
        {
            Panel(new Rect(350, 275, 580, 246));

            DrawUpgradeOffers();
        }
        Panel(new Rect(230, 544, 820, 152));
        GUI.Label(new Rect(254, 559, 740, 24), "Fence", speaker);
        GUI.Label(new Rect(254, 593, 740, 40), dialogue, text);
        if (GUI.Button(new Rect(254, 643, 180, 34), trading ? "Back" : "Show me.", button)) trading = !trading;
        if (GUI.Button(new Rect(454, 643, 180, 34), "Any work?", button))
            dialogue = DataManager.Data.levels.Any(level => level.completed == false) ? "Still plenty of gold in those vaults. Help yourself."
                : DataManager.CompletedLevel(1) ? "The royal vault's open. Gate on your right."
                : "Bring back the dungeon crown. Then I'll open the royal vault.";
        if (GUI.Button(new Rect(654, 643, 180, 34), "I need another chance! (Wipe save)", button)) DataManager.ResetAllProgress();
        if (GUI.Button(new Rect(844, 643, 180, 34), "Goodbye.", button))
        {
            Close();
            shopOpen = false;
        }
    }

    void DrawUpgradeOffers()
    {
        for (int i = 0; i < items.Length; i++)
        {
            float y = offerStartY + i * offerSpacing;

            UpgradeItem item = items[i];

            Offer(y, item, false);
        }
    }

    void Offer(float y, UpgradeItem item, bool owned)
    {

        var itemAvailability = ItemAvailable(item);
        var buttonText = $"{item.cost} coins";
        var descText = new StringBuilder($"{item.name} | Lvl. {item.Owned()} -> ");
        if (itemAvailability == 2) 
        {
            descText.Append("Max level");
        }
        else
        {
            descText.Append($"Lvl. {item.Owned() + 1}");
        }


            GUI.Label(
            new Rect(374, y + 9, 285, 30),
            descText.ToString(),
            text);

        bool previousEnabled = GUI.enabled;


        switch (itemAvailability){
            case 0:
                GUI.enabled = true;
                break;
            case 1:
                GUI.enabled = false;;
                break;
            case 2:
                GUI.enabled = false;
                buttonText = "Max Level";
                break ;
        }

        if (GUI.Button(
            new Rect(714, y, 190, 48),
            buttonText,
            button))
        {
            dialogue = "Thank you for your purchase!";
            BuyItem(item);
        }

        GUI.enabled = previousEnabled;
    }

    public void BuyItem(UpgradeItem item)
    {
        DataManager.Data.bank -= item.cost;
        item.OnBuy();
        DataManager.Save();
    }

    private int ItemAvailable(UpgradeItem item)
    {

        if (item.Owned() >= item.maxLevel) return 2;// "Already reached max level!" --- Put this first in the code
        if (DataManager.Data.bank < item.cost) return 1;// "Come back with a few more gold"

        return 0; //Success condition
    }
}
