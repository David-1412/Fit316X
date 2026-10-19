using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives the Tab menu one consistent look on every floor (each floor has its own copy
/// of the menu, so this is applied in code when the menu starts):
///   - the unused MAP tab is hidden and the remaining tabs share the width evenly,
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

    /// <summary>Returns the tab indexes that were hidden (the MAP tab).</summary>
    public static HashSet<int> Apply(TabController tabs)
    {
        var hidden = new HashSet<int>();
        if (tabs == null || tabs.tabImages == null || tabs.pages == null) return hidden;

        Sprite parchment = FindParchment(tabs.pages);

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

            bool isMap = tab.name.Contains("Map") || (label != null && label.text.Trim().ToUpper() == "MAP");
            if (isMap)
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
        foreach (Image image in page.GetComponentsInChildren<Image>(true))
        {
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
            if (ParchmentPanels.Contains(p.name) || p.name == "InventoryDescription" || p.name == "InventoryContent")
                return true;
        }
        return false;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
}
