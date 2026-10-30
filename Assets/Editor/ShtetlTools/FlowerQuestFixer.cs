using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fixes the flower quest blocker found in the 2026-10-30 play-test: the purple flower was listed twice in
// ItemDictionary, so at runtime it got ID 8 instead of 7 (its quest objective), the wallet got 9 instead of 8 and the
// box's wallet gave a purple flower. Duplicate entries are removed, each prefab gets its list position as ID, and
// hidden-object items point at the item with their name.
// Idempotent. Batch: Unity.exe -batchmode -projectPath <project> -executeMethod FlowerQuestFixer.FixBatch -logFile <log>
public static class FlowerQuestFixer
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("Shtetl/Fix Flower Quest")]
    public static void FixMenu()
    {
        if (!LocationCapture.CanEditMainScene()) return;
        Fix();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Flower quest fixed. Save the scene (Ctrl+S) to keep it.");
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
        var dictionary = Object.FindFirstObjectByType<ItemDictionary>(FindObjectsInactive.Include);
        Undo.RecordObject(dictionary, "Fix item IDs");
        int before = dictionary.itemPrefabs.Count;
        var unique = new List<Item>();
        foreach (Item item in dictionary.itemPrefabs)
            if (item != null && !unique.Contains(item)) unique.Add(item);
        dictionary.itemPrefabs = unique;
        EditorUtility.SetDirty(dictionary);

        var idByName = new Dictionary<string, int>();
        for (int i = 0; i < unique.Count; i++)
        {
            Item prefab = unique[i];
            if (prefab.itemId != i + 1)
            {
                prefab.itemId = i + 1;
                EditorUtility.SetDirty(prefab);
            }
            idByName[prefab.Name] = i + 1;
            Debug.Log($"Item {i + 1}: {prefab.Name} ({prefab.gameObject.name})");
        }
        AssetDatabase.SaveAssets();

        foreach (var hidden in Object.FindObjectsByType<HiddenObjectItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(hidden);
            var id = so.FindProperty("itemId");
            string displayName = so.FindProperty("displayName").stringValue;
            if (!idByName.TryGetValue(displayName, out int newId))
            {
                Debug.LogWarning($"Hidden object '{displayName}' matches no item name; ID {id.intValue} kept.");
                continue;
            }
            if (id.intValue == newId) continue;
            Debug.Log($"Hidden object '{displayName}': ID {id.intValue} -> {newId}");
            id.intValue = newId;
            so.ApplyModifiedProperties();
        }

        Debug.Log($"Item dictionary: {before} entries -> {unique.Count}.");
    }
}
