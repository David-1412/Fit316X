using UnityEngine;
using UnityEditor;

public class AutoSetupTransition
{
    [MenuItem("Tools/Auto-Setup Transition to T2")]
    public static void Setup()
    {
        // 1. Create the transition object
        GameObject transitionObj = new GameObject("Transition to T2");
        
        // 2. Add and configure the Collider
        BoxCollider2D col = transitionObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2, 1);
        
        // 3. Add the MapTransition script
        MapTransition transitionScript = transitionObj.AddComponent<MapTransition>();
        
        // Using SerializedObject to set private/serialized fields since direction is a private enum
        SerializedObject so = new SerializedObject(transitionScript);
        SerializedProperty directionProp = so.FindProperty("direction");
        
        // MapTransition.Direction enum: Up=0, Down=1, Left=2, Right=3, Teleport=4
        if (directionProp != null)
        {
            directionProp.enumValueIndex = 0; // Direction.Up
            so.ApplyModifiedProperties();
        }

        // Try to automatically find T2 boundary if it exists
        GameObject t2Obj = GameObject.Find("T2");
        if (t2Obj != null)
        {
            PolygonCollider2D t2Poly = t2Obj.GetComponent<PolygonCollider2D>();
            if (t2Poly != null)
            {
                SerializedProperty boundryProp = so.FindProperty("mapBoundry");
                if (boundryProp != null)
                {
                    boundryProp.objectReferenceValue = t2Poly;
                    so.ApplyModifiedProperties();
                    Debug.Log("<color=#00FF00>Automatically linked the T2 boundary collider!</color>");
                }
            }
        }
        else
        {
            Debug.LogWarning("Could not find a GameObject named 'T2'. You will need to manually drag your T2 PolygonCollider2D into the 'Map Boundry' slot on the transition script!");
        }

        // Register undo
        Undo.RegisterCreatedObjectUndo(transitionObj, "Create Transition to T2");
        
        // Select it so the user can move it
        Selection.activeGameObject = transitionObj;
        
        Debug.Log("<color=#00FF00>Successfully spawned 'Transition to T2'. Use the Move Tool (W) to place it exactly in the top hallway!</color>");
    }
}
