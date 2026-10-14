using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Scene references and run state only. Layout, models and navigation are saved assets.
public class GoblinDungeon : MonoBehaviour
{
    public static GoblinDungeon Instance { get; private set; }
    public Player player;
    public SearchAndChase[] guards;
    public HideSpot[] hidingSpots;
    public Collectible[] collectibles;
    public bool Playing => !CaughtPlayer && !Won && !Shopping;
    public bool CaughtPlayer { get; private set; }
    public bool Won { get; private set; }
    public Collectible Crown { get; private set; }
    public bool CarryingCrown => Crown != null && Crown.PlayerCarrying;
    public bool CrownProtectionUsed { get; private set; }
    public bool HitProtected => Time.time < protectedUntil;
    public string Objective => Won ? "Escaped with the crown!"
        : CarryingCrown ? "Carry the crown to the exit - sprint disabled"
        : Crown != null && Crown.GuardCarrier != null ? "A guard is returning the crown!"
        : Crown != null && Crown.Dropped ? "Crown dropped - recover it before the guards!"
        : "Steal the crown, then return to the green exit";
    public bool Shopping { get; private set; }
    public int Coins { get; private set; }
    public int Gems { get; private set; }
    public int TotalCoins { get; private set; }
    public int TotalGems { get; private set; }
    public int Bank => bank;
    public int Boots => boots;
    public bool SoftShoes => softShoes;
    static int bank, boots;
    static bool softShoes;
    float protectedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { Instance = null; bank = boots = 0; softShoes = false; }

    void Awake()
    {
        Instance = this;
        var counted = new System.Collections.Generic.HashSet<Collectible>();
        foreach (var item in collectibles)
        {
            if (item == null || !counted.Add(item)) continue;
            if (item.kind == Collectible.Kind.Coin) TotalCoins++;
            if (item.kind == Collectible.Kind.Gem) TotalGems++;
            if (item.kind == Collectible.Kind.Crown) Crown = item;
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
        if (kind == Collectible.Kind.Coin) Coins++;
        else if (kind == Collectible.Kind.Gem) Gems++;
    }

    public void Escape()
    {
        if (!Playing || !CarryingCrown) return;
        Won = true;
        bank += Coins + Gems * 3;
    }

    public void Caught()
    {
        if (!Playing || HitProtected) return;
        // The crown absorbs one hit per run; picking it up again does not refill it.
        if (CarryingCrown && !CrownProtectionUsed)
        {
            CrownProtectionUsed = true;
            protectedUntil = Time.time + 2f;
            Crown.Drop(player.transform.position);
            return;
        }
        CaughtPlayer = true;
    }

    public void OpenShop()
    {
        if (Won) Shopping = true;
    }

    public void BuyBoots()
    {
        if (!Shopping || bank < 5 || boots >= 2) return;
        bank -= 5;
        boots++;
    }

    public void BuySoftShoes()
    {
        if (!Shopping || bank < 4 || softShoes) return;
        bank -= 4;
        softShoes = true;
    }
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
