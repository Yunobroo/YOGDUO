using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run only in the isolated validation project, without -quit; exits on completion.
public static class CampaignPlayValidation
{
    const string StateKey = "Goblin.CampaignPlayValidation";
    static int stage;
    static double deadline;
    static SearchAndChase carrier;
    static bool sawGuardCarry;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        if (Application.isBatchMode && SessionState.GetBool(StateKey, false))
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        SessionState.SetBool(StateKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/GoblinDungeon2.unity");
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Timed out at Play mode stage " + stage);
            if (!EditorApplication.isPlaying || GoblinDungeon.Instance == null) return;
            var game = GoblinDungeon.Instance;
            if (stage == 0)
            {
                foreach (var guard in game.guards)
                {
                    if (!guard.agent.isOnNavMesh) throw new InvalidOperationException(guard.name + " did not start on the baked navigation mesh");
                    guard.enabled = false;
                    guard.StopAllCoroutines();
                    if (guard.agent.isOnNavMesh) guard.agent.isStopped = true;
                    if (carrier == null || Vector3.Distance(guard.transform.position, game.Crown.HomePosition) <
                        Vector3.Distance(carrier.transform.position, game.Crown.HomePosition)) carrier = guard;
                }
                Move(game.player, game.Crown.HomePosition);
                game.player.enabled = false;
                stage = 1;
            }
            else if (stage == 1 && game.CarryingCrown)
            {
                Move(game.player, new Vector3(14, .05f, 10));
                game.Caught();
                if (!game.Crown.Dropped || game.CaughtPlayer) throw new InvalidOperationException("First crown hit failed");
                Move(game.player, new Vector3(-18, .05f, -13));
                carrier.agent.Warp(new Vector3(14, .05f, 8));
                carrier.enabled = true;
                stage = 2;
            }
            else if (stage == 2)
            {
                if (game.Crown.GuardCarrier == carrier) sawGuardCarry = true;
                if (sawGuardCarry && game.Crown.GuardCarrier == null && !game.Crown.Dropped)
                {
                    if (Vector3.Distance(game.Crown.transform.position, game.Crown.HomePosition) > .01f)
                        throw new InvalidOperationException("Guard did not restore the original crown position");
                    stage = 3;
                    game.OpenShop();
                }
            }
            else if (stage == 3 && game.isHub)
            {
                if (UnityEngine.Object.FindAnyObjectByType<ShopHub>() == null) throw new InvalidOperationException("Hub failed to load");
                var shop = UnityEngine.Object.FindAnyObjectByType<ShopHub>();
                Move(game.player, shop.stations[0].transform.position);
                if (!shop.stations[0].InRange(game.player)) throw new InvalidOperationException("Merchant cannot be approached");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/CampaignPlayValidationReport.txt",
                    "PASS: Level 2 runs in Play mode; crown pickup, hit/drop, guard navigation, visible guard carry, physical return to pedestal, hub scene transition, and merchant access succeeded.\n");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/CampaignPlayValidationReport.txt", "FAILED: " + error);
            Debug.LogException(error);
            Finish(1);
        }
    }

    static void Move(Player player, Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = position;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    static void Finish(int code)
    {
        SessionState.SetBool(StateKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
