using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// M2.2 B1b: the House of Culture interior shell (Location 6), the former synagogue on the Market Square.
// One continuous hall seen in section: the entrance on the west (left) under the balcony, the stage on the east (right).
// Two placeholder panels stand for the two Batch C paintings (they meet at a shared pilaster in the middle).
// Placeholder zones only: no NPCs, interactables, quests or art yet. Size and zoom are provisional until tested.
// Owns: the "House of Culture" root, and the "House of Culture Entrance" group under the Market Square root
// (the façade door and its arrival point). MarketSquareBuilder keeps that group when it rebuilds the square.
// Idempotent: both are replaced on every run. Doesn't call other builders.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod HouseOfCultureBuilder.BuildBatch -logFile <log>
public static class HouseOfCultureBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string RootName = "House of Culture";
    const string LocationId = "6";
    const string SquareRootName = "Market Square";
    const string SquareDoorPath = "Market Square/Placeholders/PH_House of Culture/PH_Door";
    const string EntranceDoorName = "House of Culture Door";
    const string ExitName = "House of Culture Exit";

    //Below the Market Square (y -377..-315). Camera size 10 (house 8.75, streets 17.5): one view is 35.6 × 20,
    //one Batch C painting each; the hall is two views wide and one high, so the camera only scrolls sideways.
    static readonly Vector2 Origin = new Vector2(0f, -420f);
    const float CameraSize = 10f;
    const float Height = CameraSize * 2f, ViewWidth = Height * 16f / 9f, Width = ViewWidth * 2f;
    //The boundary is a little taller than the view: Cinemachine's Confiner2D pins the camera to the shape's midpoint
    //when the view is as tall as the shape (ConfinerOven: frustumHeight >= theoreticalMaxFrustumHeight), which stopped
    //it scrolling. With 0.1 to spare it scrolls sideways; the camera stays at the lowest height (the player is on the
    //floor), and the panels bleed past the edges so the 0.05 below them never shows.
    const float BoundaryHeight = Height + 0.1f, Bleed = 0.3f;
    const float FloorFromTop = 0.8f; //The paintings' floor line, 80% down

    //Layout along the hall, measured from the west wall (0) to the east wall (Width). Provisional.
    //Doorways are about 1.43 × the player (measured 4.54 tall), so about 305 px in a painting.
    const float ExitX = 1.1f, ExitWidth = 2.6f, DoorHeight = 6.5f, ArrivalX = 6f;
    const float BalconyEnd = 17.8f, BalconyFloorFromTop = 0.4f; //Its underside clears the doorways
    const float OksanaFrom = 8.5f, OksanaTo = 12.1f; //Clear of the arrival point, so her talk zone won't overlap it
    const float BookFrom = 13.2f, BookTo = 15.6f;
    const float ArchFrom = 17f, ArchTo = 19.5f;
    const float WorkshopFrom = 23.5f, WorkshopTo = 29.2f;
    static readonly float[] WindowX = { 27.7f, 42f, 50f }; //The second hall window has its lower third bricked up
    const float WindowWidth = 3.2f, WindowBottom = 3f, WindowHeight = 10.5f;
    const float ChairsFrom = 39f, ChairsTo = 55f;
    const float StageFrom = 57f, StageHeight = 1.4f, CurtainHeight = 7f;
    //The plastered arch: centred over the stage, between the curtain and the ceiling (8–32% down the painting)
    const float NicheX = 64f, NicheWidth = 6f, NicheTopFromTop = 0.08f, NicheBottomFromTop = 0.32f;

    //Square side: arrive just right of the façade door
    const float SquareArrivalOffset = 4f;

    static readonly Color WestPanel = new Color(0.83f, 0.78f, 0.68f);
    static readonly Color EastPanel = new Color(0.80f, 0.76f, 0.69f);
    static readonly Color FloorColor = new Color(0.55f, 0.42f, 0.32f);
    static readonly Color ZoneColor = new Color(0.36f, 0.50f, 0.62f, 0.35f);
    static readonly Color WoodColor = new Color(0.55f, 0.40f, 0.28f);
    static readonly Color GlassColor = new Color(0.66f, 0.78f, 0.84f);
    static readonly Color PlasterColor = new Color(0.90f, 0.87f, 0.80f);
    static readonly Color DarkColor = new Color(0.30f, 0.22f, 0.18f);

    static float X(float fromWest) => Origin.x - Width / 2f + fromWest;
    static float FloorY => Origin.y + Height / 2f - FloorFromTop * Height;

    //Editor workflow: run from the menu, then save the scene yourself (Ctrl+S). Captures are refreshed automatically.
    [MenuItem("Shtetl/Build House of Culture")]
    public static void BuildMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(LocationCapture.CaptureAll() + "\nHouse of Culture built. Save the scene (Ctrl+S) to keep it.");
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
        Location square = FindLocation("5");
        GameObject template = Find("L2/L2->L1 Return Waypoint").gameObject;
        int groundLayer = Find("L2/Background/groundLayer0/temp/temp").gameObject.layer;

        //Hall root, boundary (exactly what the zoomed camera can see vertically), floor and end walls
        foreach (var old in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == RootName))
            Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        root.layer = square.gameObject.layer;
        root.transform.position = new Vector3(Origin.x, Origin.y, 0f);
        var boundary = root.AddComponent<PolygonCollider2D>();
        boundary.isTrigger = true;
        float hw = Width / 2f, hh = Height / 2f, bh = BoundaryHeight / 2f;
        boundary.points = new[] { new Vector2(-hw, -bh), new Vector2(hw, -bh), new Vector2(hw, bh), new Vector2(-hw, bh) };

        var location = root.AddComponent<Location>();
        var squareSo = new SerializedObject(square);
        var so = new SerializedObject(location);
        so.FindProperty("locationID").stringValue = LocationId;
        so.FindProperty("locationName").stringValue = RootName;
        so.FindProperty("mapBoundary").objectReferenceValue = boundary;
        so.FindProperty("musicClip").objectReferenceValue = squareSo.FindProperty("musicClip").objectReferenceValue;
        so.FindProperty("musicVolume").floatValue = squareSo.FindProperty("musicVolume").floatValue * 0.6f; //quieter inside
        so.FindProperty("cameraSize").floatValue = CameraSize;
        so.ApplyModifiedPropertiesWithoutUndo();

        float bottom = Origin.y - hh;
        Solid(root.transform, "Floor Collider", groundLayer, Origin.x, (bottom + FloorY) / 2f, Width, FloorY - bottom);
        Solid(root.transform, "West Wall", groundLayer, X(0f) - 0.5f, Origin.y, 1f, Height);
        Solid(root.transform, "East Wall", groundLayer, X(Width) + 0.5f, Origin.y, 1f, Height);

        //Doors: the façade door on the square (placed on the PH_Door by AlignEntrance) and the west exit, both always open
        Transform entrance = EntranceGroup(square.transform);
        MapTransition squareDoorTransition = Door(entrance, EntranceDoorName, template, 0f, 0f, 1f, 1f);
        Transform squareArrival = Point(entrance, EntranceDoorName + " Arrival", 0f, 0f);
        Bounds doorBounds = AlignEntrance(square.transform);

        MapTransition exit = Door(root.transform, ExitName, template, X(ExitX), FloorY + DoorHeight / 2f, ExitWidth, DoorHeight);
        Transform hallArrival = Point(root.transform, "House of Culture Entry Arrival", X(ArrivalX), FloorY + 2f);
        Link(squareDoorTransition, location, hallArrival);
        Link(exit, square, squareArrival);

        BuildPlaceholders(root.transform);
        MeasureScale();

        Debug.Log($"House of Culture built: {Width:0.##} x {Height} at camera size {CameraSize}, root {Origin}, floor y {FloorY:0.##}. " +
                  $"Square door x {doorBounds.center.x:0.##}, square arrival x {squareArrival.position.x:0.##}; hall arrival x {hallArrival.position.x:0.##}.");
    }

    //Puts the façade door's trigger exactly on the square's PH_Door and the arrival point just right of it.
    //Also called by MarketSquareBuilder after it rebuilds the square, so the kept entrance follows the door.
    public static Bounds AlignEntrance(Transform squareRoot)
    {
        Transform group = squareRoot.Find(MarketSquareBuilder.EntranceGroupName);
        Transform door = group != null ? group.Find(EntranceDoorName) : null;
        Transform arrival = group != null ? group.Find(EntranceDoorName + " Arrival") : null;
        Transform placeholder = Find(SquareDoorPath);
        if (door == null || arrival == null) throw new System.Exception("House of Culture entrance not found. Run Shtetl → Build House of Culture.");

        Bounds bounds = placeholder.GetComponent<SpriteRenderer>().bounds;
        door.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
        door.GetComponent<BoxCollider2D>().size = new Vector2(bounds.size.x, bounds.size.y);
        arrival.position = new Vector3(bounds.center.x + SquareArrivalOffset, bounds.min.y + 1f, 0f);
        return bounds;
    }

    //The façade door's group under the Market Square root, replaced on every run (MarketSquareBuilder keeps it)
    static Transform EntranceGroup(Transform squareRoot)
    {
        Transform old = squareRoot.Find(MarketSquareBuilder.EntranceGroupName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var group = new GameObject(MarketSquareBuilder.EntranceGroupName).transform;
        group.SetParent(squareRoot, false);
        return group;
    }

    //PH_ panels and zones where the Batch C paintings and sprites will go; labels name each zone
    static void BuildPlaceholders(Transform root)
    {
        Sprite square = Find("L3/Background/Fog/Square (2)").GetComponent<SpriteRenderer>().sprite;
        var group = new GameObject("Placeholders").transform;
        group.SetParent(root, false);
        float top = Origin.y + Height / 2f, bottom = Origin.y - Height / 2f;

        //The two paintings, meeting at a shared pilaster in the middle; they bleed past the boundary's thin margin
        Block(group, "PH_Painting 1 (west)", square, X(ViewWidth / 2f), bottom - Bleed, ViewWidth, Height + 2f * Bleed, WestPanel, "Background", 0);
        Block(group, "PH_Painting 2 (east)", square, X(ViewWidth * 1.5f), bottom - Bleed, ViewWidth, Height + 2f * Bleed, EastPanel, "Background", 0);
        Block(group, "PH_Floor", square, Origin.x, bottom - Bleed, Width, FloorY - bottom + Bleed, FloorColor, "Background", 1);
        Block(group, "PH_Pilaster (seam)", square, X(ViewWidth), FloorY, 1.4f, top - FloorY, PlasterColor, "Background", 2);

        //West: exit, balcony over the foyer, Oksana's desk, the visitors' book, the inner arch
        Block(group, "PH_Exit Doorway", square, X(ExitX), FloorY, ExitWidth, DoorHeight, DarkColor, "Background", 2);
        float balconyY = top - BalconyFloorFromTop * Height;
        Block(group, "PH_Balcony", square, X(BalconyEnd / 2f), balconyY - 0.8f, BalconyEnd, 0.8f, WoodColor, "Background", 3);
        Block(group, "PH_Balcony Railing", square, X(BalconyEnd / 2f), balconyY, BalconyEnd, 1.3f, ZoneColor, "Background", 3);
        Zone(group, "PH_Oksana Zone", square, OksanaFrom, OksanaTo, 4.3f, "Oksana + desk");
        Zone(group, "PH_Visitors' Book Zone", square, BookFrom, BookTo, 2.8f, "visitors'\nbook");
        Block(group, "PH_Inner Arch West Jamb", square, X(ArchFrom + 0.4f), FloorY, 0.8f, balconyY - 0.8f - FloorY, PlasterColor, "Background", 3);
        Block(group, "PH_Inner Arch East Jamb", square, X(ArchTo - 0.4f), FloorY, 0.8f, balconyY - 0.8f - FloorY, PlasterColor, "Background", 3);
        Label(group, "balcony (former women's gallery?)", X(BalconyEnd / 2f), balconyY + 2.2f, BalconyEnd);
        Label(group, "inner arch\n(groove)", X((ArchFrom + ArchTo) / 2f), FloorY + 7f, 5f);

        //Hall: tall windows (the first lights the workshop nook), chairs, stage, plastered arch above it
        for (int i = 0; i < WindowX.Length; i++)
        {
            Block(group, "PH_Tall Window", square, X(WindowX[i]), FloorY + WindowBottom, WindowWidth, WindowHeight, GlassColor, "Background", 3);
            if (i == 1)
                Block(group, "PH_Bricked Part", square, X(WindowX[i]), FloorY + WindowBottom, WindowWidth, WindowHeight / 3f, PlasterColor, "Background", 4);
        }
        Label(group, "tall windows", X((WindowX[1] + WindowX[2]) / 2f), FloorY + WindowBottom + WindowHeight + 1.2f, 12f);
        Zone(group, "PH_Workshop Nook", square, WorkshopFrom, WorkshopTo, 2.4f, "workshop\ntable");
        Block(group, "PH_Chairs", square, X((ChairsFrom + ChairsTo) / 2f), FloorY, ChairsTo - ChairsFrom, 1.2f, WoodColor, "Background", 3);
        Block(group, "PH_Stage", square, X((StageFrom + Width) / 2f), FloorY, Width - StageFrom, StageHeight, WoodColor, "Background", 3);
        Block(group, "PH_Curtain", square, X((StageFrom + 1f + Width) / 2f), FloorY + StageHeight, Width - StageFrom - 1f, CurtainHeight, new Color(0.45f, 0.22f, 0.22f), "Background", 2);
        float nicheTop = top - NicheTopFromTop * Height, nicheBottom = top - NicheBottomFromTop * Height;
        Block(group, "PH_Plastered Arch", square, X(NicheX), nicheBottom, NicheWidth, nicheTop - nicheBottom, PlasterColor, "Background", 3);
        Label(group, "stage", X((StageFrom + Width) / 2f), FloorY + StageHeight + 3.5f, 10f);
        Label(group, "plastered arch", X(NicheX), (nicheTop + nicheBottom) / 2f, NicheWidth);
        Label(group, "PH_ Painting 1 (west)", X(ViewWidth / 2f), top - 1.2f, ViewWidth);
        Label(group, "PH_ Painting 2 (east)", X(ViewWidth * 1.5f), top - 1.2f, ViewWidth);
    }

    //Logs the player's real size, so the Batch C paintings can be drawn at the right scale
    static void MeasureScale()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogWarning("House of Culture: no Player to measure."); return; }

        Physics2D.SyncTransforms();
        var body = player.GetComponent<Collider2D>();
        var renderers = player.GetComponentsInChildren<SpriteRenderer>()
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sprite != null).ToList();
        if (renderers.Count == 0) { Debug.LogWarning("House of Culture: the player has no visible sprite to measure."); return; }

        Bounds visible = renderers[0].bounds;
        foreach (var r in renderers) visible.Encapsulate(r.bounds);
        const float PaintingWidth = 1672f, PaintingHeight = 941f;
        float pixelsPerUnit = PaintingWidth / ViewWidth;
        Debug.Log($"House of Culture scale: player sprite {visible.size.y:0.##} units tall ({visible.size.x:0.##} wide), " +
                  $"collider {(body != null ? body.bounds.size.y : 0f):0.##} tall. In a {PaintingWidth}×{PaintingHeight} painting " +
                  $"at camera size {CameraSize}: {pixelsPerUnit:0.#} px per unit, player ≈ {visible.size.y * pixelsPerUnit:0} px, " +
                  $"floor line at y {FloorFromTop * PaintingHeight:0} px, placeholder doorway {DoorHeight} units ≈ {DoorHeight * pixelsPerUnit:0} px " +
                  $"({DoorHeight / visible.size.y:0.##} × the player).");
    }

    //A floor-standing zone where a sprite or NPC will go, with its label above
    static void Zone(Transform parent, string name, Sprite square, float from, float to, float height, string label)
    {
        Block(parent, name, square, X((from + to) / 2f), FloorY, to - from, height, ZoneColor, "Background", 5);
        Label(parent, label, X((from + to) / 2f), FloorY + height + 1.1f, to - from + 2f);
    }

    //A tinted square sprite with its bottom edge at y
    static void Block(Transform parent, string name, Sprite square, float x, float bottom, float width, float height,
                      Color color, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(width / square.bounds.size.x, height / square.bounds.size.y, 1f);
        go.transform.position = new Vector3(x, bottom + height / 2f, 0f);
    }

    //World text naming a placeholder (development only, removed with the placeholders)
    static void Label(Transform parent, string text, float x, float y, float width)
    {
        var go = new GameObject("PH_Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = 7f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.15f, 0.12f);
        tmp.rectTransform.sizeDelta = new Vector2(width, 3f);
        tmp.sortingLayerID = SortingLayer.NameToID("Background");
        tmp.sortingOrder = 10;
        go.transform.position = new Vector3(x, y, 0f);
    }

    //Invisible door trigger with a MapTransition; its SpriteRenderer stays empty (the door is painted)
    static MapTransition Door(Transform parent, string name, GameObject template, float x, float y, float width, float height)
    {
        var go = new GameObject(name);
        go.layer = template.layer;
        go.tag = template.tag;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, height);
        go.AddComponent<SpriteRenderer>();
        var transition = go.AddComponent<MapTransition>();
        transition.Name = name;
        return transition;
    }

    static void Link(MapTransition from, Location target, Transform arrival)
    {
        var so = new SerializedObject(from);
        so.FindProperty("targetLocation").objectReferenceValue = target;
        so.FindProperty("teleportTargetPosition").objectReferenceValue = arrival;
        so.FindProperty("isReturnTransition").boolValue = true; //always open (unlocked on Awake)
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform Point(Transform parent, string name, float x, float y)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = new Vector3(x, y, 0f);
        return t;
    }

    //A solid box collider
    static void Solid(Transform parent, string name, int layer, float x, float y, float width, float height)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
    }

    static Location FindLocation(string id)
    {
        Location location = Object.FindObjectsByType<Location>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(l => l.LocationID == id);
        if (location == null) throw new System.Exception($"Location {id} not found. Build the Market Square first.");
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
