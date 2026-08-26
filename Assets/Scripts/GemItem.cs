using UnityEngine;

/// <summary>
/// Add this component alongside the Item component on any gem prefab.
/// It marks the item as a placeable gem and stores its beam colour.
/// 
/// To add a new gem colour in the future:
///   1. Duplicate an existing gem prefab.
///   2. Change Item.Name (e.g. "Green Gem").
///   3. Set GemItem.gemColor to the desired colour.
///   4. That's it — BeamMachine will auto-detect it.
/// </summary>
[RequireComponent(typeof(Item))]
public class GemItem : MonoBehaviour
{
    [Tooltip("The colour this gem produces when placed in a BeamMachine.")]
    public Color gemColor = Color.red;

    /// <summary>Convenience accessor for the sibling Item component.</summary>
    public Item Item => GetComponent<Item>();
}
