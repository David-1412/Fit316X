using UnityEngine;
using UnityEditor;

public class AutoFixInteractionDetector
{
    [MenuItem("Tools/Fix Interaction Detector Radius")]
    public static void FixRadius()
    {
        InteractionDetector detector = Object.FindAnyObjectByType<InteractionDetector>();

        if (detector == null)
        {
            Debug.LogError("No InteractionDetector found in the scene! Make sure your Player has it attached.");
            return;
        }

        Undo.RecordObject(detector, "Fix Interaction Detector Radius");

        detector.detectionRadius  = 3.5f;
        detector.interactionLayer = ~0; // All layers

        EditorUtility.SetDirty(detector);

        Debug.Log($"<color=#00FF00>Fixed InteractionDetector on '{detector.gameObject.name}':</color> radius = 3.5, layer = Everything");
        Debug.Log($"<color=#FFFF00>TIP: You can see the detection circle in Scene view when the Player is selected.</color>");
    }
}
