using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VisibilityValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [InitializeOnLoadMethod]
    static void Initialize() => EditorApplication.update += Pending;

    static void Pending()
    {
        if (!File.Exists("Temp/ValidateVisibility.request") || EditorApplication.isUpdating || EditorApplication.isCompiling) return;
        File.Delete("Temp/ValidateVisibility.request");
        Run();
    }

    [MenuItem("Goblin/Validate Highlights and Animation")]
    public static void Run()
    {
        var report = new StringBuilder();
        var previous = SceneManager.GetActiveScene();
        // Use a temporary additive scene so real physics queries are available.
        // The user's open scene is neither changed nor saved.
        bool playing = Application.isPlaying;
        var scene = playing
            ? SceneManager.CreateScene("Visibility validation", new CreateSceneParameters(LocalPhysicsMode.Physics3D))
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Material original = null;
        try
        {
            foreach (string path in new[] { "Visibility/Outline", "Visibility/OccluderFade", "Visibility/PeepholeLit", "Visibility/PeepholeSimple", "Visibility/XRay" })
            {
                var shader = Resources.Load<Shader>(path);
                Check(shader != null && !ShaderUtil.ShaderHasError(shader), path + " shader imported without errors", report);
            }
            var enemy = Cube(scene, new Vector3(1010, 0, 1000));
            WorldHighlight.Add(enemy, Color.red);
            var outline = enemy.transform.Find("Highlight");
            Check(outline != null && outline.GetComponent<Renderer>().sharedMaterial.GetColor("_Color") == Color.red,
                "Enemy receives a red mesh outline", report);
            var cover = Cube(scene, new Vector3(1020, 0, 1000));
            WorldHighlight.Add(cover, Color.green);
            Check(cover.transform.Find("Highlight") != null, "Hiding spot receives a green outline", report);

            var target = new GameObject("Target");
            SceneManager.MoveGameObjectToScene(target, scene);
            target.transform.position = new Vector3(1000, 0, 1000);
            var camera = new GameObject("Camera", typeof(Camera), typeof(CameraOcclusion));
            SceneManager.MoveGameObjectToScene(camera, scene);
            camera.transform.position = new Vector3(1000, 5, 990);
            var fade = camera.GetComponent<CameraOcclusion>();
            camera.GetComponent<Camera>().enabled = false;
            fade.target = target.transform;
            fade.occlusionDelay = 0;
            fade.releaseDelay = 0;
            Invoke(fade, "Awake");
            var wall = Cube(scene, new Vector3(1000, 1.5f, 998));
            wall.transform.localScale = new Vector3(6, 3, .5f);
            var renderer = wall.GetComponent<Renderer>();
            original = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            original.color = Color.gray;
            renderer.sharedMaterial = original;
            Physics.SyncTransforms();
            if (playing) scene.GetPhysicsScene().Simulate(.02f);
            Check(WorldSight.Blocked(camera.transform.position, target.transform.position + Vector3.up * .65f, camera.transform, target.transform),
                "A Default-layer pillar blocks guard sight", report);
            fade.occlusionDelay = .18f;
            Invoke(fade, "LateUpdate");
            Check(((IDictionary)typeof(CameraOcclusion).GetField("walls", Private).GetValue(fade)).Count == 0,
                "A brief obstruction does not immediately fade a wall", report);
            fade.occlusionDelay = 0;
            Invoke(fade, "LateUpdate");
            Check(((IDictionary)typeof(CameraOcclusion).GetField("walls", Private).GetValue(fade)).Count == 1,
                "Camera detects the wall between it and the player", report);
            Check(renderer.sharedMaterial != original && renderer.sharedMaterial.shader.name == "Goblin/PeepholeLit",
                "Peephole swaps in a private copy of the wall's own Lit material", report);
            Check(WorldSight.Blocked(camera.transform.position, target.transform.position + Vector3.up * .65f, camera.transform, target.transform),
                "A wall with a peephole still blocks guard sight", report);
            // Remove the obstruction and drive restoration from the final fade step.
            wall.transform.position += Vector3.right * 10;
            Physics.SyncTransforms();
            if (playing) scene.GetPhysicsScene().Simulate(.02f);
            Invoke(fade, "LateUpdate");
            Check(renderer.sharedMaterial == original && original.color == Color.gray,
                "Wall restores its exact original material when no longer blocking", report);
            // A wall well outside the peephole should keep its own material.
            wall.transform.position -= Vector3.right * 4f;
            Physics.SyncTransforms();
            if (playing) scene.GetPhysicsScene().Simulate(.02f);
            Invoke(fade, "LateUpdate");
            Check(renderer.sharedMaterial == original, "Wall outside the peephole keeps its original material", report);
            var bedAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HidingSpots/Bed.prefab");
            var bed = playing ? UnityEngine.Object.Instantiate(bedAsset) : (GameObject)PrefabUtility.InstantiatePrefab(bedAsset, scene);
            if (playing) SceneManager.MoveGameObjectToScene(bed, scene);
            bed.transform.position = new Vector3(1030, 0, 1000);
            var bedSpot = bed.GetComponentInChildren<HideSpot>();
            Check(bedSpot != null && bedSpot.Contains(bedSpot.transform.position), "Bed remains a valid hiding spot", report);
            Invoke(bedSpot, "Start");
            Check(bed.GetComponent<WorldHighlight>() != null, "Bed hide-zone highlights the actual bed model", report);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Models/Goblin/Gob_1 Animator Controller.controller");
            Check(controller.parameters.Any(p => p.name == "MoveSpeed") && controller.parameters.Any(p => p.name == "GaitRate"),
                "Animator has movement and playback-speed parameters", report);
            var machine = controller.layers[0].stateMachine;
            Check(machine.defaultState.name == "RigGob1_Idle", "Player starts in idle", report);
            foreach (var state in machine.states)
            {
                var clip = state.state.motion as AnimationClip;
                Check(clip != null && clip.isLooping, state.state.name + " animation loops", report);
                Check(state.state.transitions.Length == 1 && !state.state.transitions[0].hasExitTime &&
                    state.state.transitions[0].conditions.Any(c => c.parameter == "MoveSpeed"),
                    state.state.name + " transition follows actual movement", report);
            }
            report.AppendLine("PASS: Visibility and animation regression checks complete.");
        }
        catch (Exception error)
        {
            report.AppendLine("FAILED: " + error);
            Debug.LogException(error);
        }
        finally
        {
            // Runtime cleanup calls Destroy; editor validation releases preview objects explicitly.
            foreach (var highlight in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WorldHighlight>()))
            {
                var material = (Material)typeof(WorldHighlight).GetField("material", Private).GetValue(highlight);
                typeof(WorldHighlight).GetField("material", Private).SetValue(highlight, null);
                ((IList)typeof(WorldHighlight).GetField("outlines", Private).GetValue(highlight)).Clear();
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            }
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (playing) SceneManager.UnloadSceneAsync(scene);
            else EditorSceneManager.CloseScene(scene, true);
            if (original != null) UnityEngine.Object.DestroyImmediate(original);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/VisibilityValidationReport.txt", report.ToString());
        }
    }

    static GameObject Cube(Scene scene, Vector3 position)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(cube, scene);
        cube.transform.position = position;
        return cube;
    }

    static void Check(bool passed, string description, StringBuilder report)
    {
        if (!passed) throw new InvalidOperationException(description);
        report.AppendLine("PASS: " + description);
    }

    static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
}
