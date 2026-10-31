using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// M2.2 block B0: the existing Vytynanka mini-game (UI/Vytynanka in MainScene) as a finished workshop.
// Keeps the owner's tracing work (19 lines, 327 dots, the template and the paper) and adds what was missing:
// - the workshop screen: a dimmed backdrop, a centred board (the game's brown UI panel), a side column with the title,
//   instruction, progress, paper / backing paper swatches and the action button, the work surface on the right;
// - the baked papercut silhouette (PapercutSilhouetteBaker) and the backing paper choice;
// - the MiniGameRegistry entry (ID 5), the "My vytynanka" item (appended to ItemDictionary), the "vytynanka" journal topic,
//   the development shortcut (F9 open, Shift+F9 reset).
// Idempotent: re-running updates the same objects (the backing paper swatches are rebuilt). Doesn't touch any location or NPC.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod VytynankaSetup.SetUpBatch -logFile <log>
public static class VytynankaSetup
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const int MiniGameId = 5;
    const string ItemPrefabPath = "Assets/Prefabs/Items/Papercut.prefab";
    const string BaseItemPrefabPath = "Assets/Prefabs/Items/Item.prefab";
    const string JournalTopicPath = "Assets/Scripts/Scriptable Objects/Journal/Vytynanka.asset";
    const string UiPack = "Assets/Sprites/UI Assets/ui-pack-adventure/PNG/";
    static readonly Color TextColor = new Color(0.25f, 0.18f, 0.12f, 1f); //The cut-line brown
    static readonly Color ButtonTextColor = new Color(0.98f, 0.94f, 0.86f, 1f);

    //Board layout (canvas units; the canvas scales by height, 1080)
    static readonly Vector2 BoardSize = new Vector2(1400f, 1040f);
    const float SideX = -480f, SideWidth = 380f;
    static readonly Vector2 WorkPosition = new Vector2(220f, 0f), WorkSize = new Vector2(940f, 1010f);

    [MenuItem("Shtetl/Set Up Vytynanka")]
    public static void SetUpMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        SetUp();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Vytynanka set up. Save the scene (Ctrl+S) to keep it. Test in Play mode: F9 opens it, Shift+F9 resets it.");
    }

    public static void SetUpBatch()
    {
        int exitCode = 0;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetUp();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }

    static void SetUp()
    {
        var game = Object.FindFirstObjectByType<VytynankaMiniGame>(FindObjectsInactive.Include);
        if (game == null) throw new System.Exception("VytynankaMiniGame (UI/Vytynanka) not found in the scene.");
        Transform canvas = game.transform.Find("Canvas");
        Transform workArea = Deep(canvas, "WorkArea");
        var ghost = Deep(workArea, "TemplateGhostImage").GetComponent<Image>();

        var (silhouette, iconMask) = PapercutSilhouetteBaker.Bake(ghost, Deep(workArea, "Pathes"));

        GameObject item = BuildItemPrefab(silhouette, iconMask);
        RegisterItem(item);
        RegisterJournalTopic(BuildJournalTopic());
        RegisterMiniGame(game);
        AddDevShortcut();

        var ui = BuildWorkshop(game, canvas, workArea);

        var so = new SerializedObject(game);
        so.FindProperty("startDirectlyInTracing").boolValue = false;
        so.FindProperty("hintText").objectReferenceValue = ui.hint;
        so.FindProperty("progressText").objectReferenceValue = ui.progress;
        so.FindProperty("mountSelectPanel").objectReferenceValue = ui.mountPanel;
        so.FindProperty("templateGhostImage").objectReferenceValue = ghost;
        so.FindProperty("finishedSprite").objectReferenceValue = silhouette;
        so.FindProperty("papercutItem").objectReferenceValue = item;
        so.FindProperty("lineThickness").floatValue = 4f;
        //Sequential cutting (tested on the real lines, see VytynankaMiniGame.Follow)
        so.FindProperty("cutTolerance").floatValue = 28f;
        so.FindProperty("startTolerance").floatValue = 45f;
        so.FindProperty("maxAdvance").floatValue = 80f;
        so.FindProperty("kinkAllowance").floatValue = 8f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Workshop screen ----------

    static (TMP_Text hint, TMP_Text progress, GameObject mountPanel) BuildWorkshop(VytynankaMiniGame game, Transform canvas, Transform workArea)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.matchWidthOrHeight = 1f; //Scale by height: the board always fits, wide screens get more side space
        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.sortingOrder = 25; //Over the game's HUD while open; above the screen fader (20), so they never tie
        EditorUtility.SetDirty(scaler);
        EditorUtility.SetDirty(canvasComponent);

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var backdrop = FindOrCreate(canvas, "Backdrop");
        Stretch(backdrop);
        var backdropImage = Graphic<Image>(backdrop);
        backdropImage.color = new Color(0f, 0f, 0f, 0.65f);
        backdropImage.raycastTarget = true; //Clicks don't reach the game behind
        backdrop.SetSiblingIndex(0);

        var board = FindOrCreate(canvas, "Board");
        Place(board, Vector2.zero, BoardSize);
        var boardImage = Graphic<Image>(board);
        boardImage.sprite = Load(UiPack + "Double/panel_brown_damaged.png");
        boardImage.type = Image.Type.Sliced;
        boardImage.color = Color.white;
        board.SetSiblingIndex(1);

        //Work surface on the right; its pattern, paper, cut lines keep their own layout inside it
        workArea.SetParent(board, false);
        Place((RectTransform)workArea, WorkPosition, WorkSize);
        var workImage = workArea.GetComponent<Image>();
        workImage.sprite = uiSprite;
        workImage.type = Image.Type.Sliced;
        workImage.color = new Color(0.95f, 0.92f, 0.86f, 1f);
        EditorUtility.SetDirty(workImage);

        var progress = Deep(canvas, "ProgressText").GetComponent<TMP_Text>();
        var hintTransform = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "HintText");
        var hint = hintTransform != null ? hintTransform.GetComponent<TMP_Text>() : Object.Instantiate(progress, board);
        hint.name = "HintText";
        var title = Text(board, "Title", hint, new Vector2(SideX, 420f), new Vector2(SideWidth, 70f), 56, "Vytynanka");
        Text(board, "Subtitle", hint, new Vector2(SideX, 365f), new Vector2(SideWidth, 40f), 28, "Ukrainian papercut");
        hint.transform.SetParent(board, false);
        Style(hint, new Vector2(SideX, 220f), new Vector2(SideWidth, 220f), 34, "");
        hint.enableAutoSizing = true;
        hint.fontSizeMin = 24;
        hint.fontSizeMax = 34;
        progress.transform.SetParent(board, false);
        Style(progress, new Vector2(SideX, 80f), new Vector2(SideWidth, 60f), 44, "");
        Text(board, "CloseHint", hint, new Vector2(SideX, -470f), new Vector2(SideWidth, 40f), 24, "Esc: close").alpha = 0.7f;
        title.fontStyle = FontStyles.Bold;

        //Paper colours (the owner's four buttons) and the backing paper, in the same place
        var paperPanel = (RectTransform)Deep(canvas, "ColorSelectPanel");
        paperPanel.SetParent(board, false);
        SwatchRow(paperPanel, uiSprite);
        foreach (var paper in PapercutPalette.Papers)
        {
            string buttonName = char.ToUpper(paper.id[0]) + paper.id.Substring(1) + "Button";
            var button = paperPanel.Find(buttonName).GetComponent<Button>();
            Swatch(button, uiSprite, paper.color, char.ToUpper(paper.id[0]) + paper.id.Substring(1));
            WireString(button, game.SelectPaper, paper.id);
        }

        var oldMount = board.Find("MountSelectPanel");
        if (oldMount != null) Object.DestroyImmediate(oldMount.gameObject);
        var mountPanel = (RectTransform)Object.Instantiate(paperPanel.gameObject, board).transform;
        mountPanel.name = "MountSelectPanel";
        var template = mountPanel.GetChild(0).gameObject;
        for (int i = mountPanel.childCount - 1; i >= 1; i--) Object.DestroyImmediate(mountPanel.GetChild(i).gameObject);
        foreach (var mount in PapercutPalette.Mounts)
        {
            var button = Object.Instantiate(template, mountPanel).GetComponent<Button>();
            button.name = mount.id + "Button";
            Swatch(button, uiSprite, mount.color, "");
            WireString(button, game.SelectMount, mount.id);
            Unavailable(button, mount.color);
        }
        Object.DestroyImmediate(template);
        SwatchRow(mountPanel, uiSprite);
        SelectionFrame(mountPanel, uiSprite);
        mountPanel.gameObject.SetActive(false);

        var reveal = Deep(canvas, "RevealButton").GetComponent<Button>();
        reveal.transform.SetParent(board, false);
        Place((RectTransform)reveal.transform, new Vector2(SideX, -200f), new Vector2(300f, 84f));
        var revealImage = reveal.GetComponent<Image>();
        revealImage.sprite = Load(UiPack + "Default/button_brown.png");
        revealImage.type = Image.Type.Sliced;
        revealImage.color = Color.white;
        Label(reveal, "Unfold", 36, TextColor); //Dark brown: the light button needs dark text
        Wire(reveal, game.OnRevealButton);
        reveal.gameObject.SetActive(false);

        var close = Deep(canvas, "CloseButton").GetComponent<Button>();
        close.transform.SetParent(board, false);
        Place((RectTransform)close.transform, new Vector2(BoardSize.x / 2f - 45f, BoardSize.y / 2f - 45f), new Vector2(64f, 64f));
        var closeImage = close.GetComponent<Image>();
        closeImage.sprite = Load(UiPack + "Default/button_brown_close.png");
        closeImage.type = Image.Type.Simple;
        closeImage.preserveAspect = true;
        closeImage.color = Color.white;
        Label(close, "", 30, ButtonTextColor);
        Wire(close, game.Close);

        //The scissors are drawn over everything on the board
        var scissors = Deep(canvas, "ScissorsCursorImage");
        scissors.SetParent(board, false);
        scissors.SetAsLastSibling();

        foreach (var g in new Object[] { backdropImage, boardImage, hint, progress, paperPanel, mountPanel, reveal, close, scissors, workArea })
            EditorUtility.SetDirty(g);
        return (hint, progress, mountPanel.gameObject);
    }

    static void SwatchRow(RectTransform row, Sprite uiSprite)
    {
        Place(row, new Vector2(SideX, -50f), new Vector2(SideWidth, 86f));
        var background = row.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(1f, 1f, 1f, 0f);
            background.raycastTarget = false; //Invisible: clicks between the swatches go nowhere
            EditorUtility.SetDirty(background);
        }
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(4, 4, 6, 6);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        EditorUtility.SetDirty(layout);
    }

    static void Swatch(Button button, Sprite uiSprite, Color color, string label)
    {
        var image = button.GetComponent<Image>();
        image.sprite = uiSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        var outline = Graphic<Outline>(button.transform);
        outline.effectColor = new Color(0.25f, 0.18f, 0.12f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        Label(button, label, 22, luminance > 0.55f ? TextColor : ButtonTextColor);

        //Disabled keeps its own colour (no grey tint): the mini-game fades it and shows its diagonal stroke
        var colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        EditorUtility.SetDirty(image);
    }

    //One clean diagonal stroke across a backing swatch, shown when that backing is too close to the paper colour
    static void Unavailable(Button button, Color swatchColor)
    {
        var existing = button.transform.Find("Unavailable");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var stroke = new GameObject("Unavailable", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        stroke.rectTransform.SetParent(button.transform, false);
        stroke.rectTransform.anchorMin = stroke.rectTransform.anchorMax = stroke.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        stroke.rectTransform.sizeDelta = new Vector2(84f, 3f);
        stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 48f);
        float luminance = 0.299f * swatchColor.r + 0.587f * swatchColor.g + 0.114f * swatchColor.b;
        stroke.color = luminance > 0.55f ? TextColor : ButtonTextColor;
        stroke.raycastTarget = false;
        stroke.gameObject.SetActive(false);
    }

    //The frame around the chosen backing: a light rounded plate just behind the swatch, outside the row's layout
    static void SelectionFrame(RectTransform row, Sprite uiSprite)
    {
        var existing = row.Find("SelectionFrame");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var frame = new GameObject("SelectionFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
        frame.rectTransform.SetParent(row, false);
        frame.rectTransform.SetAsFirstSibling(); //Drawn behind the swatches
        frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0f, 1f); //Like the layout's own children
        frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        frame.GetComponent<LayoutElement>().ignoreLayout = true;
        frame.sprite = uiSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(1f, 0.84f, 0.3f, 1f);
        frame.raycastTarget = false;
        frame.gameObject.SetActive(false);
    }

    // ---------- Item, journal, registry ----------

    //"My vytynanka": a variant of the base inventory item, like Photo. Its picture is built from the icon mask (PapercutItem)
    static GameObject BuildItemPrefab(Sprite silhouette, Texture2D iconMask)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath) != null;
        GameObject instance = exists
            ? PrefabUtility.LoadPrefabContents(ItemPrefabPath)
            : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BaseItemPrefabPath));
        instance.name = "Papercut";

        var itemSo = new SerializedObject(instance.GetComponent<Item>());
        itemSo.FindProperty("Name").stringValue = "My vytynanka";
        itemSo.FindProperty("itemType").enumValueIndex = (int)ItemType.Custom;
        itemSo.ApplyModifiedPropertiesWithoutUndo();

        var image = instance.GetComponent<Image>();
        image.sprite = silhouette;
        image.preserveAspect = true;
        var spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sprite = silhouette;

        var papercut = Graphic<PapercutItem>(instance.transform);
        var papercutSo = new SerializedObject(papercut);
        papercutSo.FindProperty("iconMask").objectReferenceValue = iconMask;
        papercutSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, ItemPrefabPath);
        if (exists)
            PrefabUtility.UnloadPrefabContents(instance);
        else
            Object.DestroyImmediate(instance);
        return prefab;
    }

    //Appended, never reordered: ItemDictionary numbers items by list position and saves store those numbers
    static void RegisterItem(GameObject prefab)
    {
        var dictionary = Object.FindFirstObjectByType<ItemDictionary>(FindObjectsInactive.Include);
        var item = prefab.GetComponent<Item>();
        if (!dictionary.itemPrefabs.Contains(item))
        {
            dictionary.itemPrefabs.Add(item);
            EditorUtility.SetDirty(dictionary);
        }
        Debug.Log($"My vytynanka has item ID {dictionary.itemPrefabs.IndexOf(item) + 1}.");
    }

    static JournalTopicData BuildJournalTopic()
    {
        var topic = AssetDatabase.LoadAssetAtPath<JournalTopicData>(JournalTopicPath);
        if (topic == null)
        {
            topic = ScriptableObject.CreateInstance<JournalTopicData>();
            AssetDatabase.CreateAsset(topic, JournalTopicPath);
        }

        topic.topicId = VytynankaMiniGame.JournalTopicId;
        topic.stages = new System.Collections.Generic.List<JournalStageData>
        {
            new JournalStageData
            {
                title = "Vytynanka",
                lines = new System.Collections.Generic.List<string>
                {
                    "● I made a vytynanka, a Ukrainian papercut.",
                    "● You fold the paper and cut one half.",
                    "● Unfolded, it shows a tree of life with birds.",
                }
            }
        };
        EditorUtility.SetDirty(topic);
        AssetDatabase.SaveAssets();
        return topic;
    }

    static void RegisterJournalTopic(JournalTopicData topic)
    {
        var journal = Object.FindFirstObjectByType<JournalController>(FindObjectsInactive.Include);
        var so = new SerializedObject(journal);
        var topics = so.FindProperty("allTopics");
        bool present = Enumerable.Range(0, topics.arraySize).Any(i => topics.GetArrayElementAtIndex(i).objectReferenceValue == topic);
        if (present) return;
        topics.arraySize++;
        topics.GetArrayElementAtIndex(topics.arraySize - 1).objectReferenceValue = topic;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void RegisterMiniGame(VytynankaMiniGame game)
    {
        var registry = Object.FindFirstObjectByType<MiniGameRegistry>(FindObjectsInactive.Include);
        var so = new SerializedObject(registry);
        var list = so.FindProperty("miniGames");

        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i);
            var behaviour = entry.FindPropertyRelative("miniGameBehaviour").objectReferenceValue;
            int id = entry.FindPropertyRelative("miniGameID").intValue;
            if (behaviour == game)
            {
                if (id != MiniGameId) throw new System.Exception($"Vytynanka is already registered with ID {id}.");
                return;
            }
            if (id == MiniGameId) throw new System.Exception($"Mini-game ID {MiniGameId} is already used by {behaviour}.");
        }

        list.arraySize++;
        var added = list.GetArrayElementAtIndex(list.arraySize - 1);
        added.FindPropertyRelative("miniGameID").intValue = MiniGameId;
        added.FindPropertyRelative("miniGameBehaviour").objectReferenceValue = game;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AddDevShortcut()
    {
        var registry = Object.FindFirstObjectByType<MiniGameRegistry>(FindObjectsInactive.Include);
        if (registry.GetComponent<VytynankaDevShortcut>() == null)
            registry.gameObject.AddComponent<VytynankaDevShortcut>();
    }

    // ---------- Helpers ----------

    static Transform Deep(Transform root, string name)
    {
        var found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        if (found == null) throw new System.Exception($"Vytynanka: {name} not found under {root.name}.");
        return found;
    }

    static RectTransform FindOrCreate(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return (RectTransform)existing;
        var created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return (RectTransform)created.transform;
    }

    //Explicit null checks: Unity's "missing" components aren't C# null, so ?? would not add them
    static T Graphic<T>(Transform t) where T : Component
    {
        T existing = t.GetComponent<T>();
        return existing != null ? existing : t.gameObject.AddComponent<T>();
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    //A side-column text in the hint's font
    static TMP_Text Text(Transform parent, string name, TMP_Text template, Vector2 position, Vector2 size, float fontSize, string text)
    {
        var existing = parent.Find(name);
        var label = existing != null ? existing.GetComponent<TMP_Text>() : Object.Instantiate(template, parent);
        label.name = name;
        Style(label, position, size, fontSize, text);
        return label;
    }

    static void Style(TMP_Text label, Vector2 position, Vector2 size, float fontSize, string text)
    {
        Place(label.rectTransform, position, size);
        label.text = text;
        label.color = TextColor;
        label.enableAutoSizing = false;
        label.fontSize = fontSize;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        EditorUtility.SetDirty(label);
    }

    static void Label(Button button, string text, float fontSize, Color color)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.text = text;
        label.color = color;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        Stretch(label.rectTransform); //The labels had a zero-size rect at the button's corner, so the text sat off the button
        EditorUtility.SetDirty(label.rectTransform);
        EditorUtility.SetDirty(label);

        //No keyboard navigation: Space or Enter (dialogue keys) must not press the last clicked button again
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        EditorUtility.SetDirty(button);
    }

    //Replaces the button's persistent click actions (re-running doesn't add duplicates)
    static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        Clear(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    static void WireString(Button button, UnityEngine.Events.UnityAction<string> action, string argument)
    {
        Clear(button);
        UnityEventTools.AddStringPersistentListener(button.onClick, action, argument);
        EditorUtility.SetDirty(button);
    }

    static void Clear(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    static Sprite Load(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.Exception($"Sprite not found: {path}");
        return sprite;
    }
}
