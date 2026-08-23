using UnityEngine;
using UnityEditor;

public class AutoSetupDoor
{
    [MenuItem("Tools/Auto-Setup Door Cutscene Trigger")]
    public static void Setup()
    {
        // 1. Create a new Door object
        GameObject door = new GameObject("Door Trigger");
        
        // 2. Add and configure the Collider
        BoxCollider2D col = door.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1, 1);
        
        // 3. Add the Dialogue Trigger script
        DialogueTriggerArea trigger = door.AddComponent<DialogueTriggerArea>();
        trigger.triggerOnlyOnce = true;
        
        // 4. Try to find the Old Man to automatically hook it up
        GameObject oldManObj = GameObject.Find("Old Man");
        if (oldManObj != null)
        {
            NPC npc = oldManObj.GetComponent<NPC>();
            if (npc != null)
            {
                trigger.targetNPC = npc;
            }
            else
            {
                Debug.LogWarning("Found 'Old Man' but it doesn't have an NPC script attached.");
            }
        }
        else
        {
            Debug.LogWarning("Could not automatically find 'Old Man'. You will need to manually drag the Old Man into the Target NPC slot on the new Door Trigger object.");
        }
        
        // Register the undo operation so the user can easily revert if they want
        Undo.RegisterCreatedObjectUndo(door, "Create Door Trigger");
        
        // Select it in the hierarchy so the user sees it immediately
        Selection.activeGameObject = door;
        
        Debug.Log("<color=#00FF00>Successfully spawned a new Door Trigger and hooked it up to the Old Man!</color>");
    }
}
