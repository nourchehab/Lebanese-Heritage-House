#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Heritage > Set Up Playable Scene
/// Adds the first-person player, the gatekeeper, the door between the courtyard
/// and the connecting room, and collisions for the scanned models. Safe to run
/// again: it rebuilds everything under the "Gameplay" object.
/// </summary>
public static class HeritageSceneSetup
{
    const string RootName = "Gameplay";

    // Positions worked out from the scans (Unity world coordinates).
    static readonly Vector3 PlayerSpawn = new Vector3(3.0f, 0.05f, -1.8f);
    const float PlayerSpawnYaw = -90f; // facing the house

    static readonly Vector3 HousekeeperPos = new Vector3(0.5f, 0.0f, -1.8f);
    const float HousekeeperYaw = 90f; // facing the garden

    static readonly Vector3 GatekeeperExit = new Vector3(3.5f, 0.0f, -1.6f); // back across the garden

    // Doors sit on the wall plane; each one only works from its own side.
    static readonly Vector3 OutsideDoorPos = new Vector3(-0.95f, 1.0f, -0.595f);
    const float OutsideDoorFacing = 90f;  // courtyard side
    static readonly Vector3 ArriveInRoom = new Vector3(-2.1f, 0.25f, -0.595f);
    const float ArriveInRoomYaw = -90f; // facing the red door

    static readonly Vector3 InsideDoorPos = new Vector3(-1.07f, 1.0f, -0.595f);
    const float InsideDoorFacing = -90f;  // room side
    static readonly Vector3 ArriveInCourtyard = new Vector3(0.9f, 0.05f, -0.595f);
    const float ArriveInCourtyardYaw = 90f; // facing the garden

    [MenuItem("Heritage/Set Up Playable Scene")]
    public static void SetUp()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // Start fresh
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Set Up Playable Scene");

        int colliders = AddColliders("courtyard") + AddColliders("room_v5");
        int cameras = DisableOtherCameras();

        CreatePlayer(root.transform);
        CreateHousekeeper(root.transform);
        CreateDoor(root.transform, "Door_ToConnectingRoom", "Enter the connecting room", OutsideDoorPos, OutsideDoorFacing, ArriveInRoom, ArriveInRoomYaw, -1f);
        CreateDoor(root.transform, "Door_ToCourtyard", "Go out to the courtyard", InsideDoorPos, InsideDoorFacing, ArriveInCourtyard, ArriveInCourtyardYaw, -1f);
        CreateAmbience(root.transform);
        CreateFountainBlocker(root.transform);
        CreateFountainWater(root.transform);
        CreateCarvedStonePanel(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Heritage: playable scene ready ({colliders} colliders added, {cameras} extra cameras turned off). Press Play to walk around.");
    }

