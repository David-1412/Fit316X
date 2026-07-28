using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentTooltipPanel : MonoBehaviour
{
    public static EquipmentTooltipPanel Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;
    public TMP_Text itemNameText;
    public TMP_Text itemStatsText;
    public Image itemIconImage;
    public Button actionButton;
    public TMP_Text actionButtonText;
    public Button closeButton;

    private EquipmentItem currentItem;
    private bool isCurrentlyEquipped;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (actionButton != null) actionButton.onClick.AddListener(OnActionClicked);
        if (closeButton != null) closeButton.onClick.AddListener(HideTooltip);
    }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void ShowTooltip(EquipmentItem item, bool isEquipped)
    {
        if (item == null || panelRoot == null) return;

        currentItem = item;
        isCurrentlyEquipped = isEquipped;

        // Set UI values
        itemNameText.text = $"{item.Name} <size=60%><color=#AAAAAA>({item.rarity})</color></size>";
        
        // Build stats string
        string stats = "";
        if (item.attackBonus > 0) stats += $"<color=#FF5555>Attack: +{item.attackBonus}</color>\n";
        if (item.hpBonus > 0) stats += $"<color=#55FF55>HP: +{item.hpBonus}</color>\n";
        if (item.critRateBonus > 0) stats += $"<color=#FFFF55>Crit Rate: +{item.critRateBonus * 100}%</color>\n";
        if (item.critDamageBonus > 0) stats += $"<color=#FFAA00>Crit Dmg: +{item.critDamageBonus * 100}%</color>\n";
        
        if (string.IsNullOrEmpty(stats)) stats = "No bonus stats.";
        
        itemStatsText.text = stats + $"\n<size=80%><color=#AAAAAA><i>{item.description}</i></color></size>";

        // Set Icon
        Image itemImage = item.GetComponent<Image>();
        if (itemIconImage != null && itemImage != null)
        {
            itemIconImage.sprite = itemImage.sprite;
            itemIconImage.enabled = true;
        }

        // Set Button text
        if (actionButtonText != null)
        {
            actionButtonText.text = isEquipped ? "Unequip" : "Equip";
        }

        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentItem = null;
    }

    private void OnActionClicked()
    {
        if (currentItem != null)
        {
            if (isCurrentlyEquipped)
            {
                PlayerEquipment.Instance.UnequipItem(currentItem.equipSlot);
            }
            else
            {
                currentItem.UseItem(); // This equips it and removes from inventory
            }
            HideTooltip();
            
            // Refresh player stats UI
            PlayerStatsUI statsUI = Object.FindAnyObjectByType<PlayerStatsUI>();
            if (statsUI != null) statsUI.RefreshUI();
        }
    }
}
