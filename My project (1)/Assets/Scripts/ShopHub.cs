using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static DungeonUI;

public class ShopHub : MonoBehaviour
{
    public string stationName;
    public float interactionRadius = 2.4f;
    public string dialogue = "Looking to buy something?";
    bool shopOpen, trading;
    DungeonUI.Skin skin;
    GoblinDungeon game;
    public UpgradeItem[] items = new UpgradeItem[5];
    [Tooltip("Height of each row in the wares panel (1280x720 canvas units).")]
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
    Player player;

    private bool InRange()
    {
        // Cached: searching the scene every call ran several times per frame.
        if (player == null) player = FindAnyObjectByType<Player>();
        if (player == null) return false;
        var playerPos = player.transform.position;
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

    // Drawn on a fixed 1280x720 canvas that GUI.matrix scales to the screen.
    void OnGUI()
    {
        if (GoblinDungeon.Instance == null || !GoblinDungeon.Instance.isHub) return;
        skin = DungeonUI.Fixed();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        bool oldEnabled = GUI.enabled;
        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
        if (scale <= 0) return;
        GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x + (safe.width - 1280 * scale) / 2,
            Screen.height - safe.yMax + (safe.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        try
        {
            DrawPurse();
            if (shopOpen) DrawConversation();
            else if (InRange())
                Prompt(1280, 640, Gamepad.current != null ? "A" : "E", "Talk to the Fence", skin, Torch);
        }
        finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; }
    }

    void DrawPurse()
    {
        var r = new Rect(1080, 20, 180, 44);
        Plaque(r, 1, .92f);
        Fill(new Rect(r.x + 16, r.y + 18, 9, 9), CoinColor);
        Text(new Rect(r.x + 34, r.y + 11, 130, 22), DataManager.Data.bank + " coins", skin.heading, CoinColor);
    }

    void DrawConversation()
    {
        if (trading) DrawUpgradeOffers();

        var box = new Rect(230, 536, 820, 164);
        Plaque(box, 1, .95f, Torch);
        Text(new Rect(256, 550, 400, 24), "THE FENCE", skin.heading, Torch);
        Fill(new Rect(256, 576, 120, 1), A(Bronze, .8f));
        Text(new Rect(256, 586, 770, 48), dialogue, skin.body, Parchment);

        GUI.color = Color.white;
        float y = 646, bw = 180, bh = 38;
        if (GUI.Button(new Rect(256, y, bw, bh), trading ? "Back" : "Show me your wares", skin.button)) trading = !trading;
        if (GUI.Button(new Rect(452, y, bw, bh), "Any work?", skin.button))
            dialogue = DataManager.Data.levels.Any(level => level.completed == false) ? "Still plenty of gold in those vaults. Help yourself."
                : DataManager.CompletedLevel(1) ? "The royal vault's open. Gate on your right."
                : "Bring back the dungeon crown. Then I'll open the royal vault.";
        if (GUI.Button(new Rect(648, y, bw, bh), "I need another chance! (Wipe save)", skin.dangerButton)) DataManager.ResetAllProgress();
        if (GUI.Button(new Rect(844, y, bw, bh), "Goodbye.", skin.button)) Close();
        Text(new Rect(256, 690, 770, 14), "Esc to step away", skin.tiny, Faded, .8f);
    }

    void DrawUpgradeOffers()
    {
        float rowHeight = offerSpacing;
        float height = 58 + items.Length * rowHeight;
        var panel = new Rect(350, 524 - height, 580, height);
        Plaque(panel, 1, .95f);
        Text(new Rect(panel.x, panel.y + 12, panel.width, 26), "~ WARES ~", skin.bodyCenter, Torch);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            float y = panel.y + 48 + i * rowHeight;
            if (i > 0) Fill(new Rect(panel.x + 20, y - 4, panel.width - 40, 1), A(Bronze, .45f));
            Offer(new Rect(panel.x + 24, y, panel.width - 48, rowHeight - 8), items[i]);
        }
    }

    void Offer(Rect row, UpgradeItem item)
    {
        int availability = ItemAvailable(item);
        int owned = item.Owned();

        Text(new Rect(row.x, row.y + 4, 300, 22), item.name, skin.heading, Parchment);
        // One pip per upgrade level; lit pips are already owned.
        for (int level = 0; level < item.maxLevel; level++)
        {
            var pip = new Rect(row.x + level * 14, row.y + 32, 9, 9);
            Fill(pip, level < owned ? Torch : new Color(.22f, .17f, .12f));
            Outline(pip, A(Bronze, .8f));
        }
        string levelText = availability == 2 ? "Mastered" : $"Lvl {owned}  >  {owned + 1}";
        Text(new Rect(row.x + item.maxLevel * 14 + 8, row.y + 28, 200, 18), levelText, skin.small, Faded);

        string buttonText = availability switch
        {
            2 => "Max level",
            3 => $"Needs the orb from level {item.orbNeeded}",
            _ => $"{item.CurrentCost()} coins"
        };
        var buttonRect = new Rect(row.xMax - 200, row.y + 6, 200, Mathf.Min(44, row.height - 6));
        bool previousEnabled = GUI.enabled;
        GUI.enabled = availability == 0;
        GUI.color = Color.white;
        if (GUI.Button(buttonRect, buttonText, skin.button))
        {
            dialogue = item.buyText;
            BuyItem(item);
        }
        GUI.enabled = previousEnabled;
        if (availability == 1)
            Text(new Rect(buttonRect.x, buttonRect.yMax + 1, buttonRect.width, 14), "not enough coin", skin.tiny, Blood);
    }

    public void BuyItem(UpgradeItem item)
    {
        DataManager.Data.bank -= item.CurrentCost();
        item.OnBuy();
        DataManager.Save();
    }

    private int ItemAvailable(UpgradeItem item)
    {

        if (item.Owned() >= item.maxLevel) return 2;                                                // "Already reached max level!" --- Put this first in the code

        if (item.orbNeeded != 0                                                                     //If an orb is required
            && !DataManager.GetLevelData(item.orbNeeded)                                            //Check the level of which the orb is required
            .collectedCollectables.Any(collectable => collectable.kind == Collectible.Kind.Orb))    //Has the orb been collected
            return 3;                                                                               //Locked behing orb


        if (DataManager.Data.bank < item.CurrentCost()) return 1;                                            // "Come back with a few more gold"
        return 0;                                                                                   //Success condition
    }
}