    static int AddColliders(string rootName)
    {
        var root = GameObject.Find(rootName);
        if (root == null) { Debug.LogWarning($"Heritage: '{rootName}' not found in the scene."); return 0; }
        int added = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
            mc.sharedMesh = mf.sharedMesh;
            added++;
        }
        return added;
    }

    static int DisableOtherCameras()
    {
        int n = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            if (cam.transform.root.name == RootName) continue;
            Undo.RecordObject(cam.gameObject, "Disable camera");
            cam.gameObject.SetActive(false);
            n++;
        }
        return n;
    }

    static void CreatePlayer(Transform parent)
    {
        var player = new GameObject("Player");
        player.transform.SetParent(parent, false);
        player.transform.SetPositionAndRotation(PlayerSpawn, Quaternion.Euler(0f, PlayerSpawnYaw, 0f));
        player.tag = "Player";
        player.layer = 2; // Ignore Raycast

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.4f;
        cc.radius = 0.24f;
        cc.center = new Vector3(0f, 0.7f, 0f);
        cc.stepOffset = 0.25f;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.03f;

        var camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();

        var fps = player.AddComponent<FirstPersonController>();
        fps.cameraPivot = camGo.transform;
        fps.groundMask = ~(1 << 2);
        player.AddComponent<PlayerInteractor>();
    }

    static void CreateHousekeeper(Transform parent)
    {
        var hk = new GameObject("Gatekeeper");
        hk.transform.SetParent(parent, false);
        hk.transform.SetPositionAndRotation(HousekeeperPos, Quaternion.Euler(0f, HousekeeperYaw, 0f));

        // Simple stand-in figure: replace with a character model later.
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body (placeholder)";
        body.transform.SetParent(hk.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.75f, 0.4f);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterial("Gatekeeper_Clothes", new Color(0.18f, 0.22f, 0.32f));

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head (placeholder)";
        head.transform.SetParent(hk.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.63f, 0f);
        head.transform.localScale = Vector3.one * 0.27f;
        head.GetComponent<Renderer>().sharedMaterial = GetMaterial("Gatekeeper_Skin", new Color(0.78f, 0.6f, 0.47f));
        Object.DestroyImmediate(head.GetComponent<Collider>());

        var focus = new GameObject("TalkPoint");
        focus.transform.SetParent(hk.transform, false);
        focus.transform.localPosition = new Vector3(0f, 1.3f, 0f);

        var guide = hk.AddComponent<HousekeeperGuide>();
        guide.radius = 2.3f;
        guide.focusPoint = focus.transform;

        var exit = new GameObject("Gatekeeper_Exit");
        exit.transform.SetParent(parent, false);
        exit.transform.position = GatekeeperExit;
        guide.exitPoint = exit.transform;
    }

    static void CreateDoor(Transform parent, string name, string prompt, Vector3 pos, float facing, Vector3 arrive, float arriveYaw, float ambienceVolume)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent, false);
        door.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, facing, 0f));

        var dest = new GameObject(name + "_Arrival");
        dest.transform.SetParent(parent, false);
        dest.transform.SetPositionAndRotation(arrive, Quaternion.Euler(0f, arriveYaw, 0f));

        var portal = door.AddComponent<DoorPortal>();
        portal.promptText = prompt;
        portal.destination = dest.transform;
        portal.radius = 1.4f;
        portal.ambienceVolume = ambienceVolume;
    }

    // The scanned fountain rim slopes gently, so without this the player can
    // walk up onto it and get stuck. An invisible round wall keeps them off.
    static readonly Vector3 FountainCenter = new Vector3(2.17f, 0f, -0.45f);
    const float FountainRadius = 0.72f;

    static void CreateFountainBlocker(Transform parent)
    {
        var go = new GameObject("FountainBlocker (invisible)");
        go.transform.SetParent(parent, false);
        go.transform.position = FountainCenter;
        var col = go.AddComponent<CapsuleCollider>();
        col.radius = FountainRadius;
        col.height = 4f;
        col.center = new Vector3(0f, 1.5f, 0f);
    }

    // Water effect from the teammate's project, placed on the fountain spout.
    const string FountainWaterPrefab = "Assets/Fountain/FountainWater.prefab";
    const string FountainSoundPath = "Assets/Fountain/fountain_water_loop.wav";
    static readonly Vector3 FountainSpout = new Vector3(2.151f, 0.707f, -0.4956f);

    static void CreateFountainWater(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FountainWaterPrefab);
        if (prefab == null) { Debug.LogWarning($"Heritage: {FountainWaterPrefab} not found."); return; }
        var water = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        water.transform.position = FountainSpout;
        if (water.GetComponent<FountainProximity>() == null) water.AddComponent<FountainProximity>();

        // Water sound: comes from the fountain, louder up close.
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(FountainSoundPath);
        if (clip == null) { Debug.LogWarning($"Heritage: {FountainSoundPath} not found."); return; }
        var src = water.GetComponent<AudioSource>();
        if (src == null) src = water.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;               // 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 6f;
        src.dopplerLevel = 0f;
    }

    // First item: the carved stone panel on the fountain, picked up from the
    // door side (the side of the fountain facing the house).
    static readonly Vector3 CarvedPanelPos = new Vector3(1.45f, 0.45f, -0.5f);

    static void CreateCarvedStonePanel(Transform parent)
    {
        var go = new GameObject("Item1_CarvedStonePanel");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(CarvedPanelPos, Quaternion.Euler(0f, -90f, 0f)); // usable from the door side
        var pickup = go.AddComponent<ItemPickup>();
        pickup.itemName = "Carved Stone Panel";
        pickup.slot = 0;
        pickup.radius = 1.3f;
        pickup.revealImage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Items/carved_stone_panel.png");
        pickup.foundSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Items/item_found.wav");
        if (pickup.revealImage == null || pickup.foundSound == null)
            Debug.LogWarning("Heritage: carved stone panel image or sound not found in Assets/Items.");
    }

    const string AmbiencePath = "Assets/Audio/courtyard_ambience.mp3";

    static void CreateAmbience(Transform parent)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbiencePath);
        if (clip == null) { Debug.LogWarning($"Heritage: ambience clip not found at {AmbiencePath}."); return; }
        var go = new GameObject("Ambience");
        go.transform.SetParent(parent, false);
        var amb = go.AddComponent<AmbientAudio>();
        amb.clip = clip;

        // The connecting room: inside this box the ambience plays at 30%.
        var zone = new GameObject("ConnectingRoom_QuietZone");
        zone.transform.SetParent(parent, false);
        zone.transform.position = new Vector3(-2.24f, 2.6f, -0.57f);
        zone.layer = 2; // Ignore Raycast
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.6f, 5.6f, 4.3f);
        amb.quietZone = box;
        amb.quietZoneVolume = 0.3f;
    }

    static Material GetMaterial(string name, Color color)
    {
        string folder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = $"{folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
#endif
