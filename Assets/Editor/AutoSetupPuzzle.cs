using UnityEngine;
using UnityEditor;

public class AutoSetupPuzzle
{
    [MenuItem("Tools/Auto-Setup Gem Puzzle")]
    public static void Setup()
    {
        // 1. Create Prefabs for Red and Blue Gem
        GameObject redPrefab = CreateGemPrefab("Red Gem", "Assets/Sprites/RedGem.png");
        GameObject bluePrefab = CreateGemPrefab("Blue Gem", "Assets/Sprites/BlueGem.png");

        if (redPrefab == null || bluePrefab == null)
        {
            Debug.LogError("Failed to create Gem prefabs. Make sure RedGem.png and BlueGem.png exist in Assets/Sprites/");
            return;
        }

        // 2. Add to ItemDictionary
        ItemDictionary dict = Object.FindAnyObjectByType<ItemDictionary>();
        if (dict != null)
        {
            if (!dict.itemPrefabs.Contains(redPrefab.GetComponent<Item>()))
            {
                dict.itemPrefabs.Add(redPrefab.GetComponent<Item>());
            }
            if (!dict.itemPrefabs.Contains(bluePrefab.GetComponent<Item>()))
            {
                dict.itemPrefabs.Add(bluePrefab.GetComponent<Item>());
            }
            EditorUtility.SetDirty(dict);
        }
        else
        {
            Debug.LogWarning("Could not find ItemDictionary in scene. You will need to add the Red and Blue gems to it manually.");
        }

        // 3. Spawn Items in the scene
        GameObject redScene = (GameObject)PrefabUtility.InstantiatePrefab(redPrefab);
        redScene.transform.position = new Vector3(-8, 0, 0); // Left room roughly
        Undo.RegisterCreatedObjectUndo(redScene, "Spawn Red Gem");

        GameObject blueScene = (GameObject)PrefabUtility.InstantiatePrefab(bluePrefab);
        blueScene.transform.position = new Vector3(8, 0, 0); // Right room roughly
        Undo.RegisterCreatedObjectUndo(blueScene, "Spawn Blue Gem");

        // 4. Create Pedestal
        GameObject pedestal = new GameObject("Pedestal");
        pedestal.transform.position = new Vector3(0, 0, 0);
        
        BoxCollider2D pCol = pedestal.AddComponent<BoxCollider2D>();
        pCol.isTrigger = true;
        pCol.size = new Vector2(1, 1);

        ItemPedestal pScript = pedestal.AddComponent<ItemPedestal>();

        // Create a child object for the placed visual so it draws above the pedestal
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(pedestal.transform);
        visual.transform.localPosition = new Vector3(0, 0.5f, 0);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5; // Draw above other things
        pScript.placedItemSprite = sr;

        Undo.RegisterCreatedObjectUndo(pedestal, "Create Pedestal");

        Debug.Log("<color=#00FF00>Puzzle Setup Complete! Red Gem, Blue Gem, and Pedestal spawned.</color>");
    }

    private static GameObject CreateGemPrefab(string name, string spritePath)
    {
        string prefabPath = "Assets/Prefabs/" + name.Replace(" ", "") + ".prefab";
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null) return existingPrefab;

        GameObject obj = new GameObject(name);
        obj.tag = "Item";
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sprite = sprite;
        sr.sortingOrder = 2;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Item item = obj.AddComponent<Item>();
        item.Name = name;
        item.description = "A mysterious " + name;

        // Note: For inventory UI, it usually expects an Image component on the prefab,
        // because Item.cs does GetComponent<Image>() in ShowPopUp
        UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        Object.DestroyImmediate(obj);
        return prefab;
    }
}
