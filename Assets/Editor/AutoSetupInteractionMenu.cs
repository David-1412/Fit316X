using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AutoSetupInteractionMenu
{
    [MenuItem("Tools/Fix & Rebuild Interaction Menu")]
    public static void Setup()
    {
        // ---- 1. Find the UICanvas (your game's main canvas) ----
        Canvas targetCanvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name == "UICanvas" || c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }
        if (targetCanvas == null)
        {
            Debug.LogError("Could not find a Canvas! Create one first.");
            return;
        }

        // ---- 2. Destroy old broken objects ----
        GameObject oldManager = GameObject.Find("InteractionMenuManager");
        if (oldManager != null)
        {
            Object.DestroyImmediate(oldManager);
            Debug.Log("Removed old floating InteractionMenuManager.");
        }

        GameObject oldPanel = GameObject.Find("InteractionMenuPanel");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel);
            Debug.Log("Removed old InteractionMenuPanel.");
        }

        GameObject oldTemplate = GameObject.Find("InteractionButtonTemplate");
        if (oldTemplate != null)
        {
            Object.DestroyImmediate(oldTemplate);
            Debug.Log("Removed old InteractionButtonTemplate.");
        }

        // ---- 3. Create the Menu Panel INSIDE the UICanvas ----
        GameObject panelObj = new GameObject("InteractionMenuPanel");
        panelObj.transform.SetParent(targetCanvas.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin       = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax       = new Vector2(0.5f, 0.5f);
        panelRT.pivot           = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta       = new Vector2(280, 220);

        // Outline for legibility
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor    = new Color(0.8f, 0.8f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        // ---- 4. Title ----
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text      = "[ Choose Action ]";
        titleText.fontSize  = 18;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color     = new Color(1f, 0.9f, 0.2f);

        RectTransform titleRT   = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0, 1);
        titleRT.anchorMax       = new Vector2(1, 1);
        titleRT.pivot           = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -8);
        titleRT.sizeDelta       = new Vector2(0, 30);

        // ---- 5. Content Container ----
        GameObject contentObj = new GameObject("ContentContainer");
        contentObj.transform.SetParent(panelObj.transform, false);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset(12, 12, 8, 8);
        vlg.spacing              = 4;
        vlg.childAlignment       = TextAnchor.UpperLeft;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform contentRT   = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin       = new Vector2(0, 0);
        contentRT.anchorMax       = new Vector2(1, 1);
        contentRT.offsetMin       = new Vector2(0, 0);
        contentRT.offsetMax       = new Vector2(0, -42); // below title

        // ---- 6. Button Template (hidden) ----
        GameObject buttonObj = new GameObject("InteractionButtonTemplate");
        buttonObj.transform.SetParent(targetCanvas.transform, false);
        buttonObj.SetActive(false); // Hidden - used as a prefab

        Image btnImg   = buttonObj.AddComponent<Image>();
        btnImg.color   = new Color(0.15f, 0.15f, 0.15f, 0.0f); // transparent bg

        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.minHeight      = 34;
        le.preferredHeight = 34;

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        TMP_Text btnText   = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text       = "Option";
        btnText.fontSize   = 20;
        btnText.alignment  = TextAlignmentOptions.Left;
        btnText.color      = Color.white;

        RectTransform btnTextRT   = btnTextObj.GetComponent<RectTransform>();
        btnTextRT.anchorMin       = Vector2.zero;
        btnTextRT.anchorMax       = Vector2.one;
        btnTextRT.offsetMin       = new Vector2(8, 0);
        btnTextRT.offsetMax       = Vector2.zero;

        // ---- 7. Create the Manager object INSIDE the UICanvas ----
        GameObject managerObj = new GameObject("InteractionMenuManager");
        managerObj.transform.SetParent(targetCanvas.transform, false);

        InteractionMenuUI uiScript = managerObj.AddComponent<InteractionMenuUI>();
        uiScript.menuPanel        = panelObj;
        uiScript.contentContainer = contentObj.transform;
        uiScript.buttonPrefab     = buttonObj;
        uiScript.normalColor      = Color.white;
        uiScript.selectedColor    = new Color(1f, 0.9f, 0.2f);

        // ---- 8. Hide the panel by default ----
        panelObj.SetActive(false);

        Undo.RegisterCreatedObjectUndo(managerObj, "Rebuild Interaction Menu");

        Debug.Log("<color=#00FF00>SUCCESS: Interaction Menu rebuilt inside UICanvas!</color>");
        Debug.Log($"<color=#00FF00>Parent canvas: {targetCanvas.name}</color>");
    }
}
