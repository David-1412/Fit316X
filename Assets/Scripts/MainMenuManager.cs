using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private string saveFilePath;
    private string saveScreenPath;

    void Start()
    {
        saveFilePath = Application.persistentDataPath + "/saveData.json";
        saveScreenPath = Application.persistentDataPath + "/saveScreen.png";

        bool saveExists = File.Exists(saveFilePath);

        continueButton.interactable = saveExists;
        loadGameButton.interactable = saveExists;

        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);

        newGameButton.onClick.AddListener(OnNewGameClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        loadGameButton.onClick.AddListener(OnLoadGameClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        confirmLoadButton.onClick.AddListener(OnConfirmLoadClicked);
        closeLoadPanelButton.onClick.AddListener(() => loadGamePanel.SetActive(false));
    }

    private void OnNewGameClicked()
    {
        // Delete save data to start fresh
        if (File.Exists(saveFilePath))
            File.Delete(saveFilePath);
        
        if (File.Exists(saveScreenPath))
            File.Delete(saveScreenPath);

        SceneManager.LoadScene("SampleScene");
    }

    private void OnContinueClicked()
    {
        // SaveController auto-loads if file exists
        SceneManager.LoadScene("SampleScene");
    }

    private void OnLoadGameClicked()
    {
        loadGamePanel.SetActive(true);

        if (File.Exists(saveScreenPath))
        {
            byte[] fileData = File.ReadAllBytes(saveScreenPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            savePreviewImage.texture = tex;
        }
        else
        {
            savePreviewImage.texture = null;
        }
    }

    private void OnConfirmLoadClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}