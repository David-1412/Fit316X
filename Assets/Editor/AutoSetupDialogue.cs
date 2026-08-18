using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class AutoSetupDialogue
{
    [MenuItem("Tools/Auto-Setup Dialogue UI")]
    public static void Setup()
    {
        int fixesMade = 0;

        // 1. Setup DialogueUI
        GameObject uiObj = GameObject.Find("DialogueUI");
        if (uiObj != null)
        {
            DialogueController controller = uiObj.GetComponent<DialogueController>();
            if (controller == null)
            {
                controller = uiObj.AddComponent<DialogueController>();
                fixesMade++;
            }

            // Assign fields if null
            if (controller.dialoguePanel == null) { controller.dialoguePanel = uiObj; fixesMade++; }
            
            TMP_Text[] texts = uiObj.GetComponentsInChildren<TMP_Text>(true);
            foreach(var t in texts)
            {
                if (t.name.ToLower().Contains("name") && controller.nameText == null) { controller.nameText = t; fixesMade++; }
                else if ((t.name.ToLower().Contains("dialogue") || t.name.ToLower().Contains("text")) && controller.dialogueText == null) { controller.dialogueText = t; fixesMade++; }
            }

            Image[] images = uiObj.GetComponentsInChildren<Image>(true);
            foreach(var img in images)
            {
                if (img.name.ToLower().Contains("portrait") && controller.portraitImage == null) { controller.portraitImage = img; fixesMade++; }
            }

            if (controller.choiceContainer == null)
            {
                Transform choices = uiObj.transform.Find("Choices") ?? uiObj.transform.Find("ChoiceContainer");
                if (choices != null) { controller.choiceContainer = choices; fixesMade++; }
            }

            if (controller.choiceButtonPrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("ChoiceButton t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    controller.choiceButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    fixesMade++;
                }
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("<color=#00FF00>Successfully wired up DialogueUI components!</color>");
        }
        else
        {
            Debug.LogWarning("Could not find a GameObject named 'DialogueUI'. Make sure you named it exactly that!");
        }

        // 2. Setup Old Man NPC
        GameObject oldMan = GameObject.Find("Old Man");
        if (oldMan != null)
        {
            Collider2D col = oldMan.GetComponent<Collider2D>();
            if (col == null) 
            {
                col = oldMan.AddComponent<BoxCollider2D>();
                fixesMade++;
            }
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                fixesMade++;
            }

            EditorUtility.SetDirty(oldMan);
            Debug.Log("<color=#00FF00>Successfully configured Old Man physics collider!</color>");
        }
        else
        {
            Debug.LogWarning("Could not find a GameObject named 'Old Man'.");
        }

        if (fixesMade > 0)
        {
            Debug.Log($"<color=cyan>Auto-Setup Complete! Fixed/wired {fixesMade} missing references.</color>");
        }
        else
        {
            Debug.Log("Everything looks perfectly configured already!");
        }
    }
}
