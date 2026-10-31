using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// M2.2 B1a: the Market Square shell (Location 5), reached from the left end of the L2 street, next to Maria.
// It holds the House of Culture (former synagogue) and the space reserved for the market (M3.3) and for a later
// School / Library connection. Placeholders only (PH_), no interior, NPCs or quests yet.
// Sky, clouds and the far layer are copies of L2's, so the town looks the same. Unlike L2, the walking ground and
// everything on it stand still (no parallax), so edit positions are exactly where things appear.
// Touches only its own objects: the "Market Square" root and the L2 waypoint pair below. Doesn't call other builders.
// Idempotent: the root, the L2 waypoint and its arrival point are replaced on every run.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod MarketSquareBuilder.BuildBatch -logFile <log>
public static class MarketSquareBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string RootName = "Market Square";
    const string LocationId = "5";
    const string WaypointTemplatePath = "L2/L2->L1 Return Waypoint";
    const string StreetWaypointName = "L2->Market Square Waypoint";
    const string SquareWaypointName = "Market Square->L2 Waypoint";

    //Below the house interior (y -249..-231); same frame as L2 (boundary 15 above to 47 below the root)
    static readonly Vector2 Origin = new Vector2(0f, -330f);
    const float BoundaryTop = 15f, BoundaryBottom = -47f;
    //About 2.3 camera views (62.2 wide): 18 s to walk across at the player's 8 units/s, L2 takes 46 s
    const float HalfWidth = 72f;
    const float WallOffset = 67f, WallWidth = 5f, WallHeight = 65f, WallY = -15f; //As L2's end walls
    const float WaypointY = -12.9f, ArrivalY = -19f; //Relative to the root, as on L2

    //L2's left end (its left wall's inner face is at x -183.86): the pine appears there, as the L2->L3 pine does at the right end
    const float StreetWaypointShownX = -183.5f, StreetArrivalX = -178f;
    //The square's right edge (its right wall's inner face is at x 64.5): you walk left out of L2 and come in from the right
    const float SquareWaypointX = 64.3f, SquareArrivalX = 58.8f;

    //Layout, world x. Right to left: arrival, House of Culture, plaza, market, reserved exit.
    //Provisional proportions (the final art gives it a distinctive historic character): about 1.4 L2 houses tall, so the
    //roof stays inside the game camera (which sits about 3.5 above the feet); one row of tall windows = the double-height hall
    const float CultureX = 32f, CultureWidth = 30f, CultureHeight = 18f;
    static readonly float[] CultureWindowX = { -12f, -7f, 7f, 12f };
    const float WindowWidth = 2.6f, WindowHeight = 10f, WindowBottom = 3.5f;
    const float DoorWidth = 3.2f, DoorHeight = 5.5f, SignWidth = 8f;
    const float WellX = 0f, BoardX = 7f;
    const float MarketLeft = -54f, MarketRight = -8f;
    static readonly float[] StallX = { -46f, -34f, -22f };
    const float StallWidth = 8f, StallHeight = 6f;
    const float ReservedExitX = -59f;

    static readonly Color PlaceholderWall = new Color(0.86f, 0.82f, 0.70f);
    static readonly Color PlaceholderRoof = new Color(0.45f, 0.36f, 0.30f);
    static readonly Color PlaceholderDark = new Color(0.36f, 0.26f, 0.20f);
    static readonly Color PlaceholderGlass = new Color(0.62f, 0.74f, 0.78f);
    static readonly Color PlaceholderWood = new Color(0.62f, 0.45f, 0.30f);

    //Editor workflow: run from the menu, then save the scene yourself (Ctrl+S). Captures are refreshed automatically.
    [MenuItem("Shtetl/Build Market Square")]
    public static void BuildMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(LocationCapture.CaptureAll() + "\nMarket Square built. Save the scene (Ctrl+S) to keep it.");
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
        GameObject template = Find(WaypointTemplatePath).gameObject;

        //Root, boundary, end walls
        foreach (var old in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == RootName))
            Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        root.layer = street.gameObject.layer;
        root.transform.position = new Vector3(Origin.x, Origin.y, 0f);
        var boundary = root.AddComponent<PolygonCollider2D>();
        boundary.isTrigger = true;
        boundary.points = new[]
        {
            new Vector2(HalfWidth, BoundaryTop), new Vector2(-HalfWidth, BoundaryTop),
            new Vector2(-HalfWidth, BoundaryBottom), new Vector2(HalfWidth, BoundaryBottom)
        };
        foreach (float side in new[] { -1f, 1f })
        {
            var wall = root.AddComponent<BoxCollider2D>();
            wall.offset = new Vector2(side * WallOffset, WallY);
            wall.size = new Vector2(WallWidth, WallHeight);
        }

        var location = root.AddComponent<Location>();
        var streetSo = new SerializedObject(street);
        var so = new SerializedObject(location);
        so.FindProperty("locationID").stringValue = LocationId;
        so.FindProperty("locationName").stringValue = RootName;
        so.FindProperty("mapBoundary").objectReferenceValue = boundary;
        so.FindProperty("musicClip").objectReferenceValue = streetSo.FindProperty("musicClip").objectReferenceValue;
        so.FindProperty("musicVolume").floatValue = streetSo.FindProperty("musicVolume").floatValue;
        so.FindProperty("cameraSize").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform background = BuildBackground(root.transform, street);
        Physics2D.SyncTransforms();

        //L2 <-> Market Square: always open, like the L2 <-> L3 pair
        Transform streetArrival = StreetWaypointPair(street, template, out MapTransition streetWaypoint);
        MapTransition squareWaypoint = Waypoint(root.transform, template, SquareWaypointName, SquareWaypointX, false);
        Transform squareArrival = Point(root.transform, SquareWaypointName + " Arrival", SquareArrivalX, Origin.y + ArrivalY);
        Link(streetWaypoint, location, squareArrival);
        Link(squareWaypoint, street, streetArrival);

        BuildPlaceholders(root.transform, background);

        Debug.Log($"Market Square built: x {-HalfWidth}..{HalfWidth}, root y {Origin.y}. " +
                  $"L2 waypoint edit x {streetWaypoint.transform.position.x:0.##} (appears at {StreetWaypointShownX}), arrival {StreetArrivalX}; " +
                  $"square waypoint {SquareWaypointX}, arrival {SquareArrivalX}.");
    }

    //A copy of L2's background: the sky, clouds and far layer keep their parallax; the two walking-ground layers
    //(groundLayer0 has the ground colliders) stand still, so the ground under the buildings never shifts
    static Transform BuildBackground(Transform root, Location street)
    {
        Transform source = street.transform.Find("Background");
        if (source == null) throw new System.Exception("L2/Background not found.");
        GameObject copy = Object.Instantiate(source.gameObject, root);
        copy.name = "Background";
        copy.transform.localPosition = source.localPosition;

        foreach (string still in new[] { "groundLayer0", "groundLayer1" })
        {
            Transform layer = copy.transform.Find(still);
            if (layer == null) throw new System.Exception($"L2/Background/{still} not found.");
            var parallax = layer.GetComponent<ParallaxController>();
            if (parallax == null) continue;
            var p = new SerializedObject(parallax);
            p.FindProperty("parallaxEffect").floatValue = 0f;
            p.FindProperty("enableLooping").boolValue = false; //The strip is 448 wide, the square 144
            p.ApplyModifiedPropertiesWithoutUndo();

            //The extra pieces at x ±128 and ±192 stand still beyond the square's edges, so they can never be seen
            Transform outer = layer.Find("temp");
            if (outer != null) Object.DestroyImmediate(outer.gameObject);
        }
        return copy.transform;
    }

    //L2 end: the pine moves with the street (parallax 0.1), so its edit x is chosen to appear at the wall
    //when the camera is at the street's left limit (where it is whenever the player is at that end)
    static Transform StreetWaypointPair(Location street, GameObject template, out MapTransition waypoint)
    {
        foreach (string old in new[] { StreetWaypointName, StreetWaypointName + " Arrival" })
        {
            Transform existing = street.transform.Find(old);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        float effect = 0f;
        var parallax = template.GetComponent<ParallaxController>();
        if (parallax != null) effect = new SerializedObject(parallax).FindProperty("parallaxEffect").floatValue;
        float editX = StreetWaypointShownX - ArrivalCameraX(street, StreetArrivalX) * effect;

        waypoint = Waypoint(street.transform, template, StreetWaypointName, editX, true);
        SitOnGround(waypoint, street.transform.Find("Background"));
        return Point(street.transform, StreetWaypointName + " Arrival", StreetArrivalX, street.transform.position.y + ArrivalY);
    }

    //In front of the ground band the pine's trunk floated above it (behind the band it was hidden). Lowers the sprite
    //onto the ground and raises the trigger by the same amount, so the trigger stays where it was in the world.
    //L2's ground and the pine share parallax 0.1, so edit positions line up. The ground loops: its pieces have one shape
    //and repeat every piece width, and the outer pieces (x ±128, ±192) are inactive in the scene, so the height is
    //sampled at the same point within the active centre piece (in play, a looped piece stands under the pine).
    static void SitOnGround(MapTransition waypoint, Transform streetBackground)
    {
        const float Sink = 0.3f; //Trunks stand a little into the ground, like the street's trees
        Transform t = waypoint.transform;
        var sr = t.GetComponent<SpriteRenderer>();
        float bottom = t.TransformPoint(new Vector3(0f, sr.sprite.vertices.Min(v => v.y), 0f)).y;

        Transform groundPiece = streetBackground.Find("groundLayer0");
        float pieceWidth = groundPiece.GetComponent<SpriteRenderer>().bounds.size.x;
        float sampleX = t.position.x + Mathf.Round((groundPiece.position.x - t.position.x) / pieceWidth) * pieceWidth;

        Physics2D.SyncTransforms();
        float top = streetBackground.parent.position.y + BoundaryTop;
        RaycastHit2D ground = Physics2D.RaycastAll(new Vector2(sampleX, top), Vector2.down, BoundaryTop - BoundaryBottom)
            .FirstOrDefault(h => !h.collider.isTrigger && h.collider.transform.IsChildOf(streetBackground));
        if (ground.collider == null)
        {
            Debug.LogWarning($"{waypoint.name}: no L2 ground found under x {sampleX:0.##} (the pine's x {t.position.x:0.##}); left as it was.");
            return;
        }

        float drop = bottom - (ground.point.y - Sink);
        t.position += Vector3.down * drop;
        var box = t.GetComponent<BoxCollider2D>();
        box.offset += new Vector2(0f, drop / t.lossyScale.y);
        Debug.Log($"{waypoint.name}: sprite lowered {drop:0.##} onto the ground at y {ground.point.y:0.##}; trigger unchanged.");
    }

    //A copy of the development pine (the final transition art is still to be designed), muted like L2's background pines.
    //On L2 it stands in front of the street's trees (Ground1 orders 1-2): behind them, the cypress at the left end hid it.
    static MapTransition Waypoint(Transform parent, GameObject template, string name, float x, bool onStreet)
    {
        GameObject go = Object.Instantiate(template, parent);
        go.name = name;
        go.transform.position = new Vector3(x, parent.position.y + WaypointY, template.transform.position.z);

        Transform clonedPoint = go.transform.Find("TeleportPoint");
        if (clonedPoint != null) Object.DestroyImmediate(clonedPoint.gameObject);
        if (!onStreet)
        {
            var parallax = go.GetComponent<ParallaxController>();
            if (parallax != null) Object.DestroyImmediate(parallax);
        }

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerName = onStreet ? "Ground1" : "Ground2";
        sr.sortingOrder = onStreet ? 3 : 10;
        sr.color = new Color(0.70f, 0.80f, 0.78f);

        var transition = go.GetComponent<MapTransition>();
        transition.Name = name;
        return transition;
    }

    //PH_ blocks that show the proportions; the art (Batch C) replaces them
    static void BuildPlaceholders(Transform root, Transform background)
    {
        Sprite square = Find("L3/Background/Fog/Square (2)").GetComponent<SpriteRenderer>().sprite;
        var group = new GameObject("Placeholders").transform;
        group.SetParent(root, false);

        //House of Culture: wall, roof band, two rows of tall windows, door, the mezuzah recess on the right doorpost, the sign
        float baseY = GroundY(background, CultureX - CultureWidth / 2f, CultureX + CultureWidth / 2f);
        Transform culture = Block(group, "PH_House of Culture", square, CultureX, baseY, CultureWidth, CultureHeight, PlaceholderWall, 1);
        Block(culture, "PH_Roof", square, CultureX, baseY + CultureHeight - 0.5f, CultureWidth + 1.5f, 2.5f, PlaceholderRoof, 2);
        foreach (float dx in CultureWindowX)
            Block(culture, "PH_Window", square, CultureX + dx, baseY + WindowBottom, WindowWidth, WindowHeight, PlaceholderGlass, 2);
        Block(culture, "PH_Door", square, CultureX, baseY, DoorWidth, DoorHeight, PlaceholderDark, 2);
        Block(culture, "PH_Mezuzah Recess", square, CultureX + DoorWidth / 2f + 0.45f, baseY + 3.8f, 0.35f, 0.9f, PlaceholderDark, 3);
        Block(culture, "PH_Sign", square, CultureX, baseY + DoorHeight + 1.6f, SignWidth, 1.8f, PlaceholderWood, 3);
        Label(group, "PH_ House of Culture\n(former synagogue)", CultureX, baseY + WindowBottom + WindowHeight + 1.8f, CultureWidth);
        Label(group, "sign: Budynok kultury", CultureX, baseY + DoorHeight + 2.5f, SignWidth, 1.3f);

        //Plaza: the well and the notice board (later: the workshop poster)
        Block(group, "PH_Well", square, WellX, GroundY(background, WellX - 1.5f, WellX + 1.5f), 3f, 2.6f, PlaceholderWood, 2);
        Block(group, "PH_Notice Board", square, BoardX, GroundY(background, BoardX - 1.2f, BoardX + 1.2f), 2.4f, 4.2f, PlaceholderWood, 2);
        Label(group, "plaza", (WellX + BoardX) / 2f, GroundY(background, WellX, BoardX) + 6.5f, 14f);

        //Market (M3.3): three stall frames
        float marketY = GroundY(background, MarketLeft, MarketRight);
        foreach (float x in StallX)
        {
            float y = GroundY(background, x - StallWidth / 2f, x + StallWidth / 2f);
            Transform stall = Block(group, "PH_Market Stall", square, x, y, StallWidth, StallHeight, PlaceholderWood, 2);
            Block(stall, "PH_Awning", square, x, y + StallHeight - 0.6f, StallWidth + 0.8f, 1.2f, PlaceholderRoof, 3);
        }
        Label(group, "PH_ Market (M3.3)", (MarketLeft + MarketRight) / 2f, marketY + StallHeight + 3f, MarketRight - MarketLeft);

        //Reserved for a later School / Library connection: a post only, no transition
        float exitY = GroundY(background, ReservedExitX - 0.5f, ReservedExitX + 0.5f);
        Block(group, "PH_Reserved Exit Post", square, ReservedExitX, exitY, 0.6f, 5f, PlaceholderWood, 2);
        Label(group, "reserved exit\n(School / Library, later)", ReservedExitX, exitY + 7.5f, 12f);
    }

    //A tinted square sprite with its bottom edge at y
    static Transform Block(Transform parent, string name, Sprite square, float x, float bottom, float width, float height, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingLayerName = "Ground1"; //With the L2 houses and fences (orders 1-2), behind the NPCs and the player
        sr.sortingOrder = order;
        Vector3 parentScale = parent.lossyScale;
        go.transform.localScale = new Vector3(width / square.bounds.size.x / parentScale.x, height / square.bounds.size.y / parentScale.y, 1f);
        go.transform.position = new Vector3(x, bottom + height / 2f, 0f);
        return go.transform;
    }

    //World text naming a placeholder (development only, removed with the placeholders)
    static void Label(Transform parent, string text, float x, float y, float width, float size = 2.4f)
    {
        var go = new GameObject("PH_Label");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size * 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.15f, 0.12f);
        tmp.rectTransform.sizeDelta = new Vector2(width, 6f);
        tmp.sortingLayerID = SortingLayer.NameToID("Ground1");
        tmp.sortingOrder = 5;
        go.transform.position = new Vector3(x, y, 0f);
    }

    //The lowest ground top between two x values (the ground is gently wavy; a building never floats)
    static float GroundY(Transform background, float left, float right)
    {
        float lowest = float.MaxValue;
        for (int i = 0; i <= 8; i++)
        {
            float x = Mathf.Lerp(left, right, i / 8f);
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(new Vector2(x, Origin.y + BoundaryTop), Vector2.down, BoundaryTop - BoundaryBottom))
            {
                if (hit.collider.isTrigger || !hit.collider.transform.IsChildOf(background)) continue;
                lowest = Mathf.Min(lowest, hit.point.y);
                break;
            }
        }
        if (lowest == float.MaxValue)
        {
            Debug.LogWarning($"Market Square: no ground found between x {left} and {right}; using the arrival height.");
            return Origin.y + ArrivalY - 0.6f;
        }
        return lowest;
    }

    //Camera x when the player stands at x: the confiner keeps the view inside the location boundary.
    //The street uses the game's default zoom (17.5); the editor camera may still hold another location's zoom.
    static float ArrivalCameraX(Location location, float x)
    {
        float halfWidth = 17.5f * 16f / 9f;
        PolygonCollider2D poly = location.MapBoundary;
        float min = poly.points.Min(p => poly.transform.TransformPoint(p + poly.offset).x);
        float max = poly.points.Max(p => poly.transform.TransformPoint(p + poly.offset).x);
        return Mathf.Clamp(x, min + halfWidth, max - halfWidth);
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
