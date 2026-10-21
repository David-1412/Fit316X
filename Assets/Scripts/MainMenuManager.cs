using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start menu: New Game, Continue, Load Game (single slot with preview) and Quit.
/// Scene loading and save handling live in SaveController's static methods.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button newGameButton;
    public Button loadGameButton;
    public Button continueButton;
    public Button quitButton;

    [Header("Load Game Sub-Panel")]
    public GameObject loadGamePanel;
    public RawImage savePreviewImage;
    public Button confirmLoadButton;
    public Button closeLoadPanelButton;
    [Tooltip("Optional. Shows the saved floor and time. Created automatically if empty.")]
    public TMP_Text saveInfoText;

    private Texture2D previewTexture;

    void Awake()
    {
        // Coming back from the game (e.g. after an ending): remove the Player, HUD,
        // camera and other objects carried between floors so they don't sit on the menu
        FinalRooftopSceneManager.DestroyCarriedOverObjects();
    }

    void Start()
    {
        // Returning to the menu (e.g. after an ending) must not leave the game paused
        PauseController.SetPause(false);
        Time.timeScale = 1f;

        RefreshButtons();
        MainMenuStyle.Apply(this); // after RefreshButtons so greyed-out buttons get dimmed labels

        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);

        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (confirmLoadButton != null) confirmLoadButton.onClick.AddListener(OnConfirmLoadClicked);
        if (closeLoadPanelButton != null) closeLoadPanelButton.onClick.AddListener(() => loadGamePanel.SetActive(false));
    }

    private void OnDestroy()
    {
        if (previewTexture != null) Destroy(previewTexture);
    }

    private void RefreshButtons()
    {
        bool saveExists = SaveController.HasSave();
        if (continueButton != null) continueButton.interactable = saveExists;
        if (loadGameButton != null) loadGameButton.interactable = saveExists;
    }

    private void OnNewGameClicked()
    {
        SetButtonsInteractable(false);
        SaveController.NewGame();
    }

    private void OnContinueClicked()
    {
        SetButtonsInteractable(false);
        SaveController.ContinueGame();
    }

    private void OnLoadGameClicked()
    {
        if (loadGamePanel == null)
        {
            // No panel in this menu - behave like Continue
            OnContinueClicked();
            return;
        }

        loadGamePanel.SetActive(true);
        ShowPreview();
        ShowSaveInfo(SaveController.ReadSave());
    }

    private void OnConfirmLoadClicked()
    {
        SetButtonsInteractable(false);
        SaveController.ContinueGame();
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (Button button in new[] { newGameButton, continueButton, loadGameButton, quitButton, confirmLoadButton })
        {
            if (button != null) button.interactable = value;
        }
    }

    // ------------------------------------------------------------------
    // Load panel contents
    // ------------------------------------------------------------------

    private void ShowPreview()
    {
        if (savePreviewImage == null) return;

        if (previewTexture != null) Destroy(previewTexture);
        previewTexture = null;

        if (File.Exists(SaveController.SaveScreenPath))
        {
            previewTexture = new Texture2D(2, 2);
            previewTexture.LoadImage(File.ReadAllBytes(SaveController.SaveScreenPath));
        }

        savePreviewImage.texture = previewTexture;
        savePreviewImage.color = previewTexture != null ? Color.white : new Color(0f, 0f, 0f, 0.6f);
    }

    private void ShowSaveInfo(SaveData data)
    {
        TMP_Text label = GetOrCreateInfoText();
        if (label == null) return;

        if (data == null)
        {
            label.text = "No save found";
            return;
        }

        string floor = FloorLabel(data.sceneName);
        string time = DateTime.TryParse(data.saveTime, null, DateTimeStyles.RoundtripKind, out DateTime saved)
            ? saved.ToString("d MMM yyyy, HH:mm")
            : "Unknown time";
        label.text = $"{floor}  \u2022  Saved {time}";
    }

    private static string FloorLabel(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName) && sceneName.Length == 2 && sceneName[0] == 'F' && char.IsDigit(sceneName[1]))
            return $"Floor {sceneName[1]}";
        return string.IsNullOrEmpty(sceneName) ? "Floor 1" : sceneName;
    }

    /// <summary>Uses the assigned label, or adds one under the preview image.</summary>
    private TMP_Text GetOrCreateInfoText()
    {
        if (saveInfoText != null) return saveInfoText;
        if (loadGamePanel == null) return null;

        GameObject go = new GameObject("SaveInfoText", typeof(RectTransform));
        go.transform.SetParent(loadGamePanel.transform, false);
        RectTransform rect = (RectTransform)go.transform;
        // Fits the gap between the preview image and the Confirm / Close buttons
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 36f);
        rect.anchoredPosition = new Vector2(0f, -81f);

        saveInfoText = go.AddComponent<TextMeshProUGUI>();
        saveInfoText.fontSize = 24;
        saveInfoText.alignment = TextAlignmentOptions.Center;
        saveInfoText.color = Color.white;
        saveInfoText.raycastTarget = false;
        return saveInfoText;
    }
}
