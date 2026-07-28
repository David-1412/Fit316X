using UnityEngine;
using System;

public class DungeonSwitch : MonoBehaviour, IInteractable
{
    public Sprite pressedSprite;
    public Action OnSwitchActivated;
    
    [Header("Checkpoint System")]
    public bool savesGameOnActivate = false;

    private bool isPressed = false;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact()
    {
        if (isPressed) return;

        isPressed = true;
        if (spriteRenderer != null && pressedSprite != null)
        {
            spriteRenderer.sprite = pressedSprite;
        }

        if (savesGameOnActivate && SaveController.Instance != null)
        {
            SaveController.Instance.SaveGame();
            DungeonHUD.Instance?.ShowCombatLog("<color=#00FFFF>Game Saved at Switch!</color>");
        }

        OnSwitchActivated?.Invoke();
    }

    public bool CanInteract()
    {
        return !isPressed;
    }
}