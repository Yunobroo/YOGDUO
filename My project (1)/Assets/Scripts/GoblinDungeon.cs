using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Scene references and run state only. Layout, models and navigation are saved assets.
public class GoblinDungeon : MonoBehaviour
{
    public static GoblinDungeon Instance { get; private set; }
    [Header("Campaign")]
    public bool isHub;
    [Range(1, 2)] public int levelNumber = 1;
    public string levelTitle = "The Dungeon";
    public Player player;
    public SearchAndChase[] guards;
    public HideSpot[] hidingSpots;
    public Collectible[] collectibles;
    public bool Playing => !CaughtPlayer && !Won && !Shopping;
    public bool CaughtPlayer { get; private set; }
    public bool Won { get; private set; }
    public Collectible Crown { get; private set; }
    public bool CarryingCrown => Crown != null && Crown.PlayerCarrying;
    public Collectible Key { get; private set; }
    public bool CarryingKey => Key != null && Key.PlayerCarrying;
    public bool HitProtected => Time.time < protectedUntil;
    public string Objective => Won ? "Escaped with the crown!"
        : CarryingCrown ? "Carry the crown to the exit - sprint disabled"
        : Crown != null && Crown.GuardCarrier != null ? "A guard is returning the crown!"
        : Crown != null && Crown.Dropped ? "Crown dropped - recover it before the guards!"
        : "Steal the crown, then return to the green exit";
    public bool Shopping { get; private set; }
    public int Coins { get; private set; }
    public int Gems { get; private set; }
    public int Orbs { get; private set; }
    public int TotalCoins { get; private set; }
    public int TotalGems { get; private set; }
    public int TotalOrbs { get; private set; }
    public int Bank => HeistProgress.Bank;
    public int Boots => HeistProgress.Boots;
    public bool SoftShoes => HeistProgress.SoftShoes;
    public int EscapeReward => HeistProgress.EscapeReward(levelNumber, Coins, Gems);
    float protectedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() => Instance = null;

    void Awake()
    {
        Instance = this;
        // Resolve the actual scene objects; old Inspector arrays can contain duplicate entries.
        if (player == null) player = FindAnyObjectByType<Player>();
        guards = FindObjectsByType<SearchAndChase>();
        hidingSpots = FindObjectsByType<HideSpot>();
        collectibles = FindObjectsByType<Collectible>();
        var counted = new System.Collections.Generic.HashSet<Collectible>();
        foreach (var item in collectibles)
        {
            if (item == null || !counted.Add(item)) continue;

            switch (item.kind)
            {
                case Collectible.Kind.Coin:
                    TotalCoins++; break;
                case Collectible.Kind.Gem:
                    TotalGems++; break;
                case Collectible.Kind.Orb:
                    TotalOrbs++; break;
                case Collectible.Kind.Crown:
                    Crown = item; break;
                case Collectible.Kind.Key:
                    Key = item; break;
            }
        }
    }

    public bool InHidingSpot(Vector3 position)
    {
        foreach (var spot in hidingSpots) if (spot != null && spot.Contains(position)) return true;
        return false;
    }

    public void Collect(Collectible.Kind kind)
    {
        if (!Playing) return;
        switch (kind)
        {
            case Collectible.Kind.Coin:
                Coins++; break;
            case Collectible.Kind.Gem:
                Gems++; break;
            case Collectible.Kind.Orb:
                Orbs++; break;
        }
    }

    public void Escape()
    {
        if (!Playing || !CarryingCrown) return;
        Won = true;
        HeistProgress.CompleteHeist(levelNumber, Coins, Gems);
    }

    public void Caught()
    {
        if (!Playing || isHub || HitProtected) return;
        // Holding the crown always provides protection, including after recovering it.
        if (CarryingCrown || CarryingKey)
        {
            protectedUntil = Time.time + 2f;
            Crown.Drop(player.transform.position);
            Key.Drop(player.transform.position);
            return;
        }
        CaughtPlayer = true;
    }

    public void OpenShop()
    {
        HeistProgress.ReturnToHub();
    }

    public void SetShopping(bool shopping)
    {
        if (isHub) Shopping = shopping;
    }

    public void BuyBoots()
    {
        if (isHub) HeistProgress.BuyBoots();
    }

    public void BuySoftShoes()
    {
        if (isHub) HeistProgress.BuySoftShoes();
    }
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
