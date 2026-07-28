using UnityEngine;

public class EquipmentItem : Item
{
    [Header("Equipment Slot")]
    public EquipmentSlot equipSlot;

    [Header("Equipment Stats")]
    public int attackBonus;
    public int hpBonus;
    public float critRateBonus;     // e.g. 0.05 for +5%
    public float critDamageBonus;   // e.g. 0.2 for +20%
    public string rarity;

    public override void UseItem()
    {
        base.UseItem();

        if (PlayerEquipment.Instance != null)
        {
            // Clone the item so it survives being removed from the inventory
            GameObject equippedItemObj = Instantiate(this.gameObject);
            equippedItemObj.SetActive(false); // Hide it
            equippedItemObj.transform.SetParent(PlayerEquipment.Instance.transform);
            
            EquipmentItem clonedItem = equippedItemObj.GetComponent<EquipmentItem>();

            // Equip the cloned item
            PlayerEquipment.Instance.EquipItem(clonedItem);
            
            Debug.Log($"Equipped {Name} ({rarity}) to {equipSlot}.");

            // Consume the original item from inventory
            if (InventoryController.EquipInstance != null)
            {
                InventoryController.EquipInstance.RemoveItemsFromInventory(ID, 1);
            }
        }
        else
        {
            Debug.LogWarning("PlayerEquipment Instance not found in scene!");
        }
    }
}
