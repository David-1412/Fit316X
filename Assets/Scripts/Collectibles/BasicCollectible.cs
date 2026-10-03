using System.Collections.Generic;
using UnityEngine;

public class BasicCollectible : MonoBehaviour, IMultiInteractable, ICollectible
{
    public string collectableID;
    public string collectableName;
    public bool isSet = false;
    public int quantity = 1;
    public bool canInteract = true;

    public bool CanInteract() => canInteract; 
    public string id() => collectableID;
    bool ICollectible.isSet() => isSet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CollectableManager.Instance.SetFlag(collectableID, false);
        Debug.Log(CollectableManager.Instance);
    }

    
    public void Interact()
    {
        pickUp();
    }

    public List<InteractionOption> GetInteractionOptions()
    {
        return new(){new InteractionOption
        {
            Name = $"Pick Up {collectableName}",
            OnSelect = () => pickUp()
        }};
    }

    private void pickUp() {
        if (isSet)
        {
            CollectableManager.Instance.IncrementStack(collectableID, quantity);
        }
        else
        {
            CollectableManager.Instance.SetFlag(collectableID, true);
            Debug.Log(CollectableManager.Instance.GetFlag(collectableID));
        }

        Destroy(gameObject);
    }

    
}
