using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BulbPuzzle : MonoBehaviour, IMultiInteractable
{

    public bool removeable = false;
  


    public UnityEvent OnLightOn;
    public UnityEvent OnLightOff;
    public UnityEvent OnLightGreen;
    public UnityEvent OnLightBlue;
    public UnityEvent OnLightRed;





    [SerializeField]
    private GemEntry? currentGem;

    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void lockGem() { removeable = false; }


    // ?? Gem Discovery ?????????????????????????????????????????????????????
    [Serializable]
    private struct GemEntry
    {
        public string name;
        public Color color;
        public bool core;
        //public Sprite sprite;
    }

    /// <summary>
    /// Returns all gem-like items in the player inventory.
    /// Priority: GemItem component ? name-based color inference.
    /// </summary>
    private List<GemEntry> GetGemsInInventory()
    {
        var result = new List<GemEntry>();
        var inv = InventoryController.Instance;
        if (inv == null || inv.inventoryPanel == null)
        {
            Debug.LogWarning("[BeamMachine] InventoryController.Instance or inventoryPanel is null!");
            return result;
        }

        foreach (Transform slotTransform in inv.inventoryPanel.transform)
        {
            var slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            var item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.quantity <= 0) continue;

            var sr = slot.currentItem.GetComponent<SpriteRenderer>()
                  ?? slot.currentItem.GetComponentInChildren<SpriteRenderer>();

            // ?? Option A: GemItem component (explicit, preferred) ?????????
            var gemComp = slot.currentItem.GetComponent<GemItem>();
            if (gemComp != null) { 
            
                result.Add(new GemEntry {
                    name = gemComp.gameObject.GetComponent<Item>().Name, 
                    color = gemComp.gemColor,
                    core = gemComp.isCore
                });
                continue;
            }
        }

        Debug.Log($"[BULB] Found {result.Count} gem(s) in inventory.");
        return result;
    }

    private string colourName(Color colour)
    {
        if (colour == Color.red) { return "Red"; }
        if (colour == Color.green) { return "Green"; }
        if (colour == Color.blue) { return "Blue"; } 
        if (colour == Color.white) { return "White"; }
        if (colour == Color.black) { return "Black"; }
        else { return "Unknown"; }
    }

    // ?? IMultiInteractable ????????????????????????????????????????????????
    public bool CanInteract() => removeable || currentGem == null ? true : ((GemEntry) currentGem).core;
     

    public void Interact()
    {
        var options = GetInteractionOptions();
        if (options.Count > 0) options[0].OnSelect?.Invoke();
    }

    public List<InteractionOption> GetInteractionOptions()
    {

        var options = new List<InteractionOption>();

        // Place gems

        if (currentGem == null)
        {


            var invGems = GetGemsInInventory();
            foreach (var gem in invGems)
            {
                options.Add(new InteractionOption
                {
                    Name = $"Place {gem.name}",
                    OnSelect = () => PlaceGem(gem)
                });
            }
        }
        else
        {
            if (removeable || currentGem?.core == true)
            {


                options.Add(new InteractionOption
                {
                    Name = $"Take Back {currentGem?.name}",
                    OnSelect = () => TakeGem()
                });
            }

        }

        return options;
    }

    // ?? Machine Actions ???????????????????????????????????????????????????

    private void PlaceGem(GemEntry gem)
    {
        Debug.Log(gem.name);
        InventoryController.Instance?.RemoveItemByName(gem.name);
        currentGem = gem;

        SoundEffectManager.Play("PickUp");
        Debug.Log($"[BULB] Placed '{currentGem?.name}");


        if (currentGem == null)
        {
            Debug.Log("[BULB] Gem Placement Failed");
            return;
        }

        OnLightOn.Invoke();
        switch (colourName(((GemEntry) currentGem).color))
        {
            case "Blue":
                OnLightBlue.Invoke();
                break;

            case "Green":
                OnLightGreen.Invoke();
                break ;

            case "Red":
                OnLightRed.Invoke();
                break ;

            default:
                break;
        }
    }


    private void TakeGem()
    {
        if (currentGem?.core == false && removeable == false)
        {
            return; 
        }

        var dict = FindAnyObjectByType<ItemDictionary>();
        if (dict == null) return;

        var prefab = dict.GetItemPrefabByName(currentGem?.name);
        if (prefab != null && InventoryController.Instance != null)
        {
            if (InventoryController.Instance.AddItem(prefab))
            {
                ItemPickupUIController.Instance?.ShowItemPickup(
                    prefab.GetComponent<Item>().Name,
                    prefab.GetComponent<SpriteRenderer>()?.sprite);
            }
            currentGem = null;
            OnLightOff.Invoke();
        }
        Debug.Log($"[BULB] Returned gem");
    }

}
