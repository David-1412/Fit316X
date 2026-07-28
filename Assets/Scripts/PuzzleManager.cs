using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public DungeonSwitch[] switches;
    public GameObject portalObject;

    private int switchesPressed = 0;

    void Start()
    {
        // Subscribe to all switch events
        foreach (var sw in switches)
        {
            if (sw != null)
            {
                sw.OnSwitchActivated += HandleSwitchActivated;
            }
        }

        if (portalObject != null)
        {
            portalObject.GetComponent<SpriteRenderer>().enabled = false;
            portalObject.GetComponent<BoxCollider2D>().enabled = false;
        }
    }

    private void HandleSwitchActivated()
    {
        switchesPressed++;
        
        if (switchesPressed >= 4 && portalObject != null)
        {
            portalObject.GetComponent<SpriteRenderer>().enabled = true;
            portalObject.GetComponent<BoxCollider2D>().enabled = true;
            Debug.Log("Puzzle Solved! Portal Opened!");
        }
    }

    void OnDestroy()
    {
        // Clean up subscriptions
        foreach (var sw in switches)
        {
            if (sw != null)
            {
                sw.OnSwitchActivated -= HandleSwitchActivated;
            }
        }
    }
}
