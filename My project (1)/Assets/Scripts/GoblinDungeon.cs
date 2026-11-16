using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Collectible;

// Scene references and run state only. Layout, models and navigation are saved assets.
public class GoblinDungeon : MonoBehaviour
{
    public static GoblinDungeon Instance { get; private set; }
    [Header("Campaign")]
    public bool isHub;
    public int levelNumber = 1;
    public string levelTitle = "The Dungeon";
    public Player player;
    public SearchAndChase[] guards;
    public HideSpot[] hidingSpots;
    public Collectible[] collectibles;
    public bool Playing => !CaughtPlayer && !Won;
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
    public int Coins { get; private set; }
    public int Gems { get; private set; }
    public int Orbs { get; private set; }

    public int Bars { get; private set; }
    public int TotalCoins { get; private set; }

    public List<Collectible> collectedCollectables = new List<Collectible>();

    float protectedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() => Instance = null;

    void Awake()
    {
        Instance = this;
        if (player == null) player = FindAnyObjectByType<Player>();
        guards = FindObjectsByType<SearchAndChase>();
        hidingSpots = FindObjectsByType<HideSpot>();
        collectibles = FindObjectsByType<Collectible>();
        InitializeCollectables();
        // Only the torches nearest the goblin cast shadows; see LightShadowBudget.
        if (GetComponent<LightShadowBudget>() == null) gameObject.AddComponent<LightShadowBudget>();
    }

    private void InitializeCollectables()
    {
        var counted = new System.Collections.Generic.HashSet<Collectible>();

        var data = DataManager.GetLevelData(levelNumber);

        bool populateData = data.totalCollectables.Count == 0;                          //If the save data for this level hasn't got the collectables yet populate the data

        foreach (var item in collectibles)
        {
            if (item == null || !counted.Add(item)) continue;                           //Make sure there is an item and it's not a duplicate

            Debug.Log($"Level {levelNumber} item check: {item.kind} {item.Id} " +       //Log the item we're working with  
                      $"| Collected: {DataManager.IsCollected(levelNumber, item)}");

            if (item.kind == Kind.Coin) TotalCoins++;                                   //If it's a coin track it for gui, because save data does not care about coins this does it

            if (populateData) DataManager.AddCollectable(levelNumber, item);            //If we're populating the save data add the item to the total collectables for this level

            if (item.kind == Kind.Crown)
            {
                Crown = item;
                continue;
            }

            if (item.kind == Kind.Key)
            {
                Key = item;
                continue;
            }

            if (DataManager.IsCollected(levelNumber, item))                             //Disable if it has been collected before
            {
                Debug.Log($"Deactivating {item.kind} {item.Id}");
                item.gameObject.SetActive(false);
            }
        }
    }

    public bool InHidingSpot(Vector3 position)
    {
        foreach (var spot in hidingSpots) if (spot != null && spot.Contains(position)) return true;
        return false;
    }

    public void Collect(Collectible collectable)
    {
        collectedCollectables.Add(collectable);
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && collectable.pickupSound != null)
            audioSource.PlayOneShot(collectable.pickupSound);

    }

    public void Escape()
    {
        if (!Playing || !CarryingCrown) return;
        DataManager.CompleteLevel(levelNumber, collectedCollectables);
        Won = true;
    }

    public void Caught()
    {
        if (!Playing || isHub || HitProtected) return;
        // Holding the crown always provides protection, including after recovering it.
        if (CarryingCrown || CarryingKey)
        {
            protectedUntil = Time.time + 2f;
            if (CarryingCrown) Crown.Drop(player.transform.position);
            if (CarryingKey) Key.Drop(player.transform.position);
            return;
        }
        CaughtPlayer = true;
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
