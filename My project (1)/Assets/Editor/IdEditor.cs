using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IdEditor
{
    [MenuItem("Tools/Goblin Heist/Generate Collectable IDs")]
    public static void GenerateCollectibleIds()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("No valid scene is loaded.");
            return;
        }

        Collectible[] collectibles =
            UnityEngine.Object.FindObjectsByType<Collectible>();

        HashSet<string> usedIds = new HashSet<string>();

        int generatedCount = 0;

        foreach (Collectible collectible in collectibles)
        {
            if (collectible.gameObject.scene != scene)
                continue;

            string id = collectible.Id;

            // Generate a new ID if it is empty or duplicated.
            if (string.IsNullOrEmpty(id) || !usedIds.Add(id))
            {
                SerializedObject serializedObject =
                    new SerializedObject(collectible);

                SerializedProperty idProperty =
                    serializedObject.FindProperty("id");

                string newId;

                do
                {
                    newId = Guid.NewGuid().ToString();
                }
                while (!usedIds.Add(newId));

                if (idProperty == null)
                {
                    Debug.LogError(
                        $"Could not find serialized field 'id' on {collectible.name}. " +
                        "Check the field name in the Collectible script.",
                        collectible
                    );

                    continue;
                }
                idProperty.stringValue = newId;

                serializedObject.ApplyModifiedProperties();

                generatedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            $"Collectible ID generation complete. " +
            $"{generatedCount} IDs generated."
        );
    }
}
