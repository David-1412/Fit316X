using UnityEngine;
using UnityEngine.UI;

/// <summary>Makes the Button it sits on save and return to the start menu (Settings page).</summary>
[RequireComponent(typeof(Button))]
public class MainMenuButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ReturnToMainMenu.Go);
    }
}
