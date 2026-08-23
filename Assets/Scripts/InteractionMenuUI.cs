using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InteractionMenuUI : MonoBehaviour
{
    public static InteractionMenuUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel;
    public Transform contentContainer;
    public GameObject buttonPrefab;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private List<InteractionOption> currentOptions = new List<InteractionOption>();
    private List<TMP_Text> optionTexts = new List<TMP_Text>();
    private int selectedIndex = 0;
    private bool isMenuOpen = false;
    private bool justOpened = false; // Blocks input on the exact frame the menu opens

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    public bool IsMenuOpen => isMenuOpen;

    // Called with world pos (ignored for now — panel is screen-fixed for reliability)
    public void ShowMenu(List<InteractionOption> options, Vector3 worldPosition)
    {
        ShowMenu(options);
    }

    public void ShowMenu(List<InteractionOption> options)
    {
        if (options == null || options.Count == 0) return;

        currentOptions = new List<InteractionOption>(options);
        selectedIndex = 0;
        optionTexts.Clear();

        // Destroy old buttons
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // Build new buttons
        for (int i = 0; i < options.Count; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, contentContainer);
            btnObj.SetActive(true);

            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = options[i].Name;
                optionTexts.Add(txt);
            }
        }

        // Force the panel to the center of the screen
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;   // dead centre
        }

        menuPanel.SetActive(true);
        isMenuOpen = true;
        justOpened = true; // Block input THIS frame — E key that opened the menu must not also confirm an option
        PauseController.SetPause(true); // Stop player moving while menu is open

        // Force a layout rebuild so VerticalLayoutGroup sizes correctly
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer as RectTransform);

        UpdateSelectionUI();

        Debug.Log($"<color=green>InteractionMenuUI: Menu opened with {options.Count} options.</color>");
    }

    public void HideMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        isMenuOpen = false;
        PauseController.SetPause(false); // Restore player movement
    }

    private void Update()
    {
        if (!isMenuOpen) return;
        if (Keyboard.current == null) return;

        // Skip ALL input on the frame the menu was opened
        // (the same E keypress that opened it must not also confirm option 0)
        if (justOpened)
        {
            justOpened = false;
            return;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex - 1 + currentOptions.Count) % currentOptions.Count;
            UpdateSelectionUI();
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            selectedIndex = (selectedIndex + 1) % currentOptions.Count;
            UpdateSelectionUI();
        }
        else if (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.eKey.wasPressedThisFrame)
        {
            var action = currentOptions[selectedIndex].OnSelect;

            // Tell the InteractionDetector to cool down so it doesn't re-fire on this same E press
            InteractionDetector detector = Object.FindAnyObjectByType<InteractionDetector>();
            detector?.NotifyInteractionComplete();

            HideMenu();
            action?.Invoke();
        }
        // Escape closes
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HideMenu();
        }
    }

    private void UpdateSelectionUI()
    {
        for (int i = 0; i < optionTexts.Count; i++)
        {
            // Strip old arrow
            string raw = optionTexts[i].text;
            if (raw.StartsWith("> ")) raw = raw.Substring(2);

            if (i == selectedIndex)
            {
                optionTexts[i].text  = "> " + raw;
                optionTexts[i].color = selectedColor;
            }
            else
            {
                optionTexts[i].text  = raw;
                optionTexts[i].color = normalColor;
            }
        }
    }
}
