using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Builds Location 1 from STORY_SPEC: the inside of Halyna's house (the great-grandmother's house) as its own location,
// the door from the L2 street, the chest of drawers (Key Choice mini-game) and its three finds.
// Visuals are clearly named PH_ placeholders until the Batch A art arrives; the layout constants below are what the art
// is specified against (see docs/IMPLEMENTATION_STATUS.md, "Art needed").
// Idempotent: rebuilds the "House Interior" root and the street door; doesn't touch other areas (e.g. the Mikvah).
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod HouseInteriorBuilder.BuildBatch -logFile <log>
public static class HouseInteriorBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string RootName = "House Interior";
    const string LocationId = "4";
    const string StreetDoorName = "Halyna House Door";

    //One camera view (orthographic size 17.5, 16:9), so the camera stays still inside the house
    static readonly Vector2 Center = new Vector2(0f, -240f);
    const float Width = 62.22f, Height = 35f;
    const float FloorTop = -250f; //Walking level: 7.5 units above the bottom of the view, like the streets

    //Where the painted door of Halyna's house appears in Snapshots/2_L2_Street/tile_05.png (camera x 54.3)
    const float StreetDoorApparentX = 58.27f, CaptureCameraX = 54.3f;
    const float StreetArrivalY = -85.4f, StreetDoorCenterY = -83f, StreetArrivalOffset = 2.8f;

    //Interior layout, x relative to the room centre
    const float ExitDoorX = -24f, EntryArrivalX = -19f, WardrobeX = -9f, WindowX = 5f, ChestX = 17f;

    static readonly Color WallColor = new Color(0.80f, 0.75f, 0.64f);
    static readonly Color WainscotColor = new Color(0.66f, 0.58f, 0.47f);
    static readonly Color BeamColor = new Color(0.42f, 0.30f, 0.22f);
    static readonly Color FloorColor = new Color(0.48f, 0.33f, 0.24f);
    static readonly Color DoorColor = new Color(0.29f, 0.44f, 0.41f); //teal like the street doors
    static readonly Color GlassColor = new Color(0.66f, 0.78f, 0.80f);
    static readonly Color WardrobeColor = new Color(0.40f, 0.26f, 0.17f);
    static readonly Color ChestColor = new Color(0.52f, 0.34f, 0.22f);

    //Editor workflow: run from the menu, then save the scene yourself (Ctrl+S). Captures are refreshed automatically.
    [MenuItem("Shtetl/Build House Interior")]
    public static void BuildMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(LocationCapture.CaptureAll() + "\nHouse interior rebuilt. Save the scene (Ctrl+S) to keep it.");
    }

    public static void BuildBatch()
    {
        int exitCode = 0;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            string summary = LocationCapture.CaptureAll();
            if (summary.Contains("FAILED") || summary.Contains("TRANSITION PROBLEM")) exitCode = 2;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }

    static void Build()
    {
        Location street = FindLocation("2");
        Transform house = Object.FindObjectsByType<QuestLocationTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => new SerializedObject(t).FindProperty("locationId").stringValue == "grandma_house")?.transform;
        if (house == null) throw new System.Exception("Halyna's house (QuestLocationTrigger 'grandma_house') not found.");

        Sprite square = Find("L3/Background/Fog/Square (2)").GetComponent<SpriteRenderer>().sprite;
        int groundLayer = Find("L2/Background/groundLayer0/temp/temp").gameObject.layer;
        GameObject waypointTemplate = Find("L2/L2->L1 Return Waypoint").gameObject;

        //Room root and boundary
        foreach (var old in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == RootName))
            Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        root.layer = street.gameObject.layer;
        root.transform.position = new Vector3(Center.x, Center.y, 0f);
        var boundary = root.AddComponent<PolygonCollider2D>();
        boundary.isTrigger = true;
        float hw = Width / 2f, hh = Height / 2f;
        boundary.points = new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh), new Vector2(-hw, hh) };
        var location = root.AddComponent<Location>();
        var streetSo = new SerializedObject(street);
        var so = new SerializedObject(location);
        so.FindProperty("locationID").stringValue = LocationId;
        so.FindProperty("locationName").stringValue = RootName;
        so.FindProperty("mapBoundary").objectReferenceValue = boundary;
        so.FindProperty("musicClip").objectReferenceValue = streetSo.FindProperty("musicClip").objectReferenceValue;
        so.FindProperty("musicVolume").floatValue = streetSo.FindProperty("musicVolume").floatValue * 0.6f; //quieter inside
        so.ApplyModifiedPropertiesWithoutUndo();

        //Placeholder room (PH_ = replace with Batch A art)
        float left = Center.x - hw, bottom = Center.y - hh, top = Center.y + hh;
        Block(root.transform, "PH_Wall", square, WallColor, Center.x, Center.y, Width, Height, "Background", 0);
        Block(root.transform, "PH_Wainscot", square, WainscotColor, Center.x, FloorTop + 1.5f, Width, 3f, "Background", 1);
        Block(root.transform, "PH_CeilingBeam", square, BeamColor, Center.x, top - 0.75f, Width, 1.5f, "Background", 1);
        Block(root.transform, "PH_Floor", square, FloorColor, Center.x, (bottom + FloorTop) / 2f, Width, FloorTop - bottom, "Ground2", 0);
        Block(root.transform, "PH_ExitDoor", square, DoorColor, X(ExitDoorX), FloorTop + 2.8f, 3.2f, 5.6f, "Background", 2);
        Block(root.transform, "PH_WindowFrame", square, DoorColor, X(WindowX), FloorTop + 6.5f, 4.6f, 4.6f, "Background", 2);
        Block(root.transform, "PH_WindowGlass", square, GlassColor, X(WindowX), FloorTop + 6.5f, 4f, 4f, "Background", 3);
        Block(root.transform, "PH_Wardrobe", square, WardrobeColor, X(WardrobeX), FloorTop + 2.9f, 3.4f, 5.8f, "Ground1", 10);

        //Floor and walls the player can't pass
        Solid(root.transform, "Floor Collider", groundLayer, Center.x, (bottom + FloorTop) / 2f, Width, FloorTop - bottom);
        Solid(root.transform, "Left Wall", groundLayer, left - 0.5f, Center.y, 1f, Height);
        Solid(root.transform, "Right Wall", groundLayer, left + Width + 0.5f, Center.y, 1f, Height);

        //Doors: the street door opens when Halyna gives the keys (FindHouse reward); the way out is always open
        Transform entryArrival = Point(root.transform, "House Interior Entry Arrival", X(EntryArrivalX), FloorTop + 3f);
        MapTransition exit = Door(root.transform, "House Interior Exit", waypointTemplate, X(ExitDoorX), FloorTop + 2.8f, 3.2f, 5.6f);

        Transform oldDoor = house.Find(StreetDoorName);
        if (oldDoor != null) Object.DestroyImmediate(oldDoor.gameObject);
        Transform oldArrival = street.transform.Find(StreetDoorName + " Arrival");
        if (oldArrival != null) Object.DestroyImmediate(oldArrival.gameObject);

        //The house moves with parallax, so the door is its child; the arrival is a plain point where the door appears
        float effect = ParallaxEffect(house);
        float doorEditX = StreetDoorApparentX - effect * CaptureCameraX;
        //Arrive just right of the door (Halyna stands on its left); the camera follows the player there,
        //so the door appears at edit x + effect * arrival x
        float arrivalX = (doorEditX + StreetArrivalOffset) / (1f - effect);
        MapTransition streetDoor = Door(house, StreetDoorName, waypointTemplate, doorEditX, StreetDoorCenterY, 2f, 6f);
        Transform streetArrival = Point(street.transform, StreetDoorName + " Arrival", arrivalX, StreetArrivalY);

        Link(streetDoor, location, entryArrival, false);
        Link(exit, street, streetArrival, true);
        int doorRewardId = RegisterTransition(streetDoor);

        //Chest of drawers: Key Choice mini-game, then the three finds
        BuildChest(root.transform, square);
        SetQuestRewards(doorRewardId);
        RegisterJournalTopics("FamilyHouse", "FamilyPhoto", "SchoolPhoto", "WeddingInvitation");
        SetUpHalyna(StreetDoorApparentX); //where the door appears while the player is beside Halyna
        NameBoxKeys();

        Debug.Log($"House interior built. Street door edit x {doorEditX:0.##} (parallax {effect}), arrival x {arrivalX:0.##}, reward id {doorRewardId}.");
    }

    static float X(float relative) => Center.x + relative;

    static void BuildChest(Transform root, Sprite square)
    {
        GameObject boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MiniGame Items/Box 1.prefab");
        Sprite boxSprite = boxPrefab.GetComponent<SpriteRenderer>().sprite;
        Sprite photoSprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Photo.prefab").GetComponent<Image>().sprite;

        var chest = new GameObject("Chest of Drawers");
        chest.layer = boxPrefab.layer;
        chest.transform.SetParent(root, false);
        chest.transform.position = new Vector3(X(ChestX), FloorTop + 1.3f, 0f);
        var trigger = chest.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4.5f, 4f);
        Block(chest.transform, "PH_ChestOfDrawers", square, ChestColor, X(ChestX), FloorTop + 1.3f, 3.8f, 2.6f, "Ground1", 10);
        Block(chest.transform, "PH_DrawerLine", square, WardrobeColor, X(ChestX), FloorTop + 1.6f, 3.4f, 0.15f, "Ground1", 11);

        var contents = new GameObject("Drawer Contents");
        contents.transform.SetParent(chest.transform, false);
        var inspect = contents.AddComponent<InspectPoint>();

        Quest leadSchool = LoadQuest("LeadSchool"), leadSynagogue = LoadQuest("LeadSynagogue");
        var pages = new SerializedObject(inspect);
        var list = pages.FindProperty("pages");
        list.arraySize = 4;
        //Placeholder images (PH): the Batch A close-ups replace them.
        //Titles fit one line of the popup title (600 px at size 70); texts stay within its 5-line description box.
        SetPage(list.GetArrayElementAtIndex(0), boxSprite, "Old drawer",
            "The drawer is open. Inside are old photos and papers.", "", null, "");
        SetPage(list.GetArrayElementAtIndex(1), photoSprite, "Family photo",
            "A big Jewish family stands in their hallway. Behind them, a small square on the wall has no paint.",
            "family_photo", null, "");
        SetPage(list.GetArrayElementAtIndex(2), photoSprite, "School photo",
            "An old class photo: girls and boys with their teacher. Did great-grandma study at this school?",
            "school_photo", leadSchool, "");
        SetPage(list.GetArrayElementAtIndex(3), photoSprite, "Invitation",
            "An old invitation to a wedding in the synagogue. Where was the synagogue?",
            "wedding_invitation", leadSynagogue, "house_finds_seen");
        pages.ApplyModifiedPropertiesWithoutUndo();

        var item = chest.AddComponent<MiniGameItem>();
        var itemSo = new SerializedObject(item);
        itemSo.FindProperty("miniGameItemID").intValue = 2; //Key Choice in the MiniGameRegistry
        itemSo.FindProperty("Name").stringValue = "Chest of drawers";
        itemSo.FindProperty("questObjectiveId").stringValue = "open_chest_of_drawers";
        itemSo.FindProperty("revealOnComplete").objectReferenceValue = inspect;
        itemSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetPage(SerializedProperty page, Sprite image, string title, string text, string topicId, Quest startsQuest, string objectiveId)
    {
        page.FindPropertyRelative("image").objectReferenceValue = image;
        page.FindPropertyRelative("title").stringValue = title;
        page.FindPropertyRelative("text").stringValue = text;
        page.FindPropertyRelative("journalTopicId").stringValue = topicId;
        page.FindPropertyRelative("journalStage").intValue = 0;
        page.FindPropertyRelative("questObjectiveId").stringValue = objectiveId;
        page.FindPropertyRelative("startsQuest").objectReferenceValue = startsQuest;
    }

    //The house sequence: FindHouse (no reward) → flowers (CollectFlowers: the husband's box with the key search) →
    //the old keys from the box (FindKeys: opens the house door) → the chest of drawers (OpenChest)
    static void SetQuestRewards(int doorRewardId)
    {
        SetRewards(LoadQuest("FindHouse"));
        SetRewards(LoadQuest("FindKeys"), (RewardType.Portal, doorRewardId, 1));
        AssetDatabase.SaveAssets();
    }

    static void SetRewards(Quest quest, params (RewardType type, int id, int amount)[] list)
    {
        var so = new SerializedObject(quest);
        var rewards = so.FindProperty("questRewards");
        rewards.arraySize = list.Length;
        for (int i = 0; i < list.Length; i++)
            SetReward(rewards.GetArrayElementAtIndex(i), list[i].type, list[i].id, list[i].amount);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(quest);
    }

    //Halyna: five dialogue stages; she paces in her garden left of the door, so her talk zone and the door never meet
    //(talk zone radius 1.67, door trigger 2 wide). Meeting her in the garden also counts as finding the house.
    static void SetUpHalyna(float doorApparentX)
    {
        NPC halyna = Object.FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(n => n.npcId == "NPC Hisa Teacher");
        if (halyna == null) throw new System.Exception("Halyna (npcId 'NPC Hisa Teacher') not found.");

        var so = new SerializedObject(halyna);
        var stages = so.FindProperty("dialogueStages");
        stages.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            var stage = AssetDatabase.LoadAssetAtPath<NPCDialogue>($"Assets/Scripts/Scriptable Objects/NPC Dialogues/Halyna NPCDialogue  Part{i}.asset");
            if (stage == null) throw new System.Exception($"Halyna dialogue Part{i} not found.");
            stages.GetArrayElementAtIndex(i).objectReferenceValue = stage;
        }
        so.ApplyModifiedProperties();

        //Halyna isn't parallaxed; the door is, so it is compared where it appears when the player stands at it
        float right = doorApparentX - 1f - 1.67f - 0.6f, left = right - 3f;
        var waypoints = (Transform)new SerializedObject(halyna.GetComponent<WaypointMover>()).FindProperty("waypointParent").objectReferenceValue;
        MoveX(waypoints.GetChild(0), right);
        MoveX(waypoints.GetChild(1), left);
        MoveX(halyna.transform, right);

        var trigger = halyna.GetComponent<QuestLocationTrigger>();
        if (trigger == null) trigger = Undo.AddComponent<QuestLocationTrigger>(halyna.gameObject);
        var triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("locationId").stringValue = "grandma_house";
        triggerSo.ApplyModifiedProperties();
        Debug.Log($"Halyna paces x {left:0.##}..{right:0.##} (door appears at {doorApparentX:0.##}).");
    }

    static void MoveX(Transform t, float x)
    {
        Undo.RecordObject(t, "Move Halyna");
        t.position = new Vector3(x, t.position.y, t.position.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
    }

    //The key search in the husband's box: labels that match the inventory names
    static void NameBoxKeys()
    {
        var dictionary = Object.FindFirstObjectByType<ItemDictionary>(FindObjectsInactive.Include);
        int taggedKey = ItemId(dictionary, "Assets/Prefabs/Items/Key Variant.prefab");
        int plainKey = ItemId(dictionary, "Assets/Prefabs/Items/Key Variant 1.prefab");
        foreach (var item in Object.FindObjectsByType<HiddenObjectItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(item);
            int id = so.FindProperty("itemId").intValue;
            if (id != taggedKey && id != plainKey) continue;
            so.FindProperty("displayName").stringValue = id == taggedKey ? "Key with a tag" : "Old key";
            so.ApplyModifiedProperties();
        }
    }

    static void SetReward(SerializedProperty reward, RewardType type, int id, int amount)
    {
        reward.FindPropertyRelative("type").enumValueIndex = (int)type;
        reward.FindPropertyRelative("rewardID").intValue = id;
        reward.FindPropertyRelative("amount").intValue = amount;
    }

    //ItemDictionary numbers its prefabs 1..n in list order
    static int ItemId(ItemDictionary dictionary, string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<Item>(prefabPath);
        int index = dictionary.itemPrefabs.IndexOf(prefab);
        if (index < 0) throw new System.Exception($"{prefabPath} is not in the ItemDictionary.");
        return index + 1;
    }

    //TransitionDictionary numbers its transitions 1..n in list order; Portal rewards use that number
    static int RegisterTransition(MapTransition transition)
    {
        var dictionary = Object.FindFirstObjectByType<TransitionDictionary>(FindObjectsInactive.Include);
        dictionary.transitionPrefabs.RemoveAll(t => t == null);
        if (!dictionary.transitionPrefabs.Contains(transition)) dictionary.transitionPrefabs.Add(transition);
        EditorUtility.SetDirty(dictionary);
        return dictionary.transitionPrefabs.IndexOf(transition) + 1;
    }

    static void RegisterJournalTopics(params string[] assetNames)
    {
        var journal = Object.FindFirstObjectByType<JournalController>(FindObjectsInactive.Include);
        var so = new SerializedObject(journal);
        var topics = so.FindProperty("allTopics");
        foreach (string name in assetNames)
        {
            var topic = AssetDatabase.LoadAssetAtPath<JournalTopicData>($"Assets/Scripts/Scriptable Objects/Journal/{name}.asset");
            if (topic == null) throw new System.Exception($"Journal topic {name} not found.");
            bool present = Enumerable.Range(0, topics.arraySize).Any(i => topics.GetArrayElementAtIndex(i).objectReferenceValue == topic);
            if (present) continue;
            topics.arraySize++;
            topics.GetArrayElementAtIndex(topics.arraySize - 1).objectReferenceValue = topic;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Quest LoadQuest(string name)
    {
        var quest = AssetDatabase.LoadAssetAtPath<Quest>($"Assets/Scripts/Scriptable Objects/Quests/{name}.asset");
        if (quest == null) throw new System.Exception($"Quest {name} not found.");
        return quest;
    }

    //Invisible door trigger with a MapTransition; its SpriteRenderer stays empty (the door is painted on the house/room)
    static MapTransition Door(Transform parent, string name, GameObject template, float x, float y, float width, float height)
    {
        var go = new GameObject(name);
        go.layer = template.layer;
        go.tag = template.tag;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        Vector3 scale = go.transform.lossyScale;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width / Mathf.Abs(scale.x), height / Mathf.Abs(scale.y));
        go.AddComponent<SpriteRenderer>();
        var transition = go.AddComponent<MapTransition>();
        transition.Name = name;
        return transition;
    }

    static void Link(MapTransition from, Location target, Transform arrival, bool alwaysOpen)
    {
        var so = new SerializedObject(from);
        so.FindProperty("targetLocation").objectReferenceValue = target;
        so.FindProperty("teleportTargetPosition").objectReferenceValue = arrival;
        so.FindProperty("isReturnTransition").boolValue = alwaysOpen;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform Point(Transform parent, string name, float x, float y)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = new Vector3(x, y, 0f);
        return t;
    }

    static void Block(Transform parent, string name, Sprite square, Color color, float x, float y, float width, float height,
                      string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        Vector3 parentScale = parent.lossyScale;
        go.transform.localScale = new Vector3(width / square.bounds.size.x / parentScale.x, height / square.bounds.size.y / parentScale.y, 1f);
        go.transform.position = new Vector3(x, y, 0f);
    }

    static void Solid(Transform parent, string name, int layer, float x, float y, float width, float height)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
    }

    static float ParallaxEffect(Transform t)
    {
        var parallax = t.GetComponentInParent<ParallaxController>(true);
        return parallax == null ? 0f : new SerializedObject(parallax).FindProperty("parallaxEffect").floatValue;
    }

    static Location FindLocation(string id)
    {
        Location location = Object.FindObjectsByType<Location>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(l => l.LocationID == id);
        if (location == null) throw new System.Exception($"Location {id} not found.");
        return location;
    }

    static Transform Find(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);
        Transform t = root != null ? root.transform : null;
        for (int i = 1; t != null && i < parts.Length; i++) t = t.Find(parts[i]);
        if (t == null) throw new System.Exception($"Scene object not found: {path}");
        return t;
    }
}
