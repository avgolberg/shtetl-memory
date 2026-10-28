using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Read-only visual capture of every Location in the open scene.
// Per location: overview.png (editor layout), tile_XX.png at player camera scale (parallax simulated),
// plus a short placement report in summary.txt. Output: <project>/Snapshots/ (outside Assets, gitignored).
// Batch mode (Unity closed): Unity.exe -batchmode -projectPath <project> -executeMethod LocationCapture.CaptureAllBatch -logFile <log>
public static class LocationCapture
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const float FallbackOrthoSize = 17.5f;
    const int TileWidth = 960, TileHeight = 540;
    const int OverviewLongSide = 2400;
    const int MaxTilesPerLocation = 24;
    const int MaxListedIssues = 12;

    static string OutputRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Snapshots"));

    [MenuItem("Shtetl/Capture Locations")]
    public static void CaptureAllMenu()
    {
        string summary = CaptureAll();
        Debug.Log(summary);
        EditorUtility.RevealInFinder(Path.Combine(OutputRoot, "summary.txt"));
    }

    public static void CaptureAllBatch()
    {
        int exitCode = 0;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string summary = CaptureAll();
            Debug.Log(summary);
            if (summary.Contains("FAILED")) exitCode = 2;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }

    public static string CaptureAll()
    {
        Directory.CreateDirectory(OutputRoot);
        Scene scene = SceneManager.GetActiveScene();
        bool wasDirty = scene.isDirty;
        var log = new StringBuilder($"Location capture of '{scene.path}' at {System.DateTime.Now:yyyy-MM-dd HH:mm}\n");

        Location[] locations = Object.FindObjectsByType<Location>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(l => l.LocationID).ToArray();
        if (locations.Length == 0)
        {
            log.AppendLine("FAILED: no Location components in the open scene.");
        }
        else
        {
            Camera cam = CreateCaptureCamera(out float orthoSize, out float cameraZ);
            captureHalfWidth = orthoSize * TileWidth / TileHeight;
            log.AppendLine($"Player camera ortho size {orthoSize} (view {orthoSize * 2f * TileWidth / TileHeight:0.#} x {orthoSize * 2f:0.#} units)");
            try
            {
                foreach (Location location in locations)
                    log.AppendLine(CaptureLocation(location, cam, orthoSize, cameraZ));
            }
            finally
            {
                Object.DestroyImmediate(cam.gameObject);
            }
        }

        if (!wasDirty && scene.isDirty)
            log.AppendLine("WARNING: scene was marked dirty during capture. Nothing was changed on purpose; do not save it.");

        File.WriteAllText(Path.Combine(OutputRoot, "summary.txt"), log.ToString());
        return log.ToString();
    }

    static Camera CreateCaptureCamera(out float orthoSize, out float cameraZ)
    {
        Camera main = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        GameObject go = EditorUtility.CreateGameObjectWithHideFlags("LocationCaptureCamera", HideFlags.HideAndDontSave, typeof(Camera));
        Camera cam = go.GetComponent<Camera>();
        if (main != null) cam.CopyFrom(main);

        cam.enabled = false;
        cam.orthographic = true;
        cam.targetTexture = null;
        orthoSize = main != null && main.orthographic ? main.orthographicSize : FallbackOrthoSize;
        cameraZ = main != null ? main.transform.position.z : -10f;
        return cam;
    }

    static string CaptureLocation(Location location, Camera cam, float orthoSize, float cameraZ)
    {
        string label = Sanitize($"{location.LocationID}_{(string.IsNullOrEmpty(location.LocationName) ? location.name : location.LocationName)}");
        if (location.MapBoundary == null)
            return $"[{label}] FAILED: Location has no mapBoundary; nothing captured.";

        Rect area = BoundaryRect(location.MapBoundary);
        if (area.width < 0.01f || area.height < 0.01f)
            return $"[{label}] FAILED: mapBoundary has no area.";

        string dir = Path.Combine(OutputRoot, label);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder($"[{label}] boundary x {area.xMin:0.#}..{area.xMax:0.#}, y {area.yMin:0.#}..{area.yMax:0.#} ({area.width:0.#} x {area.height:0.#})\n");

        //Overview: whole boundary, editor layout
        float aspect = area.width / area.height;
        int ow = aspect >= 1f ? OverviewLongSide : Mathf.Max(64, Mathf.RoundToInt(OverviewLongSide * aspect));
        int oh = aspect >= 1f ? Mathf.Max(64, Mathf.RoundToInt(OverviewLongSide / aspect)) : OverviewLongSide;
        sb.AppendLine("  overview.png " + SaveCapture(cam, area.center, area.height / 2f, cameraZ, ow, oh, Path.Combine(dir, "overview.png")));

        //Detail tiles at player camera scale, as the confined camera would see them
        float viewH = orthoSize * 2f, viewW = viewH * TileWidth / TileHeight;
        List<float> xs = TileCenters(area.xMin, area.xMax, viewW);
        List<float> ys = new List<float> { WalkLevelCameraY(location, area, orthoSize) };
        if (xs.Count * ys.Count > MaxTilesPerLocation)
            sb.AppendLine($"  WARNING: {xs.Count * ys.Count} tiles needed, capped at {MaxTilesPerLocation}.");

        var parallax = ParallaxState.Collect();
        try
        {
            int index = 0;
            foreach (float y in ys)
            {
                foreach (float x in xs)
                {
                    if (++index > MaxTilesPerLocation) break;
                    parallax.ApplyForCamera(x);
                    string file = $"tile_{index:00}.png";
                    sb.AppendLine($"  {file} center ({x:0.#}, {y:0.#}) " + SaveCapture(cam, new Vector2(x, y), orthoSize, cameraZ, TileWidth, TileHeight, Path.Combine(dir, file)));
                }
            }
        }
        finally
        {
            parallax.Restore();
        }

        AppendPlacementReport(sb, location, area);
        return sb.ToString();
    }

    //Camera height the player would see: walking level (median NPC height, else transitions) in the lower third of the view
    static float WalkLevelCameraY(Location location, Rect area, float orthoSize)
    {
        List<float> feet = location.GetComponentsInChildren<NPC>(true).Select(n => n.transform.position.y).OrderBy(y => y).ToList();
        if (feet.Count == 0)
            feet = location.GetComponentsInChildren<MapTransition>(true).Select(m => m.transform.position.y).OrderBy(y => y).ToList();
        float walkY = feet.Count > 0 ? feet[feet.Count / 2] : area.center.y - orthoSize * 0.4f;
        float y = walkY + orthoSize * 0.4f;
        return area.height <= orthoSize * 2f ? area.center.y : Mathf.Clamp(y, area.yMin + orthoSize, area.yMax - orthoSize);
    }

    static List<float> TileCenters(float min, float max, float view)
    {
        float size = max - min;
        if (size <= view) return new List<float> { (min + max) / 2f };

        int count = Mathf.CeilToInt(size / view);
        var centers = new List<float>();
        for (int i = 0; i < count; i++)
            centers.Add(Mathf.Lerp(min + view / 2f, max - view / 2f, i / (float)(count - 1)));
        return centers;
    }

    static string SaveCapture(Camera cam, Vector2 center, float orthoSize, float cameraZ, int width, int height, string path)
    {
        cam.transform.position = new Vector3(center.x, center.y, cameraZ);
        cam.orthographicSize = orthoSize;
        cam.aspect = (float)width / height;

        var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
        {
            sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
        };
        RenderTexture rt = RenderTexture.GetTemporary(descriptor);
        Texture2D tex = null;
        try
        {
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            return LooksEmpty(tex) ? "WARNING: image is (almost) a single color - check camera, layers or lighting" : "ok";
        }
        catch (System.Exception e)
        {
            return $"FAILED: {e.Message}";
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            if (tex != null) Object.DestroyImmediate(tex);
        }
    }

    static bool LooksEmpty(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        Color32 first = pixels[0];
        int same = 0;
        for (int i = 0; i < pixels.Length; i += 7)
        {
            Color32 p = pixels[i];
            if (Mathf.Abs(p.r - first.r) + Mathf.Abs(p.g - first.g) + Mathf.Abs(p.b - first.b) < 12) same++;
        }
        return same > pixels.Length / 7 * 0.98f;
    }

    static Rect BoundaryRect(PolygonCollider2D boundary)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        for (int p = 0; p < boundary.pathCount; p++)
        {
            foreach (Vector2 point in boundary.GetPath(p))
            {
                Vector2 world = boundary.transform.TransformPoint(point + boundary.offset);
                min = Vector2.Min(min, world);
                max = Vector2.Max(max, world);
            }
        }
        return min.x > max.x ? Rect.zero : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    //Short diagnostics: layer usage, broken or misplaced renderers, transitions, NPCs, foreign objects
    static void AppendPlacementReport(StringBuilder sb, Location location, Rect area)
    {
        SpriteRenderer[] own = location.GetComponentsInChildren<SpriteRenderer>(true);
        var layers = own.GroupBy(r => r.sortingLayerName).OrderBy(g => SortingLayer.GetLayerValueFromName(g.Key))
            .Select(g => $"{g.Key}:{g.Count()}");
        sb.AppendLine($"  renderers {own.Length} by sorting layer: {string.Join(" ", layers)}");

        var issues = new List<string>();
        foreach (SpriteRenderer r in own)
        {
            if (r.sprite == null && r.enabled && r.gameObject.activeInHierarchy) issues.Add($"no sprite: {PathOf(r.transform)}");
            else if (r.sprite != null && !area.Overlaps(ToRect(r.bounds)) && r.GetComponent<ParallaxController>() == null) issues.Add($"outside boundary: {PathOf(r.transform)}");
        }
        foreach (Transform t in location.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("temp")) issues.Add($"placeholder: {PathOf(t)}");
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) issues.Add($"missing script: {PathOf(t)}");
        }
        AppendList(sb, "issues", issues);

        var transitions = location.GetComponentsInChildren<MapTransition>(true).Select(DescribeTransition).ToList();
        AppendList(sb, "transitions", transitions);

        var npcs = location.GetComponentsInChildren<NPC>(true).Select(n => $"{n.name} ({n.npcId}) @x{n.transform.position.x:0}").ToList();
        AppendList(sb, "npcs", npcs);

        //Renderers from other roots that are visible inside this boundary (leftovers, shared objects)
        var foreign = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(r => r.sprite != null && !r.transform.IsChildOf(location.transform) && r.GetComponentInParent<Location>() == null
                        && r.GetComponentInParent<ParallaxController>() == null && area.Overlaps(ToRect(r.bounds)))
            .GroupBy(r => r.transform.root.name).Select(g => $"{g.Key} x{g.Count()}").ToList();
        AppendList(sb, "other renderers inside boundary", foreign);
    }

    //Target, arrival point inside the target boundary, and arrival clear of any transition trigger there
    static string DescribeTransition(MapTransition m)
    {
        var so = new SerializedObject(m);
        var target = so.FindProperty("targetLocation").objectReferenceValue as Location;
        var arrival = so.FindProperty("teleportTargetPosition").objectReferenceValue as Transform;
        string open = so.FindProperty("isReturnTransition").boolValue ? "open" : "locked until Portal reward";
        string head = $"{m.name} @({m.transform.position.x:0.#}, {m.transform.position.y:0.#}) [{open}]";
        if (target == null || arrival == null) return $"{head} TRANSITION PROBLEM: missing target or arrival point";

        var problems = new List<string>();
        Vector2 p = arrival.position;
        Rect area = target.MapBoundary != null ? BoundaryRect(target.MapBoundary) : Rect.zero;
        if (!area.Contains(p)) problems.Add("arrival outside target boundary");

        //Triggers of parallaxed waypoints shift with the camera; check them where the camera is after arriving
        float cameraX = Mathf.Clamp(p.x, area.xMin + captureHalfWidth, area.xMax - captureHalfWidth);
        foreach (MapTransition other in target.GetComponentsInChildren<MapTransition>(true))
        {
            var box = other.GetComponent<BoxCollider2D>();
            if (box == null) continue;
            float shift = cameraX * ParallaxEffectOf(other.transform);
            Vector2 min = other.transform.TransformPoint(box.offset - box.size / 2f), max = other.transform.TransformPoint(box.offset + box.size / 2f);
            if (Rect.MinMaxRect(Mathf.Min(min.x, max.x) + shift, Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x) + shift, Mathf.Max(min.y, max.y)).Contains(p))
                problems.Add($"arrival inside trigger of {other.name}");
        }
        if (ParallaxEffectOf(arrival) != 0f) problems.Add("arrival point moves with parallax (depends on camera at departure)");

        string result = problems.Count == 0 ? "ok" : "note: " + string.Join(", ", problems);
        if (problems.Any(x => x.StartsWith("arrival inside") || x.StartsWith("arrival outside"))) result = "TRANSITION PROBLEM: " + string.Join(", ", problems);
        return $"{head} -> {target.LocationID} {target.LocationName}, arrives ({p.x:0.#}, {p.y:0.#}) {result}";
    }

    static float captureHalfWidth = FallbackOrthoSize * TileWidth / TileHeight;

    static float ParallaxEffectOf(Transform t)
    {
        float effect = 0f;
        for (; t != null; t = t.parent)
        {
            var p = t.GetComponent<ParallaxController>();
            if (p != null) effect += new SerializedObject(p).FindProperty("parallaxEffect").floatValue;
        }
        return effect;
    }

    static void AppendList(StringBuilder sb, string title, List<string> items)
    {
        if (items.Count == 0) return;
        sb.AppendLine($"  {title} ({items.Count}):");
        foreach (string item in items.Take(MaxListedIssues)) sb.AppendLine("    " + item);
        if (items.Count > MaxListedIssues) sb.AppendLine($"    ... {items.Count - MaxListedIssues} more");
    }

    static Rect ToRect(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    static string Sanitize(string s) => string.Concat(s.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '_' : c));

    //Reproduces ParallaxController's runtime offset for a given camera x, and restores the original positions
    class ParallaxState
    {
        readonly List<(Transform t, Vector3 original, float effect, bool looping, float length)> layers = new();

        public static ParallaxState Collect()
        {
            var state = new ParallaxState();
            foreach (ParallaxController p in Object.FindObjectsByType<ParallaxController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(p);
                float effect = so.FindProperty("parallaxEffect").floatValue;
                bool looping = so.FindProperty("enableLooping").boolValue;
                SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
                state.layers.Add((p.transform, p.transform.position, effect, looping && sr != null, sr != null ? sr.bounds.size.x : 0f));
            }
            return state;
        }

        public void ApplyForCamera(float cameraX)
        {
            foreach (var layer in layers)
            {
                float start = layer.original.x;
                if (layer.looping && layer.length > 0f)
                {
                    float movement = cameraX * (1f - layer.effect);
                    while (movement > start + layer.length) start += layer.length;
                    while (movement < start - layer.length) start -= layer.length;
                }
                layer.t.position = new Vector3(start + cameraX * layer.effect, layer.original.y, layer.original.z);
            }
        }

        public void Restore()
        {
            foreach (var layer in layers) layer.t.position = layer.original;
        }
    }
}
