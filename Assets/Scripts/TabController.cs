using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    public int ActiveTab;

    // Start is called before the first frame update
    void Start()
    {
        ActivateTab(0);
        ActiveTab = 0;
    }

    public void ActivateTab(int tabNo)
    {
        for(int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabImages[i].color = Color.grey;
        }
        pages[tabNo].SetActive(true);
        tabImages[tabNo].color = Color.white;
        ActiveTab = tabNo;
    }

    void Update()
    {
        if (Keyboard.current != null && (
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.dKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame))
        {
            ActivateTab((ActiveTab + 1) % 6);
        } else if (Keyboard.current != null && (
            Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame))
        {
            ActivateTab((ActiveTab - 1) % 6);
        }
    }
}
