using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Creates saved scenes and a baked NavMesh from the project's existing art.
public static class CampaignBuilder
{
    const string Request = "Temp/BuildCampaign.request";
    const string Report = "Logs/CampaignBuildReport.txt";
    const string HubPath = "Assets/Scenes/ShopHub.unity";
    const string LevelPath = "Assets/Scenes/GoblinDungeon2.unity";
    static Material stone, dark, wood, gold, green, blue, purple;

    [InitializeOnLoadMethod]
    static void Initialize() => EditorApplication.update += ProcessRequest;

    static void ProcessRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Build();
    }

    [MenuItem("Goblin/Build Campaign Scenes")]
    public static void Build()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene generated = default;
        try
        {
            Directory.CreateDirectory("Logs");
            if (Application.isBatchMode && string.IsNullOrEmpty(previous.path))
            {
                previous = EditorSceneManager.OpenScene("Assets/Scenes/GoblinDungeon.unity");
            }
            stone = LoadMaterial("DungeonStone");
            dark = LoadMaterial("DungeonDarkStone");
            wood = LoadMaterial("DungeonWood");
            gold = LoadMaterial("LootGold");
            green = MakeMaterial("ExitJade", new Color(.12f, .65f, .36f), true);
            blue = MakeMaterial("VaultBlue", new Color(.12f, .3f, .55f), false);
            purple = MakeMaterial("HubVelvet", new Color(.26f, .12f, .29f), false);

            if (!File.Exists(LevelPath))
            {
                generated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(generated);
                BuildVault();
                EditorSceneManager.SaveScene(generated, LevelPath);
                EditorSceneManager.CloseScene(generated, true);
            }
            if (!File.Exists(HubPath))
            {
                generated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(generated);
                BuildHub();
                EditorSceneManager.SaveScene(generated, HubPath);
                EditorSceneManager.CloseScene(generated, true);
            }
            // Start in the safe house. Preserve unrelated build scenes.
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(HubPath, true),
                new EditorBuildSettingsScene("Assets/Scenes/GoblinDungeon.unity", true),
                new EditorBuildSettingsScene(LevelPath, true)
            };
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != HubPath && scene.path != LevelPath && scene.path != "Assets/Scenes/GoblinDungeon.unity") scenes.Add(scene);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Report, "PASS: ShopHub and GoblinDungeon2 saved. Royal Vault is 44 x 34m. Navigation baked. Hub is the build entry scene.\n");
        }
        catch (Exception error)
        {
            File.WriteAllText(Report, "FAILED: " + error);
            Debug.LogException(error);
            if (generated.IsValid() && generated.isLoaded) EditorSceneManager.CloseScene(generated, true);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    static void BuildVault()
    {
        SetupLighting(new Color(.44f, .5f, .62f));
        var environment = new GameObject("Royal Vault - architecture").transform;
        Cube("Vault floor - 44 x 34m", new Vector3(0, -.18f, 0), new Vector3(44, .35f, 34), stone, environment, true, 0);
        Boundary(environment, 22, 17, blue);
        // Three wings joined by wide passages above and below each partition.
        Wall("West gallery partition", -7, 0, .65f, 22, environment);
        Wall("East gallery partition", 7, 2, .65f, 22, environment);
        Wall("West storeroom", -15.2f, -5, 7.5f, .65f, environment);
        Wall("East treasury screen", 15.2f, 5, 7.5f, .65f, environment);
        Wall("Central vault screen", 0, 0, 5, .65f, environment);
        foreach (float x in new[] { -17f, -12f, 12f, 17f })
            foreach (float z in new[] { -1f, 10f })
                Cube("Gallery pillar", new Vector3(x, .9f, z), new Vector3(.8f, 1.8f, .8f), blue, environment);
        for (int x = -20; x < 22; x += 4)
            Cube("Brass floor inlay", new Vector3(x, .006f, -14.5f), new Vector3(1.8f, .012f, .12f), gold, environment, false);

        BakeNavigation(environment);
        var game = NewGame(false, 2, "The Royal Vault", new Vector3(-18, .05f, -13));
        Exit(new Vector3(-18, .035f, -13));
        Cube("Crown dais", new Vector3(18, .12f, 12), new Vector3(1.6f, .24f, 1.6f), blue, null, false);
        Spawn("Crown", new Vector3(18, .25f, 12));
        Vector3[] hiding =
        {
            new Vector3(-19, 0, -7), new Vector3(-10, 0, 13), new Vector3(-3, 0, -9),
            new Vector3(3, 0, 11), new Vector3(11, 0, -10), new Vector3(19, 0, 8)
        };
        foreach (var position in hiding) Spawn("HideSpot", position);
        Vector3[] coins =
        {
            new Vector3(-18,0,-10), new Vector3(-12,0,-10), new Vector3(-10,0,-6), new Vector3(-18,0,3),
            new Vector3(-13,0,6), new Vector3(-10,0,11), new Vector3(-5,0,14), new Vector3(-3,0,8),
            new Vector3(0,0,5), new Vector3(3,0,2), new Vector3(0,0,-5), new Vector3(-3,0,-12),
            new Vector3(3,0,-14), new Vector3(9,0,-12), new Vector3(15,0,-10), new Vector3(19,0,-6),
            new Vector3(12,0,0), new Vector3(18,0,2), new Vector3(10,0,8), new Vector3(14,0,11),
            new Vector3(0,0,13), new Vector3(-19,0,13), new Vector3(19,0,-12), new Vector3(10,0,14)
        };
        foreach (var position in coins) Spawn("Coin", position);
        foreach (var position in new[] { new Vector3(-18,0,11), new Vector3(-1,0,11), new Vector3(2,0,-10), new Vector3(17,0,-10), new Vector3(11,0,11), new Vector3(19,0,9) }) Spawn("Gem", position);
        Guard(game, "West watch", new[] { V(-15,-9), V(-10,-7), V(-10,8), V(-19,8), V(-19,-3) });
        Guard(game, "Gallery sentry", new[] { V(-4,-10), V(3,-10), V(4,-3), V(4,9), V(-4,9) });
        Guard(game, "Treasury watch", new[] { V(11,8), V(19,8), V(19,13), V(10,13) });
        Guard(game, "South patrol", new[] { V(9,-12), V(19,-12), V(19,2), V(11,2) });
        Guard(game, "North patrol", new[] { V(-18,14), V(-3,14), V(5,14), V(5,8), V(-4,8), V(-10,14) });
        WireScene(game);
    }

    static void BuildHub()
    {
        SetupLighting(new Color(.65f, .55f, .42f));
        var environment = new GameObject("Safe house - architecture").transform;
        Cube("Safe house floor", new Vector3(0, -.18f, 0), new Vector3(26, .35f, 20), dark, environment, true, 0);
        Boundary(environment, 13, 10, wood);
        Cube("Merchant rug", new Vector3(0, .015f, 3), new Vector3(6, .025f, 5), purple, environment, false);
        Cube("Heist hall runner", new Vector3(0, .015f, -4), new Vector3(17, .025f, 2.5f), purple, environment, false);
        Cube("Merchant counter", new Vector3(0, .5f, 6), new Vector3(5, 1, 1), wood, environment);
        Cube("Counter trim", new Vector3(0, 1.04f, 6), new Vector3(5.2f, .12f, 1.15f), gold, environment);
        var merchant = Spawn("Goblin", new Vector3(0, .05f, 7.4f));
        UnityEngine.Object.DestroyImmediate(merchant.GetComponent<Player>());
        UnityEngine.Object.DestroyImmediate(merchant.GetComponent<CharacterController>());
        merchant.name = "The fence - merchant";
        merchant.transform.rotation = Quaternion.Euler(0, 180, 0);
        Spawn("Crate", new Vector3(4, 0, 7));
        Spawn("Crate", new Vector3(-4, 0, 7));
        foreach (float x in new[] { -10f, 10f })
            foreach (float z in new[] { -7f, 7f })
                Cube("Timber support", new Vector3(x, 1, z), new Vector3(.6f, 2, .6f), wood, environment);
        var game = NewGame(true, 1, "The Goblin's Rest", new Vector3(0, .05f, -6));
        var hub = game.gameObject.AddComponent<ShopHub>();
        //hub.stations = new[]
        //{
        //    Station("Browse the fence's wares", 0, new Vector3(0, 0, 4)),
        //    Station("Enter Level 1 / The Dungeon", 1, new Vector3(-8, 0, -1)),
        //    Station("Enter Level 2 / The Royal Vault", 2, new Vector3(8, 0, -1))
        //};
        Gate("THE DUNGEON", new Vector3(-8, 0, 0), green);
        Gate("ROYAL VAULT", new Vector3(8, 0, 0), blue);
        WireScene(game);
    }

    static GoblinDungeon NewGame(bool hub, int level, string title, Vector3 spawn)
    {
        var manager = new GameObject("Game manager");
        var game = manager.AddComponent<GoblinDungeon>();
        game.isHub = hub;
        game.levelNumber = level;
        game.levelTitle = title;
        game.player = Spawn("Goblin", spawn).GetComponent<Player>();
        // Keep walking slower than pursuing guards, including when carrying a crown.
        game.player.walkSpeed = 3;
        var hud = manager.AddComponent<GoblinHUD>();
        hud.game = game;
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.rotation = Quaternion.Euler(48, -45, 0);
        var camera = cameraObject.GetComponent<Camera>();
        camera.backgroundColor = new Color(.04f, .06f, .08f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.farClipPlane = 150;
        var follow = cameraObject.AddComponent<DungeonCamera>();
        follow.target = game.player.transform;
        follow.zoom = hub ? 11 : 9;
        follow.offset = new Vector3(20, 25, -20);
        cameraObject.transform.position = spawn + follow.offset;
        return game;
    }

    static void WireScene(GoblinDungeon game)
    {
        // Restrict wiring to the generated scene while other scenes remain open.
        var scene = game.gameObject.scene;
        var guards = new List<SearchAndChase>();
        var spots = new List<HideSpot>();
        var items = new List<Collectible>();
        foreach (var root in scene.GetRootGameObjects())
        {
            guards.AddRange(root.GetComponentsInChildren<SearchAndChase>());
            spots.AddRange(root.GetComponentsInChildren<HideSpot>());
            items.AddRange(root.GetComponentsInChildren<Collectible>());
        }
        game.guards = guards.ToArray();
        game.hidingSpots = spots.ToArray();
        game.collectibles = items.ToArray();
    }

    static void Guard(GoblinDungeon game, string name, Vector3[] route)
    {
        var guard = Spawn("Guard", route[0]).GetComponent<SearchAndChase>();
        guard.name = name;
        guard.target = game.player.gameObject;
        guard.radius = 8;
        guard.angle = 95;
        var root = new GameObject(name + " route").transform;
        guard.patrolPoints = new Transform[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            var point = new GameObject("Waypoint " + (i + 1)).transform;
            point.SetParent(root);
            point.position = route[i];
            guard.patrolPoints[i] = point;
        }
    }

    static void BakeNavigation(Transform environment)
    {
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(environment, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
        var settings = NavMesh.GetSettingsByID(0);
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(60, 12, 50)), Vector3.zero, Quaternion.identity);
        if (data == null) throw new InvalidOperationException("Royal Vault navigation bake failed.");
        string path = "Assets/Prefabs/RoyalVaultNavMesh.asset";
        AssetDatabase.CreateAsset(data, path);
        var navigation = new GameObject("Royal Vault navigation").AddComponent<BakedNavigation>();
        navigation.data = data;
        navigation.enabled = false;
        navigation.enabled = true;
    }

    static HubStation Station(string name, int destination, Vector3 position)
    {
        var station = new GameObject(name).AddComponent<HubStation>();
        station.transform.position = position;
        station.stationName = name;
        station.destination = destination;
        return station;
    }

    static void Gate(string name, Vector3 position, Material color)
    {
        Cube(name + " departure pad", position + Vector3.up * .03f, new Vector3(3.5f, .06f, 3), color, null, false);
        foreach (float x in new[] { -1.9f, 1.9f })
            Cube(name + " gatepost", position + new Vector3(x, 1, .8f), new Vector3(.4f, 2, .4f), gold);
        Cube(name + " lintel", position + new Vector3(0, 2.15f, .8f), new Vector3(4.2f, .3f, .5f), wood);
    }

    static void Exit(Vector3 position)
    {
        var pad = Cube("Exit - bring the crown here", position, new Vector3(2.6f, .06f, 2.6f), green, null, false);
        pad.AddComponent<DungeonExit>();
    }

    static void SetupLighting(Color ambient)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        var light = new GameObject("Warm overhead light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.color = new Color(1, .89f, .72f);
        light.transform.rotation = Quaternion.Euler(55, -35, 0);
        light.shadows = LightShadows.Soft;
    }

    static void Boundary(Transform parent, float x, float z, Material material)
    {
        Cube("North boundary", new Vector3(0, .8f, z), new Vector3(x * 2, 1.6f, .6f), material, parent);
        Cube("South boundary", new Vector3(0, .8f, -z), new Vector3(x * 2, 1.6f, .6f), material, parent);
        Cube("West boundary", new Vector3(-x, .8f, 0), new Vector3(.6f, 1.6f, z * 2), material, parent);
        Cube("East boundary", new Vector3(x, .8f, 0), new Vector3(.6f, 1.6f, z * 2), material, parent);
    }

    static void Wall(string name, float x, float z, float width, float depth, Transform parent)
        => Cube(name, new Vector3(x, .75f, z), new Vector3(width, 1.5f, depth), blue, parent);

    static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null, bool solid = true, int layer = 8)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.layer = layer;
        if (!solid) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    static Vector3 V(float x, float z) => new Vector3(x, .05f, z);
    static GameObject Spawn(string prefab, Vector3 position)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab");
        if (asset == null) throw new FileNotFoundException(prefab);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, SceneManager.GetActiveScene());
        instance.transform.position = position;
        return instance;
    }

    static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/" + name + ".mat");
    static Material MakeMaterial(string name, Color color, bool unlit)
    {
        string path = "Assets/Prefabs/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        material.name = name;
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}


