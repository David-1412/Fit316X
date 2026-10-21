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

    // Tabs switched off by MenuStyle (the unused MAP tab)
    private HashSet<int> hiddenTabs = new HashSet<int>();

    // Start is called before the first frame update
    void Start()
    {
        hiddenTabs = MenuStyle.Apply(this);
        ActivateTab(0);
        ActiveTab = 0;
    }

    public void ActivateTab(int tabNo)
    {
        if (tabNo < 0 || tabNo >= pages.Length || hiddenTabs.Contains(tabNo)) return;

        for(int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabImages[i].color = Color.grey;
        }
        pages[tabNo].SetActive(true);
        tabImages[tabNo].color = Color.white;
        ActiveTab = tabNo;

        // Text added since the menu opened (quests, lore) gets the same readable colours
        MenuStyle.RefreshText(pages[tabNo]);
    }

    void Update()
    {
        if (Keyboard.current != null && (
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.dKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame))
        {
            ActivateTab(NextVisibleTab(1));
        } else if (Keyboard.current != null && (
            Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame))
        {
            ActivateTab(NextVisibleTab(-1));
        }
    }

    /// <summary>
    /// The next tab in <paramref name="step"/> direction in on-screen order (an added Lore
    /// tab sits second on screen but last in the arrays), wrapping round and skipping hidden ones.
    /// </summary>
    private int NextVisibleTab(int step)
    {
        List<int> order = new List<int>();
        for (int i = 0; i < pages.Length; i++)
            if (!hiddenTabs.Contains(i) && tabImages[i] != null) order.Add(i);
        if (order.Count == 0) return ActiveTab;
        order.Sort((a, b) => tabImages[a].transform.GetSiblingIndex().CompareTo(tabImages[b].transform.GetSiblingIndex()));

        int position = Mathf.Max(0, order.IndexOf(ActiveTab));
        return order[(position + step + order.Count) % order.Count];
    }
}
