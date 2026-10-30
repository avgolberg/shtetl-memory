using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Location 1 from STORY_SPEC: the inside of Halyna's house (the great-grandmother's house) as its own location,
// the door from the L2 street, the chest of drawers (Key Choice mini-game) and its three finds.
// The room is the Batch A art (ApplyArt): the painted room, the wardrobe, the chest of drawers (closed / open) and the
// three finds' close-ups. "Shtetl → Apply House Art" places only the art in the existing room, without a rebuild.
// Idempotent: rebuilds the "House Interior" root and the street door; doesn't touch other areas (e.g. the Mikvah).
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod HouseInteriorBuilder.BuildBatch -logFile <log>
public static class HouseInteriorBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string RootName = "House Interior";
    const string LocationId = "4";
    const string StreetDoorName = "Halyna House Door";
    const string ChestName = "Chest of Drawers";

    //One still camera view of the painted room. The painting is drawn closer than the streets (its door is about three
    //players tall at the street zoom), so the camera zooms in here: orthographic size 8.75 instead of 17.5.
    static readonly Vector2 Center = new Vector2(0f, -240f);
    const float CameraSize = 8.75f;
    const float Height = CameraSize * 2f, Width = Height * 16f / 9f;

    //Where the painted door of Halyna's house appears in Snapshots/2_L2_Street/tile_05.png (camera x 54.3)
    const float StreetDoorApparentX = 58.27f, CaptureCameraX = 54.3f;
    const float StreetArrivalY = -85.4f, StreetDoorCenterY = -83f, StreetArrivalOffset = 2.8f;

    //Batch A art. Layout in pixels of the room painting (1672 × 941), measured from its top-left corner.
    const string ArtFolder = "Assets/Sprites/Interiors/Halyna House/";
    const float RoomArtWidth = 1672f, RoomArtHeight = 941f;
    const float FloorPx = 748f; //Where the feet stand: the front edge of the rug
    const float ExitDoorPx = 110f, EntryArrivalPx = 560f; //The open doorway on the left; arrive in front of the chair
    //Feet of each piece: the wardrobe by the door (the small pictures hang behind it), the chest under the big framed
    //picture, so the player walks into the room to reach it
    static readonly Vector2 WardrobePx = new Vector2(336f, 692f);
    static readonly Vector2 ChestPx = new Vector2(1140f, 698f);
    const float ChestHeight = 2.75f, WardrobeHeight = 5.7f;
    //From the sprites' alpha: opaque height and bottom row (closed and open chest share one canvas and frame)
    const float ChestOpaquePx = 779f, ChestBottomPx = 927f, ChestCanvasHeight = 1024f;
    const float WardrobeOpaquePx = 1374f, WardrobeBottomPx = 1458f, WardrobeCanvasHeight = 1536f;
    //The chest's first position: saves know it by name + position (MiniGameItem.fixedSaveId keeps that ID after moving)
    static readonly Vector2 LegacyChestPosition = new Vector2(17f, -248.7f);
    //The unfinished square (amah al amah) is explained later (Semen, STORY_SPEC); here it only raises the question
    const string FamilyPhotoText = "A big Jewish family sits at the table at home. Behind them, one small square of the wall was left unfinished. Why?";
    //Semen's parents' wedding (STORY_SPEC, agreed 2026-10-30); the donation link is revealed later, not here
    const string InvitationImageText = "With joy, we invite you\nto celebrate the marriage\nof Rivka and David.\n\nLadanivka Synagogue\n1937";

    static float ArtPixelsPerUnit => RoomArtWidth / Width;
    static float FloorTop => ArtY(FloorPx);
    static float ArtX(float px) => Center.x - Width / 2f + px / ArtPixelsPerUnit;
    static float ArtY(float py) => Center.y + RoomArtHeight / ArtPixelsPerUnit / 2f - py / ArtPixelsPerUnit;

    //Places the art in the existing room only: no rebuild, so the street door, Halyna, quests and saves stay as they are
    [MenuItem("Shtetl/Apply House Art")]
    public static void ApplyArtMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null)
        {
            Debug.LogError($"'{RootName}' not found. Run Shtetl → Build House Interior first.");
            return;
        }
        ApplyArt(root.transform);
        UITextLayoutFixer.FixDiscoveryPopup();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(LocationCapture.CaptureAll() + "\nHouse art applied. Save the scene (Ctrl+S) to keep it.");
    }

    public static void ApplyArtBatch()
    {
        int exitCode = 0;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyArt(Find(RootName));
            UITextLayoutFixer.FixDiscoveryPopup();
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

        GameObject waypointTemplate = Find("L2/L2->L1 Return Waypoint").gameObject;

        //Room root and boundary
        foreach (var old in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == RootName))
            Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        root.layer = street.gameObject.layer;
        root.transform.position = new Vector3(Center.x, Center.y, 0f);
        var boundary = root.AddComponent<PolygonCollider2D>(); //Sized by ApplyArt
        boundary.isTrigger = true;
        var location = root.AddComponent<Location>();
        var streetSo = new SerializedObject(street);
        var so = new SerializedObject(location);
        so.FindProperty("locationID").stringValue = LocationId;
        so.FindProperty("locationName").stringValue = RootName;
        so.FindProperty("mapBoundary").objectReferenceValue = boundary;
        so.FindProperty("musicClip").objectReferenceValue = streetSo.FindProperty("musicClip").objectReferenceValue;
        so.FindProperty("musicVolume").floatValue = streetSo.FindProperty("musicVolume").floatValue * 0.6f; //quieter inside
        so.ApplyModifiedPropertiesWithoutUndo();

        //Doors: the street door opens when Halyna gives the keys (FindHouse reward); the way out is always open.
        //ApplyArt places the room's door, arrival, floor and walls on the painting.
        Transform entryArrival = Point(root.transform, "House Interior Entry Arrival", Center.x, Center.y);
        MapTransition exit = Door(root.transform, "House Interior Exit", waypointTemplate, Center.x, Center.y, 1f, 1f);

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
        BuildChest(root.transform);
        ApplyArt(root.transform);
        UITextLayoutFixer.FixDiscoveryPopup();
        SetQuestRewards(doorRewardId);
        RegisterJournalTopics("FamilyHouse", "FamilyPhoto", "SchoolPhoto", "WeddingInvitation");
        SetUpHalyna(StreetDoorApparentX); //where the door appears while the player is beside Halyna
        NameBoxKeys();

        Debug.Log($"House interior built. Street door edit x {doorEditX:0.##} (parallax {effect}), arrival x {arrivalX:0.##}, reward id {doorRewardId}.");
    }

    //The art: painted room (camera zoom, boundary, floor, walls, door), wardrobe, chest of drawers and the finds' images.
    //Works on a fresh build and on the existing scene; removes the old PH_ placeholders.
    static void ApplyArt(Transform root)
    {
        Sprite room = ImportSprite("house_interior.png", ArtPixelsPerUnit, new Vector2(0.5f, 0.5f), false);
        Vector2 chestPivot = new Vector2(0.5f, (ChestCanvasHeight - ChestBottomPx) / ChestCanvasHeight);
        Sprite chestClosed = ImportSprite("chest_closed.png", ChestOpaquePx / ChestHeight, chestPivot, true);
        Sprite chestOpen = ImportSprite("chest_open.png", ChestOpaquePx / ChestHeight, chestPivot, true);
        Sprite wardrobe = ImportSprite("wardrobe.png", WardrobeOpaquePx / WardrobeHeight,
            new Vector2(0.5f, (WardrobeCanvasHeight - WardrobeBottomPx) / WardrobeCanvasHeight), true);
        Sprite familyPhoto = ImportSprite("Finds/feiga_family_photo.png", 100f, new Vector2(0.5f, 0.5f), false);
        Sprite schoolPhoto = ImportSprite("Finds/feiga_school_photo.png", 100f, new Vector2(0.5f, 0.5f), false);
        Sprite invitation = ImportSprite("Finds/wedding_invitation.png", 100f, new Vector2(0.5f, 0.5f), false);

        foreach (Transform placeholder in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("PH_")).ToList())
            Object.DestroyImmediate(placeholder.gameObject);

        //One still view: the boundary is exactly what the zoomed camera sees
        float hw = Width / 2f, hh = Height / 2f;
        root.GetComponent<PolygonCollider2D>().points = new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh), new Vector2(-hw, hh) };
        var location = new SerializedObject(root.GetComponent<Location>());
        location.FindProperty("cameraSize").floatValue = CameraSize;
        location.ApplyModifiedPropertiesWithoutUndo();

        Art(root, "Room Background", room, Center.x, Center.y, "Background", 0);
        Art(root, "Wardrobe", wardrobe, ArtX(WardrobePx.x), ArtY(WardrobePx.y), "Ground1", 10);

        //Floor and walls the player can't pass
        int groundLayer = Find("L2/Background/groundLayer0/temp/temp").gameObject.layer;
        float left = Center.x - hw, bottom = Center.y - hh;
        Solid(root, "Floor Collider", groundLayer, Center.x, (bottom + FloorTop) / 2f, Width, FloorTop - bottom);
        Solid(root, "Left Wall", groundLayer, left - 0.5f, Center.y, 1f, Height);
        Solid(root, "Right Wall", groundLayer, left + Width + 0.5f, Center.y, 1f, Height);

        //The painted open doorway is the way out; the player arrives in front of the chair, clear of the chest and door
        Transform exit = root.Find("House Interior Exit");
        exit.position = new Vector3(ArtX(ExitDoorPx), FloorTop + 3f, 0f);
        exit.GetComponent<BoxCollider2D>().size = new Vector2(3.8f, 6f);
        root.Find("House Interior Entry Arrival").position = new Vector3(ArtX(EntryArrivalPx), FloorTop + 2f, 0f);

        //Chest of drawers: one object, closed until the Key Choice is solved, then open (MiniGameItem.updatedSprite,
        //also restored from the save). Its save ID is kept from before the move.
        Transform chest = root.Find(ChestName);
        var item = new SerializedObject(chest.GetComponent<MiniGameItem>());
        var saveId = item.FindProperty("fixedSaveId");
        if (string.IsNullOrEmpty(saveId.stringValue)) saveId.stringValue = GlobalHelper.GenerateUniqueID(chest.gameObject);
        item.FindProperty("updatedSprite").objectReferenceValue = chestOpen;
        item.ApplyModifiedPropertiesWithoutUndo();
        chest.position = new Vector3(ArtX(ChestPx.x), ArtY(ChestPx.y), 0f);
        var chestRenderer = chest.GetComponent<SpriteRenderer>();
        if (chestRenderer == null) chestRenderer = chest.gameObject.AddComponent<SpriteRenderer>();
        chestRenderer.sprite = chestClosed;
        chestRenderer.sortingLayerName = "Ground1";
        chestRenderer.sortingOrder = 10;
        var trigger = chest.GetComponent<BoxCollider2D>();
        trigger.size = new Vector2(4.4f, 3.6f);
        trigger.offset = new Vector2(0f, ChestHeight / 2f + 0.2f);

        //A different close-up on each page of the drawer's contents
        var pages = new SerializedObject(chest.GetComponentInChildren<InspectPoint>(true));
        var list = pages.FindProperty("pages");
        var images = new System.Collections.Generic.Dictionary<string, Sprite>
        {
            { "Old drawer", chestOpen }, { "Family photo", familyPhoto }, { "School photo", schoolPhoto }, { "Invitation", invitation }
        };
        for (int i = 0; i < list.arraySize; i++)
        {
            var page = list.GetArrayElementAtIndex(i);
            string title = page.FindPropertyRelative("title").stringValue;
            if (images.TryGetValue(title, out Sprite image))
                page.FindPropertyRelative("image").objectReferenceValue = image;
            else
                Debug.LogWarning($"Drawer page '{title}' has no Batch A image.");
            if (title == "Family photo") page.FindPropertyRelative("text").stringValue = FamilyPhotoText; //Matches the art
            page.FindPropertyRelative("imageText").stringValue = title == "Invitation" ? InvitationImageText : "";
        }
        pages.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"House art applied: room {Width:0.##} x {Height:0.##} (camera size {CameraSize}), floor y {FloorTop:0.##}, " +
                  $"chest x {chest.position.x:0.##} (save ID '{saveId.stringValue}'), wardrobe x {ArtX(WardrobePx.x):0.##}.");
    }

    //Sprite import: one sprite, no mipmaps, clamped; the furniture's canvases are multiples of 4 and can be compressed
    static Sprite ImportSprite(string file, float pixelsPerUnit, Vector2 pivot, bool compress)
    {
        string path = ArtFolder + file;
        if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.Exception($"Art not found: {path}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spritePixelsPerUnit = pixelsPerUnit;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.mipmapEnabled = false;
        settings.alphaIsTransparency = true;
        settings.filterMode = FilterMode.Bilinear;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.npotScale = TextureImporterNPOTScale.None;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 2048;
        importer.textureCompression = compress ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.Exception($"No sprite imported from {path}");
        return sprite;
    }

    //A sprite at its import size (scale 1); reuses the child if it exists
    static void Art(Transform parent, string name, Sprite sprite, float x, float y, string sortingLayer, int order)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
        }
        t.localScale = Vector3.one;
        t.position = new Vector3(x, y, 0f);
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
    }

    static void BuildChest(Transform root)
    {
        GameObject boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MiniGame Items/Box 1.prefab");

        //Created where it first stood, so ApplyArt keeps the save ID that existing saves use, then moves it
        var chest = new GameObject(ChestName);
        chest.layer = boxPrefab.layer;
        chest.transform.SetParent(root, false);
        chest.transform.position = LegacyChestPosition;
        var trigger = chest.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        var contents = new GameObject("Drawer Contents");
        contents.transform.SetParent(chest.transform, false);
        var inspect = contents.AddComponent<InspectPoint>();

        Quest leadSchool = LoadQuest("LeadSchool"), leadSynagogue = LoadQuest("LeadSynagogue");
        var pages = new SerializedObject(inspect);
        var list = pages.FindProperty("pages");
        list.arraySize = 4;
        //ApplyArt sets each page's image (matched by title).
        //Titles fit one line of the popup title (600 px at size 70); texts stay within its 5-line description box.
        SetPage(list.GetArrayElementAtIndex(0), null, "Old drawer",
            "The drawer is open. Inside are old photos and papers.", "", null, "");
        SetPage(list.GetArrayElementAtIndex(1), null, "Family photo",
            FamilyPhotoText, "family_photo", null, "");
        SetPage(list.GetArrayElementAtIndex(2), null, "School photo",
            "An old class photo: girls and boys with their teacher. Did great-grandma study at this school?",
            "school_photo", leadSchool, "");
        SetPage(list.GetArrayElementAtIndex(3), null, "Invitation",
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

    //A solid box collider; reuses the child if it exists
    static void Solid(Transform parent, string name, int layer, float x, float y, float width, float height)
    {
        Transform t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, height);
        box.offset = Vector2.zero;
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
