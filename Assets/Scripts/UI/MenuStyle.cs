using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gives the Tab menu one consistent look on every floor (each floor has its own copy
/// of the menu, so this is applied in code when the menu starts):
///   - the unused MAP and STORY tabs are hidden and the remaining tabs share the width evenly,
///   - floors whose menu predates the Lore tab get one (built from the Lore page prefab),
///   - the Settings page gets a "Main Menu" button next to Save,
///   - every tab uses the same button art and its label fits inside it,
///   - the Lore (and Story) pages use the same parchment panels as the Inventory page,
///   - text on parchment is dark brown, text on the blue frame is white, and small text is enlarged.
/// </summary>
public static class MenuStyle
{
    private static readonly Color InkColour = new Color(0.23f, 0.14f, 0.08f, 1f);
    private static readonly Color DividerColour = new Color(0.55f, 0.38f, 0.24f, 1f);
    private static readonly Color SlotColour = new Color(0.36f, 0.24f, 0.15f, 0.35f);

    private const float MinReadableSize = 26f;

    // Panels inside pages that should look like the Inventory page's parchment
    private static readonly HashSet<string> ParchmentPanels = new HashSet<string>
    {
        "Scroll View", "Scroll View (1)", "LorePanel", "DescriptionPanel", "QuestScrollView"
    };

    // Tabs that aren't used in this game
    private static readonly HashSet<string> HiddenTabLabels = new HashSet<string> { "MAP", "STORY" };

    /// <summary>Returns the tab indexes that were hidden (MAP and STORY).</summary>
    public static HashSet<int> Apply(TabController tabs)
    {
        var hidden = new HashSet<int>();
        if (tabs == null || tabs.tabImages == null || tabs.pages == null) return hidden;

        Sprite parchment = FindParchment(tabs.pages);
        if (parchment == null && MenuAssets.Load() != null) parchment = MenuAssets.Load().parchmentSprite;

        EnsureLoreTab(tabs);
        StyleTabs(tabs, hidden);

        for (int i = 0; i < tabs.pages.Length; i++)
        {
            GameObject page = tabs.pages[i];
            if (page == null || hidden.Contains(i)) continue;
            StylePage(page.transform, parchment);
        }
        return hidden;
    }

    /// <summary>Re-colours text added since the menu was styled (quest entries, lore items...).</summary>
    public static void RefreshText(GameObject page)
    {
        if (page == null) return;
        foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text, IsOnParchment(text.transform, page.transform));
    }

    /// <summary>Sizes an inventory item to sit inside its slot instead of overflowing it.</summary>
    public static void FitItemToSlot(RectTransform item)
    {
        if (item == null) return;
        float cell = 80f;
        GridLayoutGroup grid = item.parent != null && item.parent.parent != null
            ? item.parent.parent.GetComponent<GridLayoutGroup>() : null;
        if (grid != null) cell = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
        else if (item.parent is RectTransform slot && slot.rect.width > 1f) cell = Mathf.Min(slot.rect.width, slot.rect.height);

        item.sizeDelta = Vector2.one * cell * 0.8f;
        Image image = item.GetComponent<Image>();
        if (image != null) image.preserveAspect = true;
    }

    // ------------------------------------------------------------------
    // Tabs
    // ------------------------------------------------------------------

    private static void StyleTabs(TabController tabs, HashSet<int> hidden)
    {
        Image reference = null;
        foreach (Image tab in tabs.tabImages)
            if (tab != null && tab.sprite != null) { reference = tab; break; }

        int visible = 0;
        for (int i = 0; i < tabs.tabImages.Length; i++)
        {
            Image tab = tabs.tabImages[i];
            if (tab == null) continue;
            TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);

            bool unused = tab.name.Contains("Map") || tab.name.Contains("Quest") ||
                          (label != null && HiddenTabLabels.Contains(label.text.Trim().ToUpper()));
            if (unused)
            {
                hidden.Add(i);
                tab.gameObject.SetActive(false);
                if (i < tabs.pages.Length && tabs.pages[i] != null) tabs.pages[i].SetActive(false);
                continue;
            }
            visible++;

            // Same button art on every tab (the Lore tab had none, so it showed as a grey block)
            if (reference != null && tab.sprite == null)
            {
                tab.sprite = reference.sprite;
                tab.type = reference.type;
                tab.pixelsPerUnitMultiplier = reference.pixelsPerUnitMultiplier;
            }

            if (label != null)
            {
                // Fill the tab with a margin and shrink long words (INVENTORY, SETTINGS) to fit
                RectTransform rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(16f, 10f);
                rect.offsetMax = new Vector2(-16f, -10f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 40f;
                label.color = Color.white;
            }
        }

        // Share the bar's width between the remaining tabs
        GridLayoutGroup grid = tabs.GetComponent<GridLayoutGroup>();
        RectTransform bar = tabs.transform as RectTransform;
        if (grid != null && bar != null && visible > 0)
        {
            float width = (bar.rect.width > 1f ? bar.rect.width : grid.cellSize.x * tabs.tabImages.Length)
                          - grid.padding.horizontal - grid.spacing.x * (visible - 1);
            grid.cellSize = new Vector2(width / visible, grid.cellSize.y);
        }
    }

    /// <summary>
    /// Older floor menus (F3-F5) have no Lore tab. Add one after the Inventory tab, using
    /// the same Lore page prefab as F1/F2. It goes at the end of the tab arrays so the
    /// existing tabs' click events (which pass their index) still open the right page.
    /// </summary>
    private static void EnsureLoreTab(TabController tabs)
    {
        for (int i = 0; i < tabs.tabImages.Length; i++)
        {
            Image tab = tabs.tabImages[i];
            if (tab == null) continue;
            TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
            if (tab.name.Contains("Collectable") || (label != null && label.text.Trim().ToUpper() == "LORE")) return;
        }

        MenuAssets assets = MenuAssets.Load();
        if (assets == null || assets.lorePagePrefab == null || tabs.pages.Length == 0) return;

        int reference = 0;
        for (int i = 0; i < tabs.tabImages.Length; i++)
            if (tabs.tabImages[i] != null && tabs.tabImages[i].name.Contains("Inventory")) { reference = i; break; }
        Image referenceTab = tabs.tabImages[reference];
        GameObject referencePage = tabs.pages[reference];
        if (referenceTab == null || referencePage == null) return;

        int index = tabs.pages.Length;

        // Page: same place and size as the other pages
        GameObject page = InstantiateAsset(assets.lorePagePrefab, referencePage.transform.parent);
        page.name = "CollectablesPage";
        if (page.transform is RectTransform pageRect && referencePage.transform is RectTransform refRect)
        {
            pageRect.anchorMin = refRect.anchorMin;
            pageRect.anchorMax = refRect.anchorMax;
            pageRect.pivot = refRect.pivot;
            pageRect.anchoredPosition = refRect.anchoredPosition;
            pageRect.sizeDelta = refRect.sizeDelta;
        }
        page.SetActive(false);

        // Tab: a copy of the Inventory tab, relabelled, placed right after it
        GameObject tabObject = Object.Instantiate(referenceTab.gameObject, referenceTab.transform.parent, false);
        tabObject.name = "CollectablesTab";
        tabObject.transform.SetSiblingIndex(referenceTab.transform.GetSiblingIndex() + 1);
        TMP_Text tabLabel = tabObject.GetComponentInChildren<TMP_Text>(true);
        if (tabLabel != null) tabLabel.text = "LORE";

        // The copied Inventory click event would open the Inventory page; a MenuTabButton opens Lore
        EventTrigger trigger = tabObject.GetComponent<EventTrigger>();
        if (trigger != null) trigger.triggers.Clear();
        Button button = tabObject.GetComponent<Button>();
        if (button != null) button.onClick = new Button.ButtonClickedEvent();
        MenuTabButton tabButton = tabObject.AddComponent<MenuTabButton>();
        tabButton.tabs = tabs;
        tabButton.index = index;

        var tabList = new List<Image>(tabs.tabImages) { tabObject.GetComponent<Image>() };
        var pageList = new List<GameObject>(tabs.pages) { page };
        tabs.tabImages = tabList.ToArray();
        tabs.pages = pageList.ToArray();
    }

    // ------------------------------------------------------------------
    // Settings: Main Menu button
    // ------------------------------------------------------------------

    private static void AddMainMenuButton(Transform page, Sprite parchment)
    {
        Transform saveTransform = FindDeep(page, "SaveButton");
        if (saveTransform == null || FindDeep(page, "MainMenuButton") != null) return;

        Button save = saveTransform.GetComponent<Button>();
        if (save == null) return;
        Button mainMenu = Object.Instantiate(save, saveTransform.parent, false);
        mainMenu.name = "MainMenuButton";
        mainMenu.onClick = new Button.ButtonClickedEvent(); // drop the copied Save call
        mainMenu.gameObject.AddComponent<MainMenuButton>();   // goes to the start menu when clicked
        TMP_Text label = mainMenu.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "MAIN MENU";

        // Save on top, Main Menu below it, both clear of the volume sliders
        PlaceButton(save, new Vector2(0f, 140f), parchment);
        PlaceButton(mainMenu, new Vector2(0f, 30f), parchment);
    }

    private static void PlaceButton(Button button, Vector2 position, Sprite parchment)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 84f);

        Image image = button.GetComponent<Image>();
        if (image != null && parchment != null)
        {
            image.sprite = parchment;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = InkColour;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
            MenuAssets assets = MenuAssets.Load();
            if (assets != null && assets.font != null) label.font = assets.font;
        }
    }

    // ------------------------------------------------------------------
    // Pages
    // ------------------------------------------------------------------

    private static Sprite FindParchment(GameObject[] pages)
    {
        foreach (GameObject page in pages)
        {
            if (page == null) continue;
            foreach (Image image in page.GetComponentsInChildren<Image>(true))
                if (image.name == "InventoryDescription" && image.sprite != null) return image.sprite;
        }
        return null;
    }

    private static void StylePage(Transform page, Sprite parchment)
    {
        MenuAssets assets = MenuAssets.Load();
        foreach (Image image in page.GetComponentsInChildren<Image>(true))
        {
            // The Player page still showed the old character art
            if (assets != null && assets.playerPortrait != null && image.sprite != null
                && image.sprite.texture != null && image.sprite.texture.name == "Idle (3)")
            {
                image.sprite = assets.playerPortrait;
                image.preserveAspect = true;
                continue;
            }

            if (parchment != null && ParchmentPanels.Contains(image.name))
            {
                image.sprite = parchment;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = Color.white;
            }
            // Plain grey divider bars in the Lore panels
            else if (image.name == "Image (1)" && image.sprite == null && IsOnParchment(image.transform, page))
            {
                image.color = DividerColour;
            }
        }

        // Inventory: centre the slot grid, tint the slots to suit the parchment, tidy the details panel
        Transform content = FindDeep(page, "InventoryContent");
        if (content != null)
        {
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.childAlignment = TextAnchor.UpperCenter;
            foreach (Transform slot in content)
            {
                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null && slotImage.sprite == null) slotImage.color = SlotColour;
                foreach (Transform item in slot) FitItemToSlot(item as RectTransform);
            }
        }
        LayoutItemDetails(FindDeep(page, "InventoryDescription"));
        AddMainMenuButton(page, parchment);

        foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text, IsOnParchment(text.transform, page));
    }

    private static void LayoutItemDetails(Transform panel)
    {
        if (panel == null) return;

        if (FindDeep(panel, "ItemName") is RectTransform name)
        {
            name.anchorMin = new Vector2(0f, 1f);
            name.anchorMax = new Vector2(1f, 1f);
            name.pivot = new Vector2(0.5f, 1f);
            name.offsetMin = new Vector2(40f, -130f);
            name.offsetMax = new Vector2(-40f, -50f);
            TMP_Text text = name.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Center;
                text.fontStyle = FontStyles.Bold;
                text.enableAutoSizing = true;
                text.fontSizeMin = 24f;
                text.fontSizeMax = 44f;
            }
        }

        if (FindDeep(panel, "ItemIcon") is RectTransform icon)
        {
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
            icon.pivot = new Vector2(0.5f, 1f);
            icon.anchoredPosition = new Vector2(0f, -150f);
            icon.sizeDelta = new Vector2(150f, 150f);
            Image image = icon.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }

        if (FindDeep(panel, "ItemDesc") is RectTransform desc)
        {
            desc.anchorMin = new Vector2(0f, 0f);
            desc.anchorMax = new Vector2(1f, 1f);
            desc.offsetMin = new Vector2(40f, 40f);
            desc.offsetMax = new Vector2(-40f, -330f);
            TMP_Text text = desc.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.Top;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = 32f;
                text.lineSpacing = 10f;
            }
        }
    }

    private static void StyleText(TMP_Text text, bool onParchment)
    {
        // The Lore page used a narrow digital display font; swap it for a clear one
        MenuAssets assets = MenuAssets.Load();
        if (assets != null && assets.bodyFont != null && text.font != null && text.font.name.Contains("Electronic"))
        {
            text.font = assets.bodyFont;
            text.fontStyle &= ~FontStyles.Bold;
        }

        // Only neutral (white / grey / black) text is recoloured, so coloured highlights stay
        Color.RGBToHSV(text.color, out _, out float saturation, out _);
        if (saturation < 0.2f)
            text.color = new Color(onParchment ? InkColour.r : 1f, onParchment ? InkColour.g : 1f,
                                   onParchment ? InkColour.b : 1f, text.color.a);

        // Small body text grows, but never past what fits in its box
        if (!text.enableAutoSizing && text.fontSize < MinReadableSize)
        {
            text.fontSizeMin = Mathf.Min(text.fontSize, 18f);
            text.fontSizeMax = MinReadableSize + 4f;
            text.enableAutoSizing = true;
        }
    }

    private static bool IsOnParchment(Transform t, Transform page)
    {
        for (Transform p = t; p != null && p != page.parent; p = p.parent)
        {
            if (ParchmentPanels.Contains(p.name) || p.name == "InventoryDescription" || p.name == "InventoryContent"
                || p.name == "SaveButton" || p.name == "MainMenuButton")
                return true;
        }
        return false;
    }

    /// <summary>Instantiates a prefab; when baking in the editor it stays linked to the prefab.</summary>
    private static GameObject InstantiateAsset(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Object.Instantiate(prefab, parent, false);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
}
