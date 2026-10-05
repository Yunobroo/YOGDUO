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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { Instance = null; bank = boots = 0; softShoes = false; }

    void Awake()
    {
        Instance = this;
        foreach (var item in collectibles)
        {
            if (item == null) continue;
            if (item.kind == Collectible.Kind.Coin) TotalCoins++;
            if (item.kind == Collectible.Kind.Gem) TotalGems++;
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
        else { Won = true; bank += Coins + Gems * 3; }
    }

    public void Caught() { if (Playing) CaughtPlayer = true; }
    public void OpenShop() { if (Won) Shopping = true; }
    public void BuyBoots() { if (Shopping && bank >= 5 && boots < 2) { bank -= 5; boots++; } }
    public void BuySoftShoes() { if (Shopping && bank >= 4 && !softShoes) { bank -= 4; softShoes = true; } }
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
