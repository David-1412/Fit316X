using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;

    private InventoryController inventoryController;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inventoryController = InventoryController.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; //Save OG parent
        transform.SetParent(transform.root); //Above other canvas'
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f; //Semi-transparent during drag
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; //Follow the mouse
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; //Enables raycasts
        canvasGroup.alpha = 1f; //No longer transparent

        // Check for Equipment Drop
        EquipmentDropHandler equipDrop = eventData.pointerEnter?.GetComponent<EquipmentDropHandler>();
        if (equipDrop == null && eventData.pointerEnter != null)
        {
            equipDrop = eventData.pointerEnter.GetComponentInParent<EquipmentDropHandler>();
        }

        if (equipDrop != null)
        {
            EquipmentItem equipItem = GetComponent<EquipmentItem>();
            if (equipItem != null && equipItem.equipSlot == equipDrop.targetSlot)
            {
                equipItem.UseItem();
                return; // Stop drag logic, item is consumed
            }
        }

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>(); //Slot where item dropped
        if(dropSlot == null)
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null)
            {
                dropSlot = dropItem.GetComponentInParent<Slot>();
            }
        }
        Slot originalSlot = originalParent != null ? originalParent.GetComponent<Slot>() : null;

        if (dropSlot == originalSlot)
        {
            transform.SetParent(originalParent);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }

        if (dropSlot != null)
        {
            //Is a slot under drop point
            if (dropSlot.currentItem != null)
            {
                Item draggedItem = GetComponent<Item>();
                Item targetItem = dropSlot.currentItem.GetComponent<Item>();

                if(draggedItem.ID == targetItem.ID)
                {
                    targetItem.AddToStack(draggedItem.quantity);
                    originalSlot.currentItem = null;
                    Destroy(gameObject);
                }
                else
                {
                    //Slot has an item - swap items
                    dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                    originalSlot.currentItem = dropSlot.currentItem;
                    dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    transform.SetParent(dropSlot.transform);
                    dropSlot.currentItem = gameObject;
                    GetComponent<RectTransform>().anchoredPosition = Vector2.zero; //Center
                }
            }
            else
            {
                originalSlot.currentItem = null;
                transform.SetParent(dropSlot.transform);
                dropSlot.currentItem = gameObject;
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero; //Center
            }
        }
        else
        {
            //No slot under drop point
            //If where we're dropping is not within the inventory
            if (!IsWithinInventory(eventData.position))
            {
                //Drop our item
                DropItem(originalSlot);
            }
            else
            {
                //Snap back to og slot
                transform.SetParent(originalParent);
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero; //Center
            }
        }
    }

    bool IsWithinInventory(Vector2 mousePosition)
    {
        RectTransform inventoryRect = originalParent.parent.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition);
    }

    void DropItem(Slot originalSlot)
    {
        Item item = GetComponent<Item>();
        if (item == null) return;

        int quantity = item.quantity;

        // Find the correct WORLD prefab from ItemDictionary (NOT the UI clone)
        ItemDictionary dict = Object.FindAnyObjectByType<ItemDictionary>();
        GameObject worldPrefab = dict != null ? dict.GetItemPrefab(item.ID) : null;

        if (worldPrefab == null)
        {
            // Fallback: snap back rather than crash
            Debug.LogWarning($"Could not find world prefab for item '{item.Name}' (ID {item.ID}). Snapping back.");
            transform.SetParent(originalParent);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }

        if (quantity > 1)
        {
            item.RemoveFromStack();
            transform.SetParent(originalParent);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            quantity = 1;
        }
        else
        {
            if (originalSlot != null) originalSlot.currentItem = null;
            Destroy(gameObject); // Remove the UI item
        }

        // Find player
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Missing 'Player' tag");
            return;
        }

        // Spawn the world prefab near the player
        Vector2 dropOffset = Random.insideUnitCircle.normalized * Random.Range(minDropDistance, maxDropDistance);
        Vector2 dropPosition = (Vector2)playerTransform.position + dropOffset;

        GameObject dropItem = Instantiate(worldPrefab, dropPosition, Quaternion.identity);
        Item droppedItemComp = dropItem.GetComponent<Item>();
        if (droppedItemComp != null) droppedItemComp.quantity = 1;

        // Re-enable world visuals and fix sorting so it appears above the floor
        SpriteRenderer sr = dropItem.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            sr.sortingLayerName = "Collision"; // Same layer gems use in scene
            sr.sortingOrder = 5;               // Above the ground tilemaps

            // If SR has no sprite, sync from Image
            if (sr.sprite == null)
            {
                UnityEngine.UI.Image img = dropItem.GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.sprite != null)
                    sr.sprite = img.sprite;
            }
        }

        // Ensure it is on the Default layer (not UI)
        dropItem.layer = LayerMask.NameToLayer("Default");

        // Disable the Image component so it doesn't interfere with world rendering
        UnityEngine.UI.Image imageComp = dropItem.GetComponent<UnityEngine.UI.Image>();
        if (imageComp != null) imageComp.enabled = false;

        BounceEffect bounce = dropItem.GetComponent<BounceEffect>();
        if (bounce != null) bounce.StartBounce();

        InventoryController.Instance.RebuildItemCounts();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Item item = GetComponent<Item>();
            Debug.Log($"<color=#FFFF00>ItemDragHandler Clicked on: {gameObject.name}</color>");
            
            if (item != null)
            {
                if (item is EquipmentItem equipItem)
                {
                    if (EquipmentTooltipPanel.Instance != null)
                    {
                        Debug.Log("<color=#00FF00>Showing EquipmentTooltipPanel!</color>");
                        EquipmentTooltipPanel.Instance.ShowTooltip(equipItem, false);
                    }
                    else
                    {
                        Debug.LogError("<color=#FF0000>EquipmentTooltipPanel.Instance is NULL! Did you delete the generated UI from your Canvas?</color>");
                    }
                }
                else
                {
                    Debug.Log("Item is not an EquipmentItem, showing standard description panel.");
                    InventoryDescriptionPanel.Instance?.ShowItemDescription(item);
                }
            }
            else
            {
                Debug.LogWarning("No Item component found on clicked object!");
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            SplitStack();
        }
    }

    private void SplitStack()
    {
        Item item = GetComponent<Item>();
        if (item == null || item.quantity <= 1) return;

        int splitAmount = item.quantity / 2;
        if (splitAmount <= 0) return;

        item.RemoveFromStack(splitAmount);

        GameObject newItem = item.CloneItem(splitAmount);

        if (inventoryController == null || newItem == null) return;

        foreach(Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if(slot != null && slot.currentItem == null)
            {
                slot.currentItem = newItem;
                newItem.transform.SetParent(slot.transform);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                return;
            }
        }

        //No empty slot - return to stack
        item.AddToStack(splitAmount);
        Destroy(newItem);
    }
}
