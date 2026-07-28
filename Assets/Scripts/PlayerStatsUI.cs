using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Stats Texts")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI critRateText;
    public TextMeshProUGUI critDamageText;

    [Header("Equipment Slots UI (Buttons)")]
    public Button weaponSlotBtn;
    public Button armorSlotBtn;
    public Button accessorySlotBtn;

    [Header("Equipment Slot Icons (Images)")]
    public Image weaponIcon;
    public Image armorIcon;
    public Image accessoryIcon;

    private void OnEnable()
    {
        if (PlayerEquipment.Instance != null)
        {
            PlayerEquipment.Instance.OnEquipmentChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (PlayerEquipment.Instance != null)
        {
            PlayerEquipment.Instance.OnEquipmentChanged -= RefreshUI;
        }
    }

    private void Start()
    {
        // Try to auto-repair missing weapon slot
        if (weaponSlotBtn == null)
        {
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string lowerName = btn.name.ToLower();
                if (lowerName.Contains("weapon") || lowerName.Contains("attack"))
                {
                    weaponSlotBtn = btn;
                    Debug.Log($"<color=#00FF00>Auto-Repaired Weapon Slot Button by finding: {btn.name}</color>");
                    break;
                }
            }
        }

        // Globally strip raycastTargets from all text in the UI to prevent overlapping bounding boxes
        foreach (var txt in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            txt.raycastTarget = false;
        }

        // Setup unequip buttons
        if (weaponSlotBtn != null) 
        {
            weaponSlotBtn.interactable = true;
            var bg = weaponSlotBtn.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;
            
            // Clean up children that might block the left side (like text labels)
            foreach (var graphic in weaponSlotBtn.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                if (graphic.gameObject != weaponSlotBtn.gameObject) graphic.raycastTarget = false;
            }
            
            weaponSlotBtn.onClick.AddListener(() => ShowEquipmentTooltip(EquipmentSlot.Weapon));
        }
        else
        {
            Debug.LogError("CRITICAL ERROR: 'weaponSlotBtn' is NOT assigned and could not be auto-found! Please drag it manually in the inspector.");
        }

        if (armorSlotBtn != null) armorSlotBtn.onClick.AddListener(() => ShowEquipmentTooltip(EquipmentSlot.Armor));
        if (accessorySlotBtn != null) accessorySlotBtn.onClick.AddListener(() => ShowEquipmentTooltip(EquipmentSlot.Accessory));

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (PlayerEquipment.Instance == null) return;

        // Update Stats Text
        if (hpText != null) hpText.text = $"Max HP: {PlayerEquipment.Instance.MaxHp}";
        if (attackText != null) attackText.text = $"Attack: {PlayerEquipment.Instance.AttackDamage}";
        if (critRateText != null) critRateText.text = $"Crit Rate: {PlayerEquipment.Instance.CritRate * 100f}%";
        if (critDamageText != null) critDamageText.text = $"Crit Damage: {PlayerEquipment.Instance.CritDamage * 100f}%";

        // Update Icons
        UpdateSlotIcon(weaponIcon, PlayerEquipment.Instance.Weapon);
        UpdateSlotIcon(armorIcon, PlayerEquipment.Instance.Armor);
        UpdateSlotIcon(accessoryIcon, PlayerEquipment.Instance.Accessory);
    }

    private void UpdateSlotIcon(Image iconTarget, EquipmentItem item)
    {
        if (iconTarget == null) return;

        if (item != null)
        {
            iconTarget.enabled = true;
            iconTarget.raycastTarget = false; // Prevent the icon from blocking button clicks!
            
            // Get the image component off the item prefab to get its sprite
            var img = item.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                iconTarget.sprite = img.sprite;
            }
            else
            {
                var sr = item.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    iconTarget.sprite = sr.sprite;
            }
        }
        else
        {
            // Empty slot
            iconTarget.sprite = null;
            iconTarget.enabled = false;
        }
    }

    private void Unequip(EquipmentSlot slot)
    {
        if (PlayerEquipment.Instance != null)
        {
            PlayerEquipment.Instance.UnequipItem(slot);
        }
        RefreshUI();
    }

    private void ShowEquipmentTooltip(EquipmentSlot slot)
    {
        Debug.Log($"Clicked Equipment Slot: {slot}");
        
        if (PlayerEquipment.Instance == null)
        {
            Debug.LogWarning("PlayerEquipment.Instance is null! Cannot show tooltip.");
            return;
        }
        if (EquipmentTooltipPanel.Instance == null)
        {
            Debug.LogWarning("EquipmentTooltipPanel.Instance is null! Did you generate the UI and leave it active in the scene?");
            return;
        }

        EquipmentItem item = null;
        switch (slot)
        {
            case EquipmentSlot.Weapon: item = PlayerEquipment.Instance.Weapon; break;
            case EquipmentSlot.Armor: item = PlayerEquipment.Instance.Armor; break;
            case EquipmentSlot.Accessory: item = PlayerEquipment.Instance.Accessory; break;
        }

        if (item != null)
        {
            Debug.Log($"Showing tooltip for equipped item: {item.Name}");
            EquipmentTooltipPanel.Instance.ShowTooltip(item, true); // true = isEquipped
        }
        else
        {
            Debug.Log($"No item equipped in {slot} slot.");
        }
    }
}
