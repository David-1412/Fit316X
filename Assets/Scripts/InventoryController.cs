using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    private ItemDictionary itemDictionary;

    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    public bool isEquipmentInventory;
    public static InventoryController Instance { get; private set; }
    public static InventoryController EquipInstance { get; private set; }
    Dictionary<int, int> itemsCountCache = new();
    public event Action OnInventoryChanged; //event to notify quest system (or any other system that needs to know!)

    private void Awake()
    {
        if (isEquipmentInventory)
        {
            if (EquipInstance != null && EquipInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            EquipInstance = this;
        }
        else
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        itemDictionary = UnityEngine.Object.FindAnyObjectByType<ItemDictionary>();

        // Generate empty slots if none exist
        if (inventoryPanel != null && inventoryPanel.transform.childCount == 0 && slotPrefab != null)
        {
            for (int i = 0; i < slotCount; i++)
            {
                Instantiate(slotPrefab, inventoryPanel.transform);
            }
        }

        RebuildItemCounts();
    }

    public void RebuildItemCounts()
    {
        itemsCountCache.Clear();

        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            Slot slot = slotTranform.GetComponent<Slot>();
            if(slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if(item != null)
                {
                    itemsCountCache[item.ID] = itemsCountCache.GetValueOrDefault(item.ID, 0) + item.quantity;
                }
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public Dictionary<int, int> GetItemCounts() => itemsCountCache;

    private void SetupUIItem(GameObject itemObj)
    {
        itemObj.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = itemObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(100, 100); // Give it a visible, clickable size
        }
        
        UnityEngine.UI.Image img = itemObj.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.raycastTarget = true;

        SpriteRenderer sr = itemObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (itemObj.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup cg = itemObj.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
        
        if (itemObj.GetComponent<ItemDragHandler>() == null)
        {
            itemObj.AddComponent<ItemDragHandler>();
        }
    }

    public bool AddItem(GameObject itemPrefab)
    {
        Item itemToAdd = itemPrefab.GetComponent<Item>();
        if (itemToAdd == null) return false;

        if (itemDictionary != null)
        {
            GameObject cleanPrefab = itemDictionary.GetItemPrefab(itemToAdd.ID);
            if (cleanPrefab != null)
            {
                itemPrefab = cleanPrefab;
                itemToAdd = itemPrefab.GetComponent<Item>();
            }
        }

        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            Slot slot = slotTranform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item slotItem = slot.currentItem.GetComponent<Item>();
                if(slotItem != null && slotItem.ID == itemToAdd.ID)
                {
                    slotItem.AddToStack();
                    RebuildItemCounts();
                    return true;
                }
            }
        }

        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            Slot slot = slotTranform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                GameObject newItem = Instantiate(itemPrefab, slotTranform);
                SetupUIItem(newItem);

                slot.currentItem = newItem;
                RebuildItemCounts();
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            Slot slot = slotTranform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData 
                { 
                    itemID = item.ID, 
                    slotIndex = slotTranform.GetSiblingIndex(), 
                    quantity = item.quantity
                });
            }
        }
        return invData;
    }

    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        foreach (Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    SetupUIItem(item);

                    Item itemComponent = item.GetComponent<Item>();
                    if(itemComponent != null && data.quantity > 1)
                    {
                        itemComponent.quantity = data.quantity;
                        itemComponent.UpdateQuantityDisplay();
                    }

                    slot.currentItem = item;
                }
            }
        }

        RebuildItemCounts();
    }

    public void RemoveItemsFromInventory(int itemID, int amountToRemove)
    {
        foreach(Transform slotTranform in inventoryPanel.transform)
        {
            if (amountToRemove <= 0) break;

            Slot slot = slotTranform.GetComponent<Slot>();
            if(slot?.currentItem?.GetComponent<Item>() is Item item && item.ID == itemID)
            {
                int removed = Mathf.Min(amountToRemove, item.quantity);
                item.RemoveFromStack(removed);
                amountToRemove -= removed;

                if(item.quantity == 0)
                {
                    Destroy(slot.currentItem);
                    slot.currentItem = null;
                }
            }
        }

        RebuildItemCounts();
    }

    public bool HasItem(string itemName)
    {
        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            Slot slot = slotTranform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && item.Name == itemName && item.quantity > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void RemoveItemByName(string itemName, int amountToRemove = 1)
    {
        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            if (amountToRemove <= 0) break;
            Slot slot = slotTranform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && item.Name == itemName)
                {
                    int removed = Mathf.Min(amountToRemove, item.quantity);
                    item.RemoveFromStack(removed);
                    amountToRemove -= removed;
                    if (item.quantity <= 0)
                    {
                        Destroy(slot.currentItem);
                        slot.currentItem = null;
                    }
                }
            }
        }
        RebuildItemCounts();
    }
}
