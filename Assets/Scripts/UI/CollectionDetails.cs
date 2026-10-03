using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CollectionDetails : MonoBehaviour
{
    public string Name;
    public string Description;


    [System.Serializable]
    public struct LoreCollection
    {
        public string Subtitle;
        public List<string> Bodies;
        public string Counter;
        public int Count;

        public readonly bool PrereqMet()
        {
            if (Counter == null) { return true; }
            else { return CollectableManager.Instance.GetStacks(Counter) >= Count; }
        }
    }


    [Serialize]
    public List<LoreCollection> Lore;


    [SerializeField]
    public Sprite Image;


    private CollectionDetailsManager displayManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!Image) { Image = gameObject.GetComponent<Image>().sprite; }
        displayManager = FindAnyObjectByType<CollectionDetailsManager>();
    }

    public void Display()
    {
        // Updates the display manager if it is somehow lost
        if (displayManager == null || !displayManager.isActiveAndEnabled)
        {
            displayManager = FindAnyObjectByType<CollectionDetailsManager>();
            Debug.Log("Display Manager lost. Locating new manager at", displayManager);
        }

        if (displayManager == null)
        {
            Debug.LogError("No display manager could be found");
        }

        // Allows display to toggle
        if (displayManager.CurrentDisplay == this)
        {
            Debug.Log("CLoseDisplay");
            displayManager.CloseDisplay();
        }
        else
        {
            displayManager.CurrentDisplay= this;
            displayManager.Display(this);
        }
    }

    
}
