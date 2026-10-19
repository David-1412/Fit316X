using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupUIController : MonoBehaviour
{
    private static ItemPickupUIController s_Instance;

    /// <summary>
    /// The live popup controller, or null. A destroyed one (e.g. from a canvas removed
    /// on a scene change) is never returned, so "Instance?.ShowItemPickup" stays safe.
    /// </summary>
    public static ItemPickupUIController Instance
    {
        get
        {
            if (s_Instance == null) // Unity's null check: also true once destroyed
                s_Instance = FindAnyObjectByType<ItemPickupUIController>();
            return s_Instance;
        }
    }

    public GameObject popupPrefab;
    public int maxPopups = 5;
    public float popupDuration = 3f;

    private readonly Queue<GameObject> activePopups = new();

    private void Awake()
    {
        if (s_Instance == null || s_Instance == this)
        {
            s_Instance = this;
        }
        else
        {
            // A persistent instance already exists from a previous scene load — silently destroy this duplicate.
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
    }

    public void ShowItemPickup(string itemName, Sprite itemIcon)
    {
        GameObject newPopup = Instantiate(popupPrefab, transform);
        newPopup.GetComponentInChildren<TMP_Text>().text = itemName;

        Image itemImage = newPopup.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemImage)
        {
            itemImage.sprite = itemIcon;
            itemImage.enabled = itemIcon != null; // text-only messages have no icon
        }

        activePopups.Enqueue(newPopup);
        if(activePopups.Count > maxPopups)
        {
            Destroy(activePopups.Dequeue());
        }

        //Fade out and destory
        StartCoroutine(FadeOutAndDestroy(newPopup));
    }

    private IEnumerator FadeOutAndDestroy(GameObject popup)
    {
        yield return new WaitForSeconds(popupDuration);
        if (popup == null) yield break;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        for(float timePassed = 0f; timePassed < 1f; timePassed += Time.deltaTime)
        {
            if (popup == null) yield break;
            canvasGroup.alpha = 1f - timePassed;
            yield return null;
        }

        Destroy(popup);
    }
}
