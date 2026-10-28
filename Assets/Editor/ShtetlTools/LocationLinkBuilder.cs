using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates always-open MapTransition pairs between locations by cloning the existing "L2->L1 Return Waypoint".
// Waypoints keep the template's ParallaxController (0.1, like the scenery), so their edit x is chosen to appear at the
// intended spot when the camera is where the player arrives. Arrival points are separate, non-parallaxed objects
// ("<waypoint> Arrival") so the teleport target does not depend on the camera position at departure.
// Idempotent: existing waypoints and arrival points with the same names are replaced.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod LocationLinkBuilder.BuildBatch -logFile <log>
public static class LocationLinkBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string TemplatePath = "L2/L2->L1 Return Waypoint";

    [MenuItem("Shtetl/Build Location Links")]
    public static void BuildMenu()
    {
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
        //L2 street right end <-> L3 road side of the Mikvah (Fisherman just left of the arrival, cemetery to the right).
        //Heights follow the L1<->L2 pair; L3's walking level is 72.6 below L2's.
        //Development stage: always open. Quest-based unlocking comes with the final story order.
        //Waypoint x values are where the pine should appear; arrival points are plain world positions.
        CreatePair("2", "L2->L3 Waypoint", new Vector2(185f, -79.3f), new Vector2(179.5f, -85.4f),
                   "3", "L3->L2 Return Waypoint", new Vector2(-146f, -151.9f), new Vector2(-160f, -158f));
        Debug.Log("Location links built.");
    }

    static void CreatePair(string idA, string nameA, Vector2 waypointA, Vector2 arrivalA,
                           string idB, string nameB, Vector2 waypointB, Vector2 arrivalB)
    {
        Location a = FindLocation(idA), b = FindLocation(idB);
        (MapTransition ta, Transform arriveA) = CreateWaypoint(a, nameA, waypointA, arrivalA);
        (MapTransition tb, Transform arriveB) = CreateWaypoint(b, nameB, waypointB, arrivalB);
        Link(ta, b, arriveB);
        Link(tb, a, arriveA);
    }

    static (MapTransition, Transform) CreateWaypoint(Location location, string name, Vector2 shownAt, Vector2 arrival)
    {
        foreach (string old in new[] { name, name + " Arrival" })
        {
            Transform existing = location.transform.Find(old);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        GameObject template = FindPath(TemplatePath);
        GameObject go = Object.Instantiate(template, location.transform);
        go.name = name;

        //Compensate the waypoint's parallax for the camera position right after arriving here
        float effect = 0f;
        var parallax = go.GetComponent<ParallaxController>();
        if (parallax != null) effect = new SerializedObject(parallax).FindProperty("parallaxEffect").floatValue;
        float editX = shownAt.x - ArrivalCameraX(location, arrival.x) * effect;
        go.transform.position = new Vector3(editX, shownAt.y, template.transform.position.z);

        Transform clonedPoint = go.transform.Find("TeleportPoint");
        if (clonedPoint != null) Object.DestroyImmediate(clonedPoint.gameObject);

        var arrive = new GameObject(name + " Arrival").transform;
        arrive.SetParent(location.transform, false);
        arrive.position = new Vector3(arrival.x, arrival.y, 0f);

        return (go.GetComponent<MapTransition>(), arrive);
    }

    //Camera x when the player stands at x: the confiner keeps the view inside the location boundary
    static float ArrivalCameraX(Location location, float x)
    {
        Camera main = Camera.main;
        float halfWidth = (main != null ? main.orthographicSize : 17.5f) * 16f / 9f;
        Bounds b = location.MapBoundary.bounds.size.x > 0f ? location.MapBoundary.bounds : PointsBounds(location.MapBoundary);
        return Mathf.Clamp(x, b.min.x + halfWidth, b.max.x - halfWidth);
    }

    static Bounds PointsBounds(PolygonCollider2D poly)
    {
        var bounds = new Bounds(poly.transform.TransformPoint(poly.points[0] + poly.offset), Vector3.zero);
        foreach (Vector2 p in poly.points) bounds.Encapsulate(poly.transform.TransformPoint(p + poly.offset));
        return bounds;
    }

    //Transition 'from' leads to 'target', arriving at the given arrival point
    static void Link(MapTransition from, Location target, Transform arrival)
    {
        var so = new SerializedObject(from);
        so.FindProperty("targetLocation").objectReferenceValue = target;
        so.FindProperty("teleportTargetPosition").objectReferenceValue = arrival;
        so.FindProperty("isReturnTransition").boolValue = true; //always open (unlocked on Awake)
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Location FindLocation(string id)
    {
        Location location = Object.FindObjectsByType<Location>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(l => l.LocationID == id);
        if (location == null) throw new System.Exception($"Location {id} not found.");
        return location;
    }

    static GameObject FindPath(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);
        Transform t = root != null ? root.transform : null;
        for (int i = 1; t != null && i < parts.Length; i++) t = t.Find(parts[i]);
        if (t == null) throw new System.Exception($"Scene object not found: {path}");
        return t.gameObject;
    }
}
