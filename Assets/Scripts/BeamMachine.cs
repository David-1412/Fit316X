using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class BeamMachine : MonoBehaviour, IMultiInteractable
{
    [Header("Accepted Gems")]
    public string redGemName = "Red Gem";
    public string blueGemName = "Blue Gem";

    [Header("Visuals")]
    public SpriteRenderer placedItemSprite;
    public Transform nozzleTransform; // The part that rotates
    public LineRenderer lineRenderer;

    [Header("Beam Settings")]
    public float maxBeamDistance = 50f;
    public LayerMask obstacleLayer;
    public Color redBeamColor = Color.red;
    public Color blueBeamColor = Color.cyan;

    private string currentGem = "";
    private int rotationState = 0; // 0=Up, 1=Right, 2=Down, 3=Left
    private bool isMachineOn = false; // Requires explicit turn on

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (placedItemSprite != null) placedItemSprite.sprite = null;
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        // Continuously draw the beam if a gem is placed AND the machine is turned on
        if (!string.IsNullOrEmpty(currentGem) && isMachineOn && lineRenderer != null)
        {
            DrawBeam();
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void DrawBeam()
    {
        lineRenderer.enabled = true;
        lineRenderer.sortingLayerName = "Collision"; // Put beam in front of ground layer
        lineRenderer.sortingOrder = 5000; 
        
        Vector2 direction = Vector2.up;
        if (rotationState == 1) direction = Vector2.right;
        else if (rotationState == 2) direction = Vector2.down;
        else if (rotationState == 3) direction = Vector2.left;

        Vector2 startPos = (Vector2)nozzleTransform.position + (direction * 0.5f);
        lineRenderer.SetPosition(0, startPos);

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxBeamDistance, obstacleLayer);
        
        if (hit.collider != null)
            lineRenderer.SetPosition(1, hit.point);
        else
            lineRenderer.SetPosition(1, startPos + (direction * maxBeamDistance));
    }

    private void SetBeamColor(Color color)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = color;
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.5f);
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        List<InteractionOption> options = GetInteractionOptions();
        if (options.Count > 0) options[0].OnSelect?.Invoke();
    }

    public List<InteractionOption> GetInteractionOptions()
    {
        List<InteractionOption> options = new List<InteractionOption>();

        // 1. Rotate
        options.Add(new InteractionOption { Name = "Rotate Machine", OnSelect = RotateMachine });

        // 2. Power Toggle
        if (isMachineOn)
            options.Add(new InteractionOption { Name = "Turn Off", OnSelect = () => { isMachineOn = false; SoundEffectManager.Play("PickUp"); } });
        else
            options.Add(new InteractionOption { Name = "Turn On", OnSelect = TryTurnOn });

        // 3. Put Gem
        options.Add(new InteractionOption { Name = "Put Gem", OnSelect = TryPutGem });

        // 4. Remove Gem
        options.Add(new InteractionOption { Name = "Remove Gem", OnSelect = TryTakeGem });

        return options;
    }

    private void ShowPopup(string message)
    {
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup(message, null);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void TryTurnOn()
    {
        if (string.IsNullOrEmpty(currentGem))
        {
            ShowPopup("Need a gem inside first!");
        }
        else
        {
            isMachineOn = true;
            SoundEffectManager.Play("PickUp");
        }
    }

    private void TryPutGem()
    {
        if (!string.IsNullOrEmpty(currentGem))
        {
            ShowPopup("Machine is already full!");
            return;
        }

        InventoryController inv = InventoryController.Instance;
        if (inv != null)
        {
            if (inv.HasItem(redGemName))
                PlaceGem(redGemName, redBeamColor);
            else if (inv.HasItem(blueGemName))
                PlaceGem(blueGemName, blueBeamColor);
            else
                ShowPopup("No gem in inventory!");
        }
    }

    private void TryTakeGem()
    {
        if (string.IsNullOrEmpty(currentGem))
        {
            ShowPopup("Machine is empty!");
            return;
        }

        TakeGem();
    }

    private void RotateMachine()
    {
        rotationState = (rotationState + 1) % 4;
        if (nozzleTransform != null)
            nozzleTransform.rotation = Quaternion.Euler(0, 0, -rotationState * 90f);
        SoundEffectManager.Play("PickUp");
    }

    private void PlaceGem(string gemName, Color beamColor)
    {
        InventoryController.Instance?.RemoveItemByName(gemName, 1);
        currentGem = gemName;
        SetBeamColor(beamColor);

        ItemDictionary dict = FindAnyObjectByType<ItemDictionary>();
        if (dict != null && placedItemSprite != null)
        {
            GameObject prefab = dict.GetItemPrefabByName(gemName);
            if (prefab != null)
            {
                placedItemSprite.sprite = prefab.GetComponent<SpriteRenderer>().sprite;
                // Scale stays at (1,1,1) — control gem visual size via Pixels Per Unit on the sprite texture
            }
        }
        
        if (nozzleTransform != null)
            nozzleTransform.rotation = Quaternion.Euler(0, 0, -rotationState * 90f);
    }

    private void TakeGem()
    {
        ItemDictionary dict = FindAnyObjectByType<ItemDictionary>();
        if (dict != null)
        {
            GameObject prefab = dict.GetItemPrefabByName(currentGem);
            if (prefab != null && InventoryController.Instance != null)
            {
                if (InventoryController.Instance.AddItem(prefab))
                {
                    ItemPickupUIController.Instance?.ShowItemPickup(prefab.GetComponent<Item>().Name, prefab.GetComponent<SpriteRenderer>().sprite);
                    currentGem = "";
                    isMachineOn = false;
                    if (placedItemSprite != null) placedItemSprite.sprite = null;
                    if (lineRenderer != null) lineRenderer.enabled = false;
                }
            }
        }
    }
}
