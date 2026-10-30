using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Makes UI text fit its containers (found in the 2026-10-30 play-test: dialogue text below the panel, clipped quest
// notifications, overlapping journal lines, key buttons wider than their row). Text keeps its designed size and only
// shrinks (auto-size, with a readable minimum) when a long line needs it; boxes use the space their panel has.
// Idempotent. Scene objects are found through the components that use them; prefabs are edited in place.
// Batch: Unity.exe -batchmode -projectPath <project> -executeMethod UITextLayoutFixer.FixBatch -logFile <log>
public static class UITextLayoutFixer
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const float NotificationWidth = 410f; //keeps the notification column (x 40..450) left of the dialogue panel (x 460)

    [MenuItem("Shtetl/Fix UI Text Layout")]
    public static void FixMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        Fix();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("UI text layout fixed. Save the scene (Ctrl+S) to keep it.");
    }

    public static void FixBatch()
    {
        int exitCode = 0;
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Fix();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }

    static void Fix()
    {
        FixDialogue();
        FixInfoPopup();
        FixNotifications();
        FixLists();
        FixKeyChoice();
        FixPrefab("Assets/Prefabs/UI/ChoiceButton.prefab", root => AutoSize(root.GetComponentInChildren<TMP_Text>(true), 18f, false));
        FixPrefab("Assets/Prefabs/UI/KeyChoiceButton.prefab", root => AutoSize(root.GetComponentInChildren<TMP_Text>(true), 16f, true));
        FixPrefab("Assets/Prefabs/UI/TargetTemplate.prefab", root =>
        {
            TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
            AutoSize(label, 12f, true);
            label.rectTransform.sizeDelta = new Vector2(116f, 40f); //two short lines under the icon
        });
        Debug.Log("UI text layout: dialogue, info popup, notifications, quest/journal lists, Key Choice, choice, key and target buttons.");
    }

    //Dialogue panel 1000 x 175: the text uses the area right of the portrait and below the name,
    //clear of the close button; three lines fit at the minimum size
    static void FixDialogue()
    {
        var dialogue = Object.FindFirstObjectByType<DialogueController>(FindObjectsInactive.Include);
        TMP_Text text = dialogue.dialogueText;
        var rect = text.rectTransform;
        Undo.RecordObject(rect, "Fix dialogue text");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(241f, 12f);
        rect.offsetMax = new Vector2(-80f, -64f);
        AutoSize(text, 26f, true);
        text.verticalAlignment = VerticalAlignmentOptions.Top;

        AutoSize(dialogue.nameText, 20f, false);
    }

    static void FixInfoPopup()
    {
        var popup = Object.FindFirstObjectByType<GraveInfoUI>(FindObjectsInactive.Include);
        var so = new SerializedObject(popup);
        AutoSize((TMP_Text)so.FindProperty("symbolTitleText").objectReferenceValue, 40f, false);
        AutoSize((TMP_Text)so.FindProperty("descriptionText").objectReferenceValue, 26f, true);
    }

    //The info popup (GraveInfoUI) fits its content at runtime; here: a less see-through panel, so the room behind it
    //doesn't distract, and the text printed on an image (e.g. the invitation's wording), over the image's blank centre.
    //Idempotent; also run by Shtetl → Apply House Art.
    public static void FixDiscoveryPopup()
    {
        var popup = Object.FindFirstObjectByType<GraveInfoUI>(FindObjectsInactive.Include);
        var so = new SerializedObject(popup);
        var panel = (GameObject)so.FindProperty("panel").objectReferenceValue;
        var image = (Image)so.FindProperty("graveImage").objectReferenceValue;
        var description = (TMP_Text)so.FindProperty("descriptionText").objectReferenceValue;

        var background = panel.GetComponent<Image>();
        Undo.RecordObject(background, "Popup opacity");
        Color color = background.color;
        color.a = 0.94f; //was 0.79
        background.color = color;

        Transform existing = image.transform.Find("ImageText");
        GameObject textObject;
        if (existing != null)
        {
            textObject = existing.gameObject;
        }
        else
        {
            textObject = Object.Instantiate(description.gameObject, image.transform);
            textObject.name = "ImageText";
            Undo.RegisterCreatedObjectUndo(textObject, "Popup image text");
        }
        var text = textObject.GetComponent<TMP_Text>();
        Undo.RecordObject(text, "Popup image text");
        Undo.RecordObject(text.rectTransform, "Popup image text");
        text.rectTransform.anchorMin = new Vector2(0.17f, 0.24f); //Inside the invitation's border, between the
        text.rectTransform.anchorMax = new Vector2(0.83f, 0.74f); //synagogue at the top and the rings at the bottom
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.text = "";
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.30f, 0.21f, 0.15f); //Faded ink
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMax = 34f;
        text.fontSizeMin = 20f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        textObject.SetActive(false);

        so.FindProperty("imageOverlayText").objectReferenceValue = text;
        so.ApplyModifiedProperties();
    }

    //Quest and item notifications: the text fills the popup right of the icon and may take two or three lines
    static void FixNotifications()
    {
        var controller = Object.FindFirstObjectByType<ItemPickupUIController>(FindObjectsInactive.Include);
        var column = (RectTransform)controller.transform;
        Undo.RecordObject(column, "Fix notification column");
        column.sizeDelta = new Vector2(NotificationWidth, column.sizeDelta.y);

        FixPrefab(AssetDatabase.GetAssetPath(controller.popupPrefab), root =>
        {
            var popupRect = (RectTransform)root.transform;
            popupRect.sizeDelta = new Vector2(NotificationWidth, popupRect.sizeDelta.y);
            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(95f, 8f); //right of the 75 px icon
            textRect.offsetMax = new Vector2(-12f, -8f);
            AutoSize(text, 20f, true);
            text.horizontalAlignment = HorizontalAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
        });
    }

    //Quest log and journal: rows take the height their wrapped text needs, so long lines can't overlap the next one
    static void FixLists()
    {
        foreach (MonoBehaviour owner in new MonoBehaviour[]
                 {
                     Object.FindFirstObjectByType<QuestController>(FindObjectsInactive.Include),
                     Object.FindFirstObjectByType<JournalController>(FindObjectsInactive.Include)
                 })
        {
            var content = (Transform)new SerializedObject(owner).FindProperty("listContent").objectReferenceValue;
            ControlChildren(content.GetComponent<VerticalLayoutGroup>());
        }

        FixPrefab("Assets/Prefabs/UI/EntryBlock.prefab", root =>
        {
            ControlChildren(root.GetComponent<VerticalLayoutGroup>());
            RemoveFitter(root); //the list controls the block's size now
            Transform lines = root.transform.Find("LinesContainer");
            ControlChildren(lines.GetComponent<VerticalLayoutGroup>());
            RemoveFitter(lines.gameObject);
            Wrap(root.transform.Find("TitleText").GetComponent<TMP_Text>());
        });
        FixPrefab("Assets/Prefabs/UI/LineText.prefab", root => Wrap(root.GetComponent<TMP_Text>()));
    }

    //Up to four keys share the 700 px row instead of overflowing it
    static void FixKeyChoice()
    {
        var keyChoice = Object.FindFirstObjectByType<KeyChoiceGameController>(FindObjectsInactive.Include);
        var buttons = (Transform)new SerializedObject(keyChoice).FindProperty("buttonsContainer").objectReferenceValue;
        var row = buttons.GetComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(row, "Fix key row");
        row.childControlWidth = true;
        row.childForceExpandWidth = true;
        EditorUtility.SetDirty(row);
    }

    static void ControlChildren(VerticalLayoutGroup group)
    {
        Undo.RecordObject(group, "Fix list layout");
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        EditorUtility.SetDirty(group);
    }

    static void RemoveFitter(GameObject go)
    {
        var fitter = go.GetComponent<ContentSizeFitter>();
        if (fitter != null) Object.DestroyImmediate(fitter, true);
    }

    static void Wrap(TMP_Text text)
    {
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(text);
    }

    //Keeps the designed size as the maximum; shrinks only as far as needed, never below the readable minimum
    static void AutoSize(TMP_Text text, float minSize, bool wrap)
    {
        Undo.RecordObject(text, "Fix text size");
        if (!text.enableAutoSizing) text.fontSizeMax = text.fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(text);
    }

    static void FixPrefab(string path, System.Action<GameObject> change)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
