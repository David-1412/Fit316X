using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a short message in the lower middle of the screen that fades out,
/// e.g. "Missing required item". Builds its own overlay canvas, so it works
/// in any scene without setup. Call PopupText.Show("...").
/// </summary>
public class PopupText : MonoBehaviour
{
    private const float HoldSeconds = 1.6f;
    private const float FadeSeconds = 0.5f;

    private static PopupText s_Instance;

    private TextMeshProUGUI label;
    private CanvasGroup group;
    private Coroutine routine;

    public static void Show(string message)
    {
        if (s_Instance == null) s_Instance = Create();
        s_Instance.Display(message);
    }

    private static PopupText Create()
    {
        GameObject canvasObject = new GameObject("PopupText (auto)");
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 2; // above the HUD, below fades and ending screens

        UnityEngine.UI.CanvasScaler scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        PopupText popup = canvasObject.AddComponent<PopupText>();
        popup.group = canvasObject.AddComponent<CanvasGroup>();
        popup.group.alpha = 0f;
        popup.group.blocksRaycasts = false;
        popup.group.interactable = false;

        // Dark box behind the text so it reads on any background
        GameObject boxObject = new GameObject("Box", typeof(RectTransform));
        boxObject.transform.SetParent(canvasObject.transform, false);
        RectTransform box = (RectTransform)boxObject.transform;
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.22f);
        box.sizeDelta = new Vector2(760f, 96f);
        UnityEngine.UI.Image background = boxObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Message", typeof(RectTransform));
        textObject.transform.SetParent(box, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 8f);
        rect.offsetMax = new Vector2(-24f, -8f);

        popup.label = textObject.AddComponent<TextMeshProUGUI>();
        popup.label.fontSize = 44;
        popup.label.fontStyle = FontStyles.Bold;
        popup.label.alignment = TextAlignmentOptions.Center;
        popup.label.color = new Color(1f, 0.4f, 0.35f, 1f);
        popup.label.enableAutoSizing = true;
        popup.label.fontSizeMin = 24;
        popup.label.fontSizeMax = 44;
        popup.label.raycastTarget = false;
        return popup;
    }

    private void Display(string message)
    {
        label.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // Unscaled time so it still fades while the game is paused
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(HoldSeconds);

        float t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
            yield return null;
        }
        group.alpha = 0f;
        routine = null;
    }

    private void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
    }
}
