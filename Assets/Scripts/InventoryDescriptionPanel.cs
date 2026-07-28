using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDescriptionPanel : MonoBehaviour
{
    public static InventoryDescriptionPanel Instance { get; private set; }

    public GameObject descriptionPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public Image itemIconImage;

    public Button actionButton;
    public TMP_Text actionButtonText;
    
    private Item currentlySelectedItem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple InventoryDescriptionPanel instances detected! Destroying the extra one.");
            Destroy(gameObject);
            return;
        }

        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionClicked);
        }
    }

    public void ShowItemDescription(Item item)
    {
        if (item == null || descriptionPanel == null) return;

        currentlySelectedItem = item;
        descriptionPanel.SetActive(true);

        if (itemNameText != null)
        {
            itemNameText.text = item.Name;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
        }

        if (itemIconImage != null)
        {
            Image itemImage = item.GetComponent<Image>();
            itemIconImage.sprite = itemImage != null ? itemImage.sprite : null;
            itemIconImage.enabled = itemIconImage.sprite != null;
        }

        if (actionButton != null && actionButtonText != null)
        {
            actionButton.gameObject.SetActive(true);
            if (item is EquipmentItem)
            {
                actionButtonText.text = "Equip";
            }
            else
            {
                actionButtonText.text = "Use";
            }
        }
    }

    public void OnActionClicked()
    {
        if (currentlySelectedItem != null)
        {
            currentlySelectedItem.UseItem();
            ClearDescription(); // Close panel after use
        }
    }

    public void ClearDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
        currentlySelectedItem = null;
    }
}
