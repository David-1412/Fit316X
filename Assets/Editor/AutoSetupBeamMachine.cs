using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoSetupBeamMachine
{
    [MenuItem("Tools/Auto-Setup Beam Machine")]
    public static void Setup()
    {
        // 1. Find the old Pedestal
        GameObject pedestal = GameObject.Find("Pedestal");
        if (pedestal == null)
        {
            Debug.LogError("Could not find an object named 'Pedestal' in the scene to upgrade!");
            return;
        }

        // 2. Replace Script
        ItemPedestal oldScript = pedestal.GetComponent<ItemPedestal>();
        SpriteRenderer placedItemSprite = null;
        if (oldScript != null)
        {
            placedItemSprite = oldScript.placedItemSprite;
            Object.DestroyImmediate(oldScript);
        }

        BeamMachine newScript = pedestal.AddComponent<BeamMachine>();
        newScript.placedItemSprite = placedItemSprite;
        newScript.obstacleLayer = LayerMask.GetMask("Obstacle", "Wall"); // set default obstacle layers

        // 3. Move the generated sprite to Assets
        string sourceSpritePath = "C:/Users/David/.gemini/antigravity/brain/ced579b6-b25c-4aac-9d71-b549bb30c5d9/beam_nozzle_1787481544741.png";
        string targetSpriteDir = "Assets/Sprites";
        string targetSpritePath = targetSpriteDir + "/beam_nozzle.png";

        if (!AssetDatabase.IsValidFolder(targetSpriteDir))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        if (File.Exists(sourceSpritePath))
        {
            File.Copy(sourceSpritePath, targetSpritePath, true);
            AssetDatabase.Refresh();

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(targetSpritePath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        else
        {
            Debug.LogWarning("Generated nozzle image not found! You may need to assign a sprite to the Nozzle manually.");
        }

        // 4. Create Nozzle Visual
        Transform existingNozzle = pedestal.transform.Find("Nozzle");
        GameObject nozzleObj = existingNozzle != null ? existingNozzle.gameObject : new GameObject("Nozzle");
        nozzleObj.transform.SetParent(pedestal.transform, false);
        nozzleObj.transform.localPosition = new Vector3(0, 0, -0.1f); // Slightly in front of gem
        
        SpriteRenderer nozzleSR = nozzleObj.GetComponent<SpriteRenderer>();
        if (nozzleSR == null) nozzleSR = nozzleObj.AddComponent<SpriteRenderer>();
        
        Sprite nozzleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetSpritePath);
        if (nozzleSprite != null) nozzleSR.sprite = nozzleSprite;

        newScript.nozzleTransform = nozzleObj.transform;

        // Note: BeamMachine now manages its own LineRenderer pool internally.
        // A default Sprites/Default material will be auto-assigned to the first segment.
        // If you want a custom material, drag it onto the first BeamSegment_0 child at runtime.

        // 6. Ensure tag/layer are correct
        pedestal.layer = LayerMask.NameToLayer("Interactable") > -1 ? LayerMask.NameToLayer("Interactable") : 0;

        Undo.RegisterCompleteObjectUndo(pedestal, "Upgrade Pedestal to Beam Machine");
        Debug.Log("<color=#00FF00>Successfully upgraded Pedestal into a Beam Machine!</color>");
    }
}
