using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MovementValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [InitializeOnLoadMethod]
    static void Initialize() => EditorApplication.update += CheckRequest;

    static void CheckRequest()
    {
        if (!File.Exists("Temp/ValidateMovement.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/ValidateMovement.request");
        Run();
    }

    [MenuItem("Goblin/Validate Player Movement")]
    public static void Run()
    {
        var report = new StringBuilder();
        Scene preview = EditorSceneManager.NewPreviewScene();
        var cache = typeof(DataManager).GetField("data", BindingFlags.Static | BindingFlags.NonPublic);
        object previousData = cache.GetValue(null);
        try
        {
            // Use in-memory upgrades; do not read or write the player's save file.
            cache.SetValue(null, new SaveData());
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Actors/Player.prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            root.transform.position = new Vector3(1000, .05f, 1000);
            foreach (var animator in root.GetComponentsInChildren<Animator>()) animator.enabled = false;
            var player = root.GetComponentInChildren<Player>();
            Invoke(player, "Start");
            typeof(Player).GetField("movementCamera", Hidden).SetValue(player, null);
            var camera = root.GetComponentInChildren<PlayerCameraFollow>();
            if (camera == null) throw new Exception("Player camera follow component is missing");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, preview);
            floor.transform.position = new Vector3(1000, -.1f, 1000);
            floor.transform.localScale = new Vector3(100, .2f, 100);
            var controller = player.GetComponent<CharacterController>();
            var move = typeof(Player).GetMethod("Move", Hidden);
            Vector3 parentPosition = root.transform.position;
            foreach (int fps in new[] { 30, 60, 144 })
            {
                controller.enabled = false;
                player.transform.position = parentPosition;
                controller.enabled = true;
                Physics.SyncTransforms();
                for (int i = 0; i < 30; i++) move.Invoke(player, new object[] { Vector2.zero, false, 1f / 60 });
                Invoke(camera, "Start");
                float cameraHeight = camera.transform.position.y;
                Vector3 start = player.transform.position;
                float minY = start.y, maxY = start.y;
                for (int i = 0; i < fps; i++)
                {
                    move.Invoke(player, new object[] { Vector2.up, false, 1f / fps });
                    Invoke(camera, "LateUpdate");
                    minY = Mathf.Min(minY, player.transform.position.y);
                    maxY = Mathf.Max(maxY, player.transform.position.y);
                    if (Mathf.Abs(camera.transform.position.y - cameraHeight) > .001f) throw new Exception("Camera height changed");
                }
                Vector3 travel = player.transform.position - start;
                travel.y = 0;
                if (Mathf.Abs(travel.magnitude - player.walkSpeed) > .025f) throw new Exception("Incorrect walking distance at " + fps + " FPS: " + travel.magnitude);
                if (maxY - minY > .025f) throw new Exception("Player bounces on a flat floor at " + fps + " FPS");
                if (Vector3.Distance(root.transform.position, parentPosition) > .001f) throw new Exception("Player moved its own parent");
                report.AppendLine("PASS: " + fps + " FPS / " + travel.magnitude.ToString("F3") + "m per second / stable player and camera height / parent unchanged");
            }

            foreach (string scene in new[] { "Level_1", "Level_2" })
            {
                string source = File.ReadAllText("Assets/Scenes/" + scene + ".unity");
                string guid = AssetDatabase.AssetPathToGUID("Assets/Prefabs/Actors/Player.prefab");
                if (!source.Contains(guid)) throw new Exception(scene + " does not use the corrected player prefab");
                report.AppendLine("PASS: " + scene + " uses the corrected shared player prefab");
            }
            report.AppendLine("PASS: Movement regression checks complete; player save data unchanged.");
        }
        catch (Exception error)
        {
            report.AppendLine("FAILED: " + error);
            Debug.LogException(error);
        }
        finally
        {
            cache.SetValue(null, previousData);
            EditorSceneManager.ClosePreviewScene(preview);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MovementValidationReport.txt", report.ToString());
        }
    }

    static void Invoke(object target, string method) => target.GetType().GetMethod(method, Hidden).Invoke(target, null);
}

