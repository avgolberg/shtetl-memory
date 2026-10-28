using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the Mikvah (natural pond) area on the left edge of L3, before the cemetery graves.
// Idempotent: rebuilds L3/Parallax/Mikvah and sets fixed values on the existing water, fence and Fisherman.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod MikvahAreaBuilder.BuildBatch -logFile <log>
public static class MikvahAreaBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string Foliage = "Assets/Sprites/Background/foliage-pack/PNG/Retina/foliagePack_0{0}.png";

    //L3/Parallax has parallaxEffect 0.1; with the camera at its left limit (x -160.3) objects appear 16 units further left
    const float ParallaxShift = -16f;
    const float GroundY = -160.6f;
    const float WaterRightEdge = -168.5f;

    static readonly Color WaterColor = new Color(0.20f, 0.40f, 0.46f);
    static readonly Color FoliageTint = new Color(0.71f, 0.74f, 0.65f); //matches the existing L3 trees

    [MenuItem("Shtetl/Build Mikvah Area")]
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
            if (summary.Contains("FAILED")) exitCode = 2;
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
        Transform parallax = Find("L3/Parallax");

        Transform old = parallax.Find("Mikvah");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        Transform area = new GameObject("Mikvah").transform;
        area.SetParent(parallax, false);
        area.position = new Vector3(0f, 0f, parallax.position.z);

        //Existing water strip: lighter, ending at the bank instead of a hard edge in the open
        Transform waterTransform = parallax.Find("Mikvah Water") ?? Find("L3/Parallax/temp (1)");
        SpriteRenderer water = waterTransform.GetComponent<SpriteRenderer>();
        water.gameObject.name = "Mikvah Water";
        water.color = WaterColor;
        water.sortingOrder = 4;
        float waterWidth = water.sprite.bounds.size.x * Mathf.Abs(water.transform.lossyScale.x);
        SetRuntimeX(water.transform, WaterRightEdge - waterWidth / 2f);
        //Original height: water surface level with the ground, so no ground ledge shows above it
        water.transform.position = new Vector3(water.transform.position.x, -167.7f, water.transform.position.z);

        //Small leftover water patch beside the old edge is no longer needed
        Transform patch = parallax.Find("temp (2)");
        if (patch != null) patch.gameObject.SetActive(false);

        //Sloped earth bank replacing the vertical water edge, using the scene's front ground sprite and tint
        SpriteRenderer ground = Find("L3/Background/groundLayer0/groundLayer0 (1)").GetComponent<SpriteRenderer>();
        BankSlope(area, ground, new Vector2(WaterRightEdge - 0.2f, GroundY + 0.1f), 36f, 18f);

        //Stone steps: same stone, evenly spaced down the bank slope into the water (natural mikvah)
        for (int i = 0; i < 3; i++)
            Place(area, $"Step Stone {i + 1}", "54", WaterRightEdge - 1.9f - i * 1.9f, GroundY - 1.5f - i * 1.4f, 1.3f - i * 0.1f, 6, flip: i == 1);

        //Reeds in the water, clear of the steps
        Place(area, "Reeds Edge A", "20", -177.5f, GroundY - 1.3f, 3.0f, 7);
        Place(area, "Reeds Edge B", "21", -180.5f, GroundY - 1.4f, 3.4f, 7, flip: true);
        Place(area, "Bank Rock", "57", WaterRightEdge + 0.1f, GroundY - 0.6f, 1.4f, 6); //rounds off the top of the bank

        //Far shore reeds, so the pond reads as a calm natural pool rather than an endless river
        Place(area, "Reeds Far A", "19", -187.5f, GroundY - 0.6f, 2.4f, 7);
        Place(area, "Reeds Far B", "21", -185.0f, GroundY - 0.7f, 3.0f, 7);
        Place(area, "Rock Far", "56", -189.5f, GroundY - 0.8f, 1.8f, 6);

        //Light ripples on the surface
        Sprite square = Find("L3/Background/Fog/Square (2)").GetComponent<SpriteRenderer>().sprite;
        Ripple(area, square, -183.5f, 2.6f, GroundY - 1.9f);
        Ripple(area, square, -175.0f, 1.6f, GroundY - 5.6f);
        Ripple(area, square, -182.0f, 1.4f, GroundY - 4.0f);

        //Low bush behind the Fisherman gives the clearing a back edge
        Place(area, "Bush Back", "52", WaterRightEdge + 6.0f, GroundY + 0.1f, 1.4f, 2);

        //Existing grass tufts and stone from the old shoreline (L3/Parallax/Graves) moved onto the bank, spaced out
        string[] tufts = { "grass (13)", "grass (14)", "grass (16)", "grass (10)", "grass (11)", "grass (12)", "grass (15)" };
        float[] tuftX = { WaterRightEdge + 7.3f, WaterRightEdge + 7.9f, WaterRightEdge + 8.8f, WaterRightEdge + 13.9f, WaterRightEdge + 14.5f, WaterRightEdge + 16.3f, WaterRightEdge + 16.9f };
        for (int i = 0; i < tufts.Length; i++)
            SetRuntimeX(Find("L3/Parallax/Graves/" + tufts[i]), tuftX[i]);
        SetRuntimeX(Find("L3/Parallax/Graves/stone (4)"), WaterRightEdge + 12.0f);

        //Fence moved off the bank; Fisherman stands on the bank looking over the water
        SetRuntimeX(Find("L3/Parallax/fence_0 (6)"), WaterRightEdge + 10.5f);
        SetRuntimeX(Find("L3/Parallax/fence_0 (7)"), WaterRightEdge + 15.3f);

        NPC fisherman = Object.FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(n => n.dialogueStages != null && n.dialogueStages.Any(d => d != null && d.name == "NollanNPCDialogue"));
        if (fisherman == null) throw new System.Exception("Fisherman NPC (NollanNPCDialogue) not found.");
        Transform holder = fisherman.transform;
        while (holder.parent != parallax) holder = holder.parent;
        SetRuntimeX(holder, WaterRightEdge + 3.4f);
        holder.localScale = Vector3.one;

        //Facing is on the character art only: negative x (the original scene value) faces left, toward the water
        Transform body = fisherman.transform.Find("NPC Body");
        Vector3 bodyScale = body.localScale;
        body.localScale = new Vector3(-Mathf.Abs(bodyScale.x), bodyScale.y, bodyScale.z);

        Debug.Log("Mikvah area built.");
    }

    static void SetRuntimeX(Transform t, float runtimeX)
    {
        t.position = new Vector3(runtimeX - ParallaxShift, t.position.y, t.position.z);
    }

    static void Place(Transform parent, string name, string foliageId, float runtimeX, float bottomY, float height, int order, bool flip = false)
    {
        string path = string.Format(Foliage, foliageId);
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new System.Exception($"Sprite not found: {path}");

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = FoliageTint;
        sr.sortingLayerName = "Ground1";
        sr.sortingOrder = order;
        sr.flipX = flip;

        float scale = height / sprite.bounds.size.y;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        float y = bottomY - sprite.bounds.min.y * scale;
        go.transform.position = new Vector3(runtimeX - ParallaxShift, y, parent.position.z);
    }

    //Narrow copy of a ground strip, rotated so its top edge runs from bankTop down-left into the water
    static void BankSlope(Transform parent, SpriteRenderer ground, Vector2 bankTop, float angle, float length)
    {
        var go = new GameObject("Bank Slope");
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ground.sprite;
        sr.color = ground.color;
        sr.sortingLayerName = "Ground1";
        sr.sortingOrder = 5;

        Vector3 groundScale = ground.transform.lossyScale;
        var scale = new Vector3(length / ground.sprite.bounds.size.x * Mathf.Sign(groundScale.x), groundScale.y, 1f);
        go.transform.localScale = scale;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.rotation = rotation;

        //Anchor the strip's top-right corner at the bank top
        Bounds b = ground.sprite.bounds;
        Vector3 topRight = rotation * new Vector3(b.max.x * Mathf.Abs(scale.x), b.max.y * scale.y, 0f);
        go.transform.position = new Vector3(bankTop.x - ParallaxShift - topRight.x, bankTop.y - topRight.y, parent.position.z);
    }

    static void Ripple(Transform parent, Sprite square, float runtimeX, float width, float y)
    {
        var go = new GameObject("Ripple");
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.75f, 0.88f, 0.9f, 0.45f);
        sr.sortingLayerName = "Ground1";
        sr.sortingOrder = 6;
        go.transform.localScale = new Vector3(width / square.bounds.size.x, 0.12f / square.bounds.size.y, 1f);
        go.transform.position = new Vector3(runtimeX - ParallaxShift, y, parent.position.z);
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
