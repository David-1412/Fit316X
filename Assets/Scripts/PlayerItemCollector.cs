using System.Collections;
using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private InventoryController inventoryController;
    private bool canPickUp = true; //cooldown flag
    [SerializeField] private float pickupCooldown = 0.1f;

    void Start()
    {
        if (inventoryController == null)
        {
            inventoryController = InventoryController.Instance ?? UnityEngine.Object.FindAnyObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            Debug.LogError("PlayerItemCollector: No InventoryController found in the scene. Add an InventoryController component to an active GameObject or assign it in the inspector.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canPickUp) return; // ignore during cooldown
        if (inventoryController == null) return;

        if (collision.CompareTag("Item"))
        {
            Item item = collision.GetComponent<Item>();
            if (item != null)
            {
                bool itemAdded = false;

                if (item is EquipmentItem)
                {
                    if (InventoryController.EquipInstance != null)
                        itemAdded = InventoryController.EquipInstance.AddItem(collision.gameObject);
                    else
                        Debug.LogWarning("No Equipment Inventory found! Make sure an InventoryController has isEquipmentInventory = true.");
                }
                else
                {
                    if (InventoryController.Instance != null)
                        itemAdded = InventoryController.Instance.AddItem(collision.gameObject);
                    else
                        Debug.LogWarning("No Main Inventory found!");
                }

                if (itemAdded)
                {
                    item.ShowPopUp();
                    Destroy(collision.gameObject);

                    StartCoroutine(PickupCooldownRoutine());
                }
            }
        }
    }

    private IEnumerator PickupCooldownRoutine()
    {
        canPickUp = false;
        yield return new WaitForSeconds(pickupCooldown);
        canPickUp = true;
    }
}
