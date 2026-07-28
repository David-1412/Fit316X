using System;
using UnityEngine;

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory
}

public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance { get; private set; }

    [Header("Base Stats")]
    public int baseMaxHp = 5;
    public int baseAttackDamage = 2;
    public float baseCritRate = 0.05f;     // 5% default
    public float baseCritDamage = 1.5f;    // 150% default

    // Currently equipped items
    public EquipmentItem Weapon { get; private set; }
    public EquipmentItem Armor { get; private set; }
    public EquipmentItem Accessory { get; private set; }

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Computed Stats ---
    // Cached stat bonuses
    private int _weaponAttack, _weaponHp, _armorAttack, _armorHp, _accAttack, _accHp;
    private float _weaponCrit, _weaponCritDmg, _armorCrit, _armorCritDmg, _accCrit, _accCritDmg;

    public int AttackDamage => baseAttackDamage + _weaponAttack + _armorAttack + _accAttack;

    public float CritRate => baseCritRate + _weaponCrit + _armorCrit + _accCrit;

    public float CritDamage => baseCritDamage + _weaponCritDmg + _armorCritDmg + _accCritDmg;

    public int MaxHp => baseMaxHp + _weaponHp + _armorHp + _accHp;

    public void EquipItem(EquipmentItem newItem)
    {
        EquipmentItem oldItem = null;

        switch (newItem.equipSlot)
        {
            case EquipmentSlot.Weapon:
                oldItem = Weapon;
                Weapon = newItem;
                _weaponAttack = newItem.attackBonus;
                _weaponHp = newItem.hpBonus;
                _weaponCrit = newItem.critRateBonus;
                _weaponCritDmg = newItem.critDamageBonus;
                break;
            case EquipmentSlot.Armor:
                oldItem = Armor;
                Armor = newItem;
                _armorAttack = newItem.attackBonus;
                _armorHp = newItem.hpBonus;
                _armorCrit = newItem.critRateBonus;
                _armorCritDmg = newItem.critDamageBonus;
                break;
            case EquipmentSlot.Accessory:
                oldItem = Accessory;
                Accessory = newItem;
                break;
        }

        // Add the old item back to the inventory
        if (oldItem != null && InventoryController.EquipInstance != null)
        {
            oldItem.gameObject.SetActive(true); // Ensure it's visible before cloning
            InventoryController.EquipInstance.AddItem(oldItem.gameObject);
            Destroy(oldItem.gameObject); // Cleanup the hidden clone
        }

        // Cap HP if we removed +HP armor
        if (GridTurnPlayer.Instance != null && GridTurnPlayer.Instance.CurrentHp > MaxHp)
        {
            GridTurnPlayer.Instance.SetCurrentHp(MaxHp);
        }

        OnEquipmentChanged?.Invoke();
    }

    public void UnequipItem(EquipmentSlot slot)
    {
        EquipmentItem oldItem = null;
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                oldItem = Weapon;
                Weapon = null;
                _weaponAttack = _weaponHp = 0;
                _weaponCrit = _weaponCritDmg = 0;
                break;
            case EquipmentSlot.Armor:
                oldItem = Armor;
                Armor = null;
                _armorAttack = _armorHp = 0;
                _armorCrit = _armorCritDmg = 0;
                break;
            case EquipmentSlot.Accessory:
                oldItem = Accessory;
                Accessory = null;
                _accAttack = _accHp = 0;
                _accCrit = _accCritDmg = 0;
                break;
        }

        if (oldItem != null && InventoryController.EquipInstance != null)
        {
            oldItem.gameObject.SetActive(true); // Ensure it's visible before cloning
            InventoryController.EquipInstance.AddItem(oldItem.gameObject);
            Destroy(oldItem.gameObject); // Cleanup the hidden clone
        }

        if (GridTurnPlayer.Instance != null && GridTurnPlayer.Instance.CurrentHp > MaxHp)
        {
            GridTurnPlayer.Instance.SetCurrentHp(MaxHp);
        }

        OnEquipmentChanged?.Invoke();
    }
}
