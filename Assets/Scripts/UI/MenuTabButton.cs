using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Opens one Tab menu page when clicked. Used on tabs added by MenuStyle (e.g. Lore).</summary>
public class MenuTabButton : MonoBehaviour, IPointerClickHandler
{
    public TabController tabs;
    public int index;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tabs == null) tabs = GetComponentInParent<TabController>(true);
        if (tabs != null) tabs.ActivateTab(index);
    }
}
