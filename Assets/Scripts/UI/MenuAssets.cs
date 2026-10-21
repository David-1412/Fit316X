using TMPro;
using UnityEngine;

/// <summary>
/// Art shared by the code-built menu styling (MenuStyle, MainMenuStyle). The asset lives at
/// Resources/MenuAssets so it can be loaded from any scene without wiring it up.
/// </summary>
[CreateAssetMenu(fileName = "MenuAssets", menuName = "UI/Menu Assets")]
public class MenuAssets : ScriptableObject
{
    [Tooltip("Pixel font used by the Tab menu.")]
    public TMP_FontAsset font;
    [Tooltip("Dark blue framed panel (Tab menu pages and tabs).")]
    public Sprite frameSprite;
    [Tooltip("Parchment panel (Inventory details).")]
    public Sprite parchmentSprite;
    [Tooltip("The Lore page, added to floors whose menu doesn't have one.")]
    public GameObject lorePagePrefab;
    [Tooltip("Clear font for longer text (Lore entries), replacing the hard-to-read display font.")]
    public TMP_FontAsset bodyFont;
    [Tooltip("Current main character, shown on the Player page.")]
    public Sprite playerPortrait;
    [Tooltip("Soft glow behind the start menu title (made by Tools > Apply Menu Style).")]
    public Texture2D glowTexture;

    [Header("Main menu")]
    public string gameTitle = "A Light Undertaking";
    [TextArea(2, 4)]
    public string tagline = "The sun is gone, or so they say.\nBeneath the earth, the people wait in the dark.\nClimb the tower and uncover the truth.";

    private static MenuAssets s_Loaded;
    public static MenuAssets Load()
    {
        if (s_Loaded == null) s_Loaded = Resources.Load<MenuAssets>("MenuAssets");
        return s_Loaded;
    }
}
