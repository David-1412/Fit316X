using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Shared on-screen dialogue box, built entirely in code so it needs no scene setup.
/// Used by NPC conversations (Floor 1) and the Floor 5 rooftop cutscene so both look
/// and sound the same: a bottom panel with portrait, gold speaker name, typewriter text
/// with a voice blip per letter, a continue prompt, and optional choice buttons.
///
/// Usage:
///   DialogueBox box = DialogueBox.Instance;      // created on first use
///   box.Open("Old Man", portrait);
///   yield return box.TypeLine(line, 0.05f, voiceClip, 0.5f);
///   box.Close();
/// </summary>
public class DialogueBox : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Singleton (one per scene, created on demand)
    // ------------------------------------------------------------------

    private static DialogueBox s_Instance;

    public static DialogueBox Instance
    {
        get
        {
            if (s_Instance == null)
            {
                GameObject host = new GameObject("DialogueBox (auto)");
                s_Instance = host.AddComponent<DialogueBox>();
            }
            return s_Instance;
        }
    }

    // ------------------------------------------------------------------
    // Look
    // ------------------------------------------------------------------

    private static readonly Color PanelColour = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    private static readonly Color ButtonColour = new Color(0.16f, 0.16f, 0.2f, 1f);
    private static readonly Color ButtonHighlight = new Color(0.35f, 0.3f, 0.18f, 1f);
    public static readonly Color AccentColour = new Color(1f, 0.85f, 0.45f, 1f);

    private const float PanelHeight = 300f;
    private const float PortraitSize = 220f;

    // ------------------------------------------------------------------
    // Built UI
    // ------------------------------------------------------------------

    private GameObject panel;
    private Image portraitImage;
    private RectTransform textArea;
    private TMP_Text nameText;
    private TMP_Text bodyText;
    private GameObject continuePrompt;
    private RectTransform choiceContainer;
    private readonly List<Button> choiceButtons = new List<Button>();

    // Fallback voice player when no SoundEffectManager is loaded
    private AudioSource fallbackVoiceSource;

    /// <summary>True while the box is on screen.</summary>
    public bool IsOpen => panel != null && panel.activeSelf;

    /// <summary>True while choice buttons are waiting for a pick.</summary>
    public bool IsShowingChoices => choiceButtons.Count > 0;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;

        BuildUI();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Shows the box with the speaker's name and (optional) portrait.</summary>
    public void Open(string speakerName, Sprite portrait)
    {
        EnsureEventSystem();

        nameText.text = speakerName;
        bodyText.text = string.Empty;
        continuePrompt.SetActive(false);
        ClearChoices();

        // Without a portrait the text uses the full width of the panel
        bool hasPortrait = portrait != null;
        portraitImage.gameObject.SetActive(hasPortrait);
        portraitImage.sprite = portrait;
        textArea.offsetMin = new Vector2(hasPortrait ? PortraitSize + 70f : 40f, textArea.offsetMin.y);

        panel.SetActive(true);
    }

    /// <summary>Hides the box and clears its contents.</summary>
    public void Close()
    {
        ClearChoices();
        if (bodyText != null) bodyText.text = string.Empty;
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Types <paramref name="line"/> letter by letter, playing <paramref name="voice"/> on
    /// each letter. Pressing continue while typing shows the rest of the line at once.
    /// </summary>
    /// <param name="secondsPerCharacter">Delay between letters (NPCDialogue.typingSpeed).</param>
    public IEnumerator TypeLine(string line, float secondsPerCharacter, AudioClip voice, float voicePitch)
    {
        continuePrompt.SetActive(false);

        // TMP reveal keeps word wrapping stable while typing
        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;

        yield return null; // ignore the key press that started this line

        for (int i = 1; i <= total; i++)
        {
            if (AdvancePressed()) break;

            bodyText.maxVisibleCharacters = i;
            PlayVoice(voice, voicePitch);

            if (secondsPerCharacter > 0f) yield return new WaitForSeconds(secondsPerCharacter);
            else yield return null;
        }

        ShowFullLine();
    }

    /// <summary>Reveals the whole current line and shows the continue prompt.</summary>
    public void ShowFullLine()
    {
        bodyText.maxVisibleCharacters = int.MaxValue;
        continuePrompt.SetActive(!IsShowingChoices);
    }

    /// <summary>Waits for the player to press continue.</summary>
    public IEnumerator WaitForContinue()
    {
        continuePrompt.SetActive(true);
        yield return null; // don't reuse the press that finished typing
        yield return new WaitUntil(AdvancePressed);
        continuePrompt.SetActive(false);
        yield return null;
    }

    /// <summary>Shows a button for each label; <paramref name="onPick"/> gets the chosen index.</summary>
    public void ShowChoices(IList<string> labels, Action<int> onPick)
    {
        ClearChoices();
        continuePrompt.SetActive(false);

        for (int i = 0; i < labels.Count; i++)
        {
            int index = i;
            Button button = CreateChoiceButton(labels[i]);
            button.onClick.AddListener(() =>
            {
                ClearChoices();
                onPick?.Invoke(index);
            });
            choiceButtons.Add(button);
        }

        // Up/down keys cycle through the buttons
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            Navigation nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = choiceButtons[(i - 1 + choiceButtons.Count) % choiceButtons.Count],
                selectOnDown = choiceButtons[(i + 1) % choiceButtons.Count]
            };
            choiceButtons[i].navigation = nav;
        }

        if (choiceButtons.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
        }
    }

    public void ClearChoices()
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        choiceButtons.Clear();
    }

    /// <summary>Space, Enter, E or left click advances dialogue.</summary>
    public static bool AdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.enterKey.wasPressedThisFrame ||
             keyboard.eKey.wasPressedThisFrame))
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // ------------------------------------------------------------------
    // Voice
    // ------------------------------------------------------------------

    private void PlayVoice(AudioClip voice, float pitch)
    {
        if (voice == null) return;

        // Use the game's voice channel so the SFX volume slider applies
        if (SoundEffectManager.CanPlayVoice)
        {
            SoundEffectManager.PlayVoice(voice, pitch);
            return;
        }

        if (fallbackVoiceSource == null)
        {
            fallbackVoiceSource = gameObject.AddComponent<AudioSource>();
            fallbackVoiceSource.playOnAwake = false;
        }
        fallbackVoiceSource.pitch = pitch;
        fallbackVoiceSource.PlayOneShot(voice);
    }

    // ------------------------------------------------------------------
    // UI construction
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // above the game HUD

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Root that is shown / hidden
        RectTransform root = CreateUIObject("Dialogue", transform);
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel = root.gameObject;

        // Bottom-of-screen box
        RectTransform box = CreateUIObject("Panel", root);
        box.anchorMin = new Vector2(0f, 0f);
        box.anchorMax = new Vector2(1f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.offsetMin = new Vector2(80f, 40f);
        box.offsetMax = new Vector2(-80f, 40f + PanelHeight);
        box.gameObject.AddComponent<Image>().color = PanelColour;

        // Portrait on the left
        RectTransform portraitRect = CreateUIObject("Portrait", box);
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        portraitRect.anchoredPosition = new Vector2(40f, 0f);
        portraitImage = portraitRect.gameObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        // Name + text area to the right of the portrait
        textArea = CreateUIObject("TextArea", box);
        SetRect(textArea, Vector2.zero, Vector2.one, new Vector2(40f, 50f), new Vector2(-40f, -20f));

        nameText = CreateText("SpeakerName", textArea, 40f, AccentColour, TextAlignmentOptions.TopLeft);
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), Vector2.zero);
        nameText.fontStyle = FontStyles.Bold;

        bodyText = CreateText("DialogueText", textArea, 34f, Color.white, TextAlignmentOptions.TopLeft);
        SetRect(bodyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -75f));

        TMP_Text prompt = CreateText("ContinuePrompt", box, 26f, AccentColour, TextAlignmentOptions.BottomRight);
        SetRect(prompt.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-500f, 12f), new Vector2(-30f, 52f));
        prompt.text = "Space / E / Click to continue";
        continuePrompt = prompt.gameObject;

        // Choice buttons stack upwards from just above the box, on the right
        choiceContainer = CreateUIObject("Choices", root);
        choiceContainer.anchorMin = choiceContainer.anchorMax = new Vector2(1f, 0f);
        choiceContainer.pivot = new Vector2(1f, 0f);
        choiceContainer.anchoredPosition = new Vector2(-80f, 40f + PanelHeight + 20f);
        choiceContainer.sizeDelta = new Vector2(620f, 0f);

        VerticalLayoutGroup layout = choiceContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.LowerRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = choiceContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Button CreateChoiceButton(string label)
    {
        RectTransform rect = CreateUIObject("Choice", choiceContainer);
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 90f;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white; // tinted by the Button's ColorBlock

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colours = button.colors;
        colours.normalColor = ButtonColour;
        colours.highlightedColor = ButtonHighlight;
        colours.selectedColor = ButtonHighlight;
        colours.pressedColor = AccentColour;
        colours.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        button.colors = colours;

        TMP_Text text = CreateText("Label", rect, 34f, Color.white, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
        text.text = label;

        return button;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem (auto)");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    // ------------------------------------------------------------------
    // Shared UI helpers (also used by FinalRooftopSceneManager)
    // ------------------------------------------------------------------

    public static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static TMP_Text CreateText(string name, Transform parent, float size, Color colour, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = colour;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
