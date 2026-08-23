using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class AutoFixGemPrefabs
{
    [MenuItem("Tools/Fix Gem Prefabs (Collider + Sprite)")]
    public static void Fix()
    {
        string[] gemPrefabPaths = new[]
        {
            "Assets/Prefabs/RedGem.prefab",
            "Assets/Prefabs/BlueGem.prefab"
        };

        foreach (string path in gemPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Could not find prefab at: {path}");
                continue;
            }

            // Edit the prefab
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = editScope.prefabContentsRoot;

                // --- FIX 1: Resize the collider to match PPU=1000 sprite ---
                // At PPU=1000, a 1024x1024 sprite = ~1.024 Unity units
                // Use a small circle so the pickup zone feels natural
                BoxCollider2D box = root.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size   = new Vector2(1.0f, 1.0f);
                    box.offset = Vector2.zero;
                    Debug.Log($"<color=#00FF00>Fixed BoxCollider2D on {root.name}: size = (1, 1)</color>");
                }

                CircleCollider2D circle = root.GetComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = 0.5f;
                    circle.offset = Vector2.zero;
                    Debug.Log($"<color=#00FF00>Fixed CircleCollider2D on {root.name}: radius = 0.5</color>");
                }

                // --- FIX 2: Make sure SpriteRenderer has correct sprite and is enabled ---
                SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = true; // Must be enabled in the prefab for world display

                    // If SpriteRenderer has no sprite but Image does, sync it across
                    if (sr.sprite == null)
                    {
                        Image img = root.GetComponent<Image>();
                        if (img != null && img.sprite != null)
                        {
                            sr.sprite = img.sprite;
                            Debug.Log($"<color=#FFFF00>Synced sprite from Image to SpriteRenderer on {root.name}</color>");
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=#00FF00>Gem prefab fix complete!</color>");
    }
}
