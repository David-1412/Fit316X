using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dresses the start menu: dark underground backdrop with a warm glow and drifting
/// specks of light, the game title and tagline, and pixel-font buttons in the same
/// framed style as the Tab menu. Called from MainMenuManager.Start.
/// </summary>
public static class MainMenuStyle
{
    private static readonly Color Backdrop = new Color(0.035f, 0.04f, 0.08f, 1f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.45f, 1f);
    private static readonly Color TitleShadow = new Color(0.25f, 0.13f, 0.04f, 1f);
    private static readonly Color Tagline = new Color(0.85f, 0.82f, 0.74f, 0.85f);

    public static void Apply(MainMenuManager menu)
    {
        if (menu == null) return;
        MenuAssets assets = MenuAssets.Load();
        Transform root = menu.transform;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Backdrop, glow and light motes sit behind everything else
        Transform background = root.Find("Background");
        if (background != null && background.GetComponent<Image>() is Image backImage)
            backImage.color = Backdrop;
        int behindIndex = background != null ? background.GetSiblingIndex() + 1 : 0;

        // Already built (baked with Tools > Apply Menu Style, or applied earlier)
        if (root.Find("Title") == null) BuildBackdropAndTitle(root, assets, behindIndex);
        StyleTagline(root, assets);
        StyleButtons(root, assets);
    }

    private static void BuildBackdropAndTitle(Transform root, MenuAssets assets, int behindIndex)
    {
        RectTransform glow = NewRect("Glow", root, new Vector2(0.5f, 0.68f), new Vector2(1400f, 1400f));
        glow.SetSiblingIndex(behindIndex);
        RawImage glowImage = glow.gameObject.AddComponent<RawImage>();
        glowImage.texture = assets != null && assets.glowTexture != null ? assets.glowTexture : MakeGlowTexture(128);
        glowImage.color = new Color(1f, 0.78f, 0.35f, 0.32f);
        glowImage.raycastTarget = false;

        RectTransform motes = NewRect("LightMotes", root, new Vector2(0.5f, 0.5f), Vector2.zero);
        motes.anchorMin = Vector2.zero;
        motes.anchorMax = Vector2.one;
        motes.sizeDelta = Vector2.zero;
        motes.SetSiblingIndex(behindIndex + 1);
        motes.gameObject.AddComponent<MainMenuMotes>().glow = glowImage;

        // Title with a drop shadow, then the tagline
        string title = assets != null ? assets.gameTitle : "A Light Undertaking";
        TMP_FontAsset font = assets != null ? assets.font : null;
        TMP_Text shadow = NewText("TitleShadow", root, title, 120f, TitleShadow, font, new Vector2(0.5f, 0.72f), new Vector2(1700f, 200f));
        ((RectTransform)shadow.transform).anchoredPosition = new Vector2(6f, -6f);
        NewText("Title", root, title, 120f, Gold, font, new Vector2(0.5f, 0.72f), new Vector2(1700f, 200f));
        NewText("Tagline", root, "", 34f, Tagline, font, new Vector2(0.5f, 0.6f), new Vector2(1400f, 60f));
    }

    /// <summary>Kept up to date every time, so editing MenuAssets' tagline shows on the next run.</summary>
    private static void StyleTagline(Transform root, MenuAssets assets)
    {
        Transform tagline = root.Find("Tagline");
        if (tagline == null) return;

        RectTransform rect = (RectTransform)tagline;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.57f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1500f, 170f);

        TMP_Text text = tagline.GetComponent<TMP_Text>();
        if (text == null) return;
        if (assets != null) text.text = assets.tagline;
        text.fontStyle = FontStyles.Italic;
        text.fontSizeMax = 34f;
        text.fontSizeMin = 20f;
        text.lineSpacing = 25f;
        text.wordSpacing = 45f; // the pixel font's space is very narrow, so words ran together
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void StyleButtons(Transform root, MenuAssets assets)
    {
        // Buttons: stacked in the lower middle, framed pixel style
        Transform container = root.Find("MenuContainer");
        if (container is RectTransform containerRect)
        {
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.3f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(440f, 380f);

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 18f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            foreach (Button button in container.GetComponentsInChildren<Button>(true))
                StyleButton(button, assets, 400f, 72f);
        }

        // Load panel: same framed look
        Transform loadPanel = root.Find("LoadGamePanel");
        if (loadPanel != null)
        {
            if (loadPanel.GetComponent<Image>() is Image panel && assets != null && assets.frameSprite != null)
            {
                panel.sprite = assets.frameSprite;
                panel.type = Image.Type.Sliced;
                panel.color = Color.white;
            }
            foreach (Button button in loadPanel.GetComponentsInChildren<Button>(true))
                StyleButton(button, assets, 240f, 64f);
        }
    }

    private static void StyleButton(Button button, MenuAssets assets, float width, float height)
    {
        Image image = button.GetComponent<Image>();
        if (image != null && assets != null && assets.frameSprite != null)
        {
            image.sprite = assets.frameSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1f, 0.9f, 0.6f, 1f);
        colours.selectedColor = new Color(1f, 0.9f, 0.6f, 1f);
        colours.pressedColor = new Color(0.85f, 0.7f, 0.45f, 1f);
        colours.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
        button.colors = colours;

        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
        ((RectTransform)button.transform).sizeDelta = new Vector2(width, height);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            if (assets != null && assets.font != null) label.font = assets.font;
            label.text = label.text.ToUpper();
            label.color = button.interactable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 34f;
        }
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, float size, Color colour,
                                    TMP_FontAsset font, Vector2 anchor, Vector2 box)
    {
        RectTransform rect = NewRect(name, parent, anchor, box);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.4f;
        label.fontSizeMax = size;
        label.color = colour;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>Soft round glow: bright in the middle, fading to nothing at the edge.</summary>
    public static Texture2D MakeGlowTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Clamp01(new Vector2(x - half, y - half).magnitude / half);
                float a = Mathf.Pow(1f - d, 2.2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        texture.Apply();
        return texture;
    }
}
