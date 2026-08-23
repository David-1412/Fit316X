using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPedestal : MonoBehaviour, IInteractable
{
    [Header("Accepted Items")]
    public string item1Name = "Red Gem";
    public string item2Name = "Blue Gem";

    [Header("Visuals")]
    public SpriteRenderer placedItemSprite;

    private string currentItemName = "";

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (placedItemSprite != null)
        {
            placedItemSprite.sprite = null;
        }
    }

    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        InventoryController inv = InventoryController.Instance;
        if (inv == null) return;

        // If something is already placed, pick it back up
        if (!string.IsNullOrEmpty(currentItemName))
        {
            ItemDictionary dict = FindAnyObjectByType<ItemDictionary>();
            if (dict != null)
            {
                GameObject prefab = dict.GetItemPrefabByName(currentItemName);
                if (prefab != null)
                {
                    if (inv.AddItem(prefab))
                    {
                        Debug.Log("Picked up " + currentItemName + " from pedestal.");
                        currentItemName = "";
                        if (placedItemSprite != null) placedItemSprite.sprite = null;
                        
                        // Show popup
                        ItemPickupUIController ui = ItemPickupUIController.Instance;
                        if (ui != null)
                        {
                            ui.ShowItemPickup(prefab.GetComponent<Item>().Name, prefab.GetComponent<SpriteRenderer>().sprite);
                        }
                    }
                }
            }
            return;
        }

        // Otherwise, try to place an item
        if (inv.HasItem(item1Name))
        {
            PlaceItem(inv, item1Name);
        }
        else if (inv.HasItem(item2Name))
        {
            PlaceItem(inv, item2Name);
        }
        else
        {
            Debug.Log($"You need a {item1Name} or {item2Name} to place here!");
            // Optional: you can trigger a floating text or dialogue here if you have one
        }
    }

    private void PlaceItem(InventoryController inv, string itemName)
    {
        inv.RemoveItemByName(itemName, 1);
        currentItemName = itemName;
        
        ItemDictionary dict = FindAnyObjectByType<ItemDictionary>();
        if (dict != null)
        {
            GameObject prefab = dict.GetItemPrefabByName(itemName);
            if (prefab != null && placedItemSprite != null)
            {
                placedItemSprite.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
            }
        }
        Debug.Log("Placed " + itemName + " on the pedestal!");
    }
}
