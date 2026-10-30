using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Run in an isolated batch editor: -executeMethod CampaignValidation.RunAll.
// Saves and restores campaign preferences; never spends the player's real purse.
public static class CampaignValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string SaveKey = "GoblinHeist.Progress.v1";
    static StringBuilder report;
    static int assertions;

    public static void RunAll()
    {
        report = new StringBuilder();
        bool hadSave = PlayerPrefs.HasKey(SaveKey);
        string save = PlayerPrefs.GetString(SaveKey);
        var cache = typeof(HeistProgress).GetField("data", BindingFlags.Static | BindingFlags.NonPublic);
        Keyboard keyboard = null;
        try
        {
            cache.SetValue(null, Activator.CreateInstance(cache.FieldType, true));
            Check(HeistProgress.Bank == 0 && !HeistProgress.LevelTwoUnlocked, "New campaign starts with Level 2 locked");
            Check(!HeistProgress.BuyBoots(), "Insufficient funds cannot purchase upgrades");
            HeistProgress.CompleteHeist(1, 8, 2, 3);
            Check(HeistProgress.Bank == 19 && HeistProgress.LevelTwoUnlocked, "Level 1 banks loot plus bounty and unlocks Level 2");
            Check(HeistProgress.BuyBoots() && HeistProgress.BuySoftShoes() && HeistProgress.BuyStamina(), "All three shop upgrades purchase successfully");
            Check(HeistProgress.Bank == 4, "Shop deducts the correct prices");
            cache.SetValue(null, null);
            Check(HeistProgress.Bank == 4 && HeistProgress.Boots == 1 && HeistProgress.SoftShoes && HeistProgress.StaminaUpgrades == 1,
                "Purse, unlocks, and upgrades survive a save reload");

            EditorSceneManager.OpenScene("Assets/Scenes/ShopHub.unity");
            var hub = UnityEngine.Object.FindAnyObjectByType<ShopHub>();
            Check(hub != null && hub.stations.Length == 3, "Hub contains the merchant and both heist entrances");
            Check(UnityEngine.Object.FindObjectsByType<SearchAndChase>().Length == 0, "Hub has no hostile guards");
            var hubGame = UnityEngine.Object.FindAnyObjectByType<GoblinDungeon>();
            Invoke(hubGame, "Awake");
            hubGame.Caught();
            Check(!hubGame.CaughtPlayer, "The hub is safe");

            EditorSceneManager.OpenScene("Assets/Scenes/GoblinDungeon.unity");
            Bounds firstBounds = FloorBounds();
            EditorSceneManager.OpenScene("Assets/Scenes/GoblinDungeon2.unity");
            var game = UnityEngine.Object.FindAnyObjectByType<GoblinDungeon>();
            foreach (var item in UnityEngine.Object.FindObjectsByType<Collectible>()) Invoke(item, "Awake");
            Invoke(game, "Awake");
            Invoke(game.player, "Start");
            Bounds secondBounds = FloorBounds();
            Check(secondBounds.size.x * secondBounds.size.z > firstBounds.size.x * firstBounds.size.z * 2,
                "Level 2 has more than twice Level 1's floor area");
            Check(game.guards.Length == 5 && game.hidingSpots.Length == 6 && game.TotalCoins == 24 && game.TotalGems == 6,
                "Royal Vault has five guards, six hiding spots, 24 coins, and six gems");
            Check(game.Crown != null && game.levelNumber == 2, "Level 2 has its own crown objective");
            CheckPath(game.player.transform.position, game.Crown.transform.position, "Player can reach the crown from the exit");
            foreach (var guard in game.guards)
            {
                for (int i = 0; i < guard.patrolPoints.Length; i++)
                    CheckPath(guard.patrolPoints[i].position, guard.patrolPoints[(i + 1) % guard.patrolPoints.Length].position, guard.name + " patrol leg " + i);
                CheckPath(guard.transform.position, game.Crown.transform.position, guard.name + " can return the crown");
            }
            foreach (var item in game.collectibles)
                CheckPath(game.player.transform.position, item.transform.position, item.name + " is accessible");

            Vector3 home = game.Crown.HomePosition;
            MovePlayer(game.player, home);
            Invoke(game.Crown, "Update");
            Check(game.CarryingCrown && !game.Won, "Picking up the crown carries it without winning");
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            bool running = (bool)typeof(Player).GetMethod("UpdateMovementMode", Private).Invoke(game.player, new object[] { Vector2.up, true });
            Check(!running, "Holding sprint cannot sprint while carrying the crown");

            game.Caught();
            Check(!game.CaughtPlayer && game.Crown.Dropped && game.HitProtected, "Crown absorbs a hit, drops, and grants a grace period");
            game.Caught();
            Check(!game.CaughtPlayer, "Immediate follow-up hits during protection are ignored");
            typeof(Collectible).GetField("playerPickupAllowedAt", Private).SetValue(game.Crown, -1f);
            MovePlayer(game.player, game.Crown.transform.position);
            Invoke(game.Crown, "Update");
            typeof(GoblinDungeon).GetField("protectedUntil", Private).SetValue(game, -1f);
            game.Caught();
            Check(!game.CaughtPlayer && game.Crown.Dropped, "Recovering the crown refills protection for the next hit");

            var carrier = game.guards[0];
            carrier.agent.Warp(game.Crown.transform.position);
            Physics.SyncTransforms();
            Check(game.Crown.TryGuardPickup(carrier), "Guard can pick up a dropped crown");
            Check(!game.CarryingCrown && game.Crown.GuardCarrier == carrier && game.Crown.transform.parent == carrier.transform,
                "Guard visibly carries the crown and the player cannot extract it");
            int bank = HeistProgress.Bank;
            game.Escape();
            Check(!game.Won && HeistProgress.Bank == bank, "Cannot escape while a guard carries the crown");
            game.Crown.ReturnHome(carrier);
            Check(game.Crown.GuardCarrier == null && Vector3.Distance(game.Crown.transform.position, home) < .01f,
                "Guard returns the crown to the exact original pedestal");

            typeof(Collectible).GetField("playerPickupAllowedAt", Private).SetValue(game.Crown, -1f);
            MovePlayer(game.player, home);
            Invoke(game.Crown, "Update");
            game.Escape();
            Check(game.Won && HeistProgress.Bank == bank + 10 && HeistProgress.CampaignComplete,
                "Level 2 extraction banks its crown bounty and completes the campaign");
            game.Escape();
            Check(HeistProgress.Bank == bank + 10, "Extraction pays only once");

            EditorSceneManager.OpenScene("Assets/Scenes/GoblinDungeon2.unity");
            game = UnityEngine.Object.FindAnyObjectByType<GoblinDungeon>();
            Invoke(game, "Awake");
            game.Caught();
            Check(game.CaughtPlayer, "A hit without the crown ends the run");
            report.AppendLine("PASS: " + assertions + " campaign, navigation, crown, and shop checks.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/CampaignValidationReport.txt", report.ToString());
        }
        catch (Exception error)
        {
            report.AppendLine("FAILED: " + error);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/CampaignValidationReport.txt", report.ToString());
            Debug.LogException(error);
            throw;
        }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (hadSave) PlayerPrefs.SetString(SaveKey, save);
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            cache.SetValue(null, null);
        }
    }

    static void CheckPath(Vector3 from, Vector3 to, string message)
    {
        bool start = NavMesh.SamplePosition(from, out var a, 2, NavMesh.AllAreas);
        bool end = NavMesh.SamplePosition(to, out var b, 2, NavMesh.AllAreas);
        var path = new NavMeshPath();
        Check(start && end && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, message);
    }

    static Bounds FloorBounds()
    {
        Bounds largest = default;
        foreach (var collider in UnityEngine.Object.FindObjectsByType<BoxCollider>())
            if (collider.gameObject.layer == 0 && collider.bounds.size.y < 1 && collider.bounds.size.x * collider.bounds.size.z > largest.size.x * largest.size.z) largest = collider.bounds;
        return largest;
    }

    static void MovePlayer(Player player, Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = position;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
        report.AppendLine("PASS: " + message);
    }

    static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
}
