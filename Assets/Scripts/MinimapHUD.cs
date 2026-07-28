using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a small always-visible minimap HUD in the top-left corner.
/// The map content (mapParent + playerIcon) lives inside a RectMask2D panel
/// so only the minimap window area is visible at all times.
/// </summary>
public class MinimapHUD : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent that holds all generated map area images")]
    public RectTransform mapParent;

    [Tooltip("The player icon shown on the minimap")]
    public RectTransform playerIcon;

    [Header("Colours")]
    public Color defaultColour = Color.gray;
    public Color currentAreaColor = Color.green;

    [Header("Map Settings")]
    public GameObject mapBounds;
    public PolygonCollider2D initialArea;
    public float mapScale = 10f;

    [Header("Minimap Area Prefab")]
    public GameObject areaPrefab;

    // ---------------------------------------------------------------
    private PolygonCollider2D[] mapAreas;
    private Dictionary<string, RectTransform> uiAreas = new Dictionary<string, RectTransform>();

    public static MinimapHUD Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mapBounds != null)
            mapAreas = mapBounds.GetComponentsInChildren<PolygonCollider2D>();
    }

    private void Start()
    {
        GenerateMap();
    }

    // ---------------------------------------------------------------
    /// <summary>Build / rebuild the minimap for the given area.</summary>
    public void GenerateMap(PolygonCollider2D newCurrentArea = null)
    {
        if (mapAreas == null) return;
        if (areaPrefab == null)
        {
            Debug.LogWarning("[MinimapHUD] areaPrefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        PolygonCollider2D currentArea = newCurrentArea != null ? newCurrentArea : initialArea;

        ClearMap();

        foreach (PolygonCollider2D area in mapAreas)
        {
            CreateAreaUI(area, area == currentArea);
        }

        if (currentArea != null)
            MovePlayerIcon(currentArea.name);
    }

    private void ClearMap()
    {
        foreach (Transform child in mapParent)
            Destroy(child.gameObject);
        uiAreas.Clear();
    }

    private void CreateAreaUI(PolygonCollider2D area, bool isCurrent)
    {
        GameObject areaImage = Instantiate(areaPrefab, mapParent);
        RectTransform rt = areaImage.GetComponent<RectTransform>();

        Bounds bounds = area.bounds;
        rt.sizeDelta = new Vector2(bounds.size.x * mapScale, bounds.size.y * mapScale);
        rt.anchoredPosition = new Vector2(bounds.center.x * mapScale, bounds.center.y * mapScale);

        areaImage.GetComponent<Image>().color = isCurrent ? currentAreaColor : defaultColour;
        uiAreas[area.name] = rt;
    }

    /// <summary>Call this when the player enters a new map area.</summary>
    public void UpdateCurrentArea(string newCurrentArea)
    {
        foreach (KeyValuePair<string, RectTransform> area in uiAreas)
        {
            area.Value.GetComponent<Image>().color =
                area.Key == newCurrentArea ? currentAreaColor : defaultColour;
        }
        MovePlayerIcon(newCurrentArea);
    }

    private void MovePlayerIcon(string newCurrentArea)
    {
        if (uiAreas.TryGetValue(newCurrentArea, out RectTransform areaUI))
        {
            playerIcon.anchoredPosition = areaUI.anchoredPosition;
        }
    }
}
