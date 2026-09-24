using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ElevatorTransition : MonoBehaviour, IMultiInteractable
{
    public Vector2 targetPosition;

    /// <summary>
    /// If true, look for a GameObject named "PlayerSpawn" in the target scene
    /// and teleport the player there instead of using targetPosition.
    /// </summary>
    public bool useSpawnMarker = false;

    // Persists across scene load so we can apply the position after the scene is ready
    private static Vector2 s_PendingPosition;
    private static bool s_HasPendingPosition;
    private static bool s_UseSpawnMarker;

    // Floor details
    private struct FloorDetails
    {
        public string name;
        public Color colour;

        public FloorDetails(string n, Color c)
        {
            name = n; colour = c; 
        }
    }
    private List<FloorDetails> floorDetails = new List<FloorDetails> { 
        new FloorDetails("F4", Color.red),
        new FloorDetails("F2", Color.green),
        new FloorDetails("F3", Color.blue),
        new FloorDetails("F0", Color.black),
        new FloorDetails("F5", Color.white)
    };


    // Internal state
    private List<GemEntry> currentGems = new List<GemEntry>(5) {};

    private List<Color> currentColours = new List<Color>(5) {};

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;


        // Try to find an elevator to land on
        var marker = GameObject.Find("ElevatorController");
        if (marker != null)
        {
            player.transform.position = marker.transform.position;
            return;
        }


        if (!s_HasPendingPosition) return;
        s_HasPendingPosition = false;

        player.transform.position = s_PendingPosition;
    }



    private void ShowPopup(string message)
    {
        if (ItemPickupUIController.Instance != null)
            ItemPickupUIController.Instance.ShowItemPickup(message, null);
        else
            Debug.Log(message);
    }

    // ?? Gem Discovery ?????????????????????????????????????????????????????
    private struct GemEntry
    {
        public string name;
        public Color color;
        public Sprite sprite;
    }

    /// <summary>
    /// Returns all gem-like items in the player inventory.
    /// Priority: GemItem component ? name-based color inference.
    /// </summary>
    private List<GemEntry> GetGemsInInventory()
    {
        var result = new List<GemEntry>();
        var inv = InventoryController.Instance;
        if (inv == null || inv.inventoryPanel == null)
        {
            Debug.LogWarning("[BeamMachine] InventoryController.Instance or inventoryPanel is null!");
            return result;
        }

        foreach (Transform slotTransform in inv.inventoryPanel.transform)
        {
            var slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            var item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.quantity <= 0) continue;

            var sr = slot.currentItem.GetComponent<SpriteRenderer>()
                  ?? slot.currentItem.GetComponentInChildren<SpriteRenderer>();

            // ?? Option A: GemItem component (explicit, preferred) ?????????
            var gemComp = slot.currentItem.GetComponent<GemItem>();
            if (gemComp != null)
            {
                result.Add(new GemEntry { name = item.Name, color = gemComp.gemColor, sprite = sr?.sprite });
                continue;
            }

            // ?? Option B: Name-based detection (works without any setup) ??
            string lower = item.Name.ToLower();
            if (lower.Contains("gem") || lower.Contains("crystal") || lower.Contains("orb"))
            {
                result.Add(new GemEntry
                {
                    name = item.Name,
                    color = ColorFromItemName(item.Name),
                    sprite = sr?.sprite
                });
            }
        }

        Debug.Log($"[BeamMachine] Found {result.Count} gem(s) in inventory.");
        return result;
    }

    /// <summary>Infers beam colour from an item's name. Extend freely.</summary>
    private static Color ColorFromItemName(string name)
    {
        string n = name.ToLower();
        if (n.Contains("red")) return Color.red;
        if (n.Contains("blue")) return Color.cyan;
        if (n.Contains("green")) return Color.green;
        if (n.Contains("yellow")) return Color.yellow;
        if (n.Contains("purple") || n.Contains("violet")) return new Color(0.6f, 0f, 1f);
        if (n.Contains("orange")) return new Color(1f, 0.5f, 0f);
        if (n.Contains("white")) return Color.white;
        if (n.Contains("pink")) return new Color(1f, 0.4f, 0.7f);
        return Color.grey; // unknown gem 
    }

    // ?? IMultiInteractable ????????????????????????????????????????????????
    public bool CanInteract() => true;

    public void Interact()
    {
        var options = GetInteractionOptions();
        if (options.Count > 0) options[0].OnSelect?.Invoke();
    }

    public List<InteractionOption> GetInteractionOptions()
    {
        

        var options = new List<InteractionOption>();

        // Place gems
        var gems = GetGemsInInventory();
        foreach (var gem in gems)
        {
            if (!currentColours.Contains(gem.color))
            {
                options.Add(new InteractionOption
                {
                    Name = $"Place {gem.color} core",
                    OnSelect = () => PlaceGem(gem)
                });
            }
        }

        // Travel
        foreach (var floor in floorDetails)
        {
            if (currentColours.Contains(floor.colour))
            {
                options.Add(new InteractionOption
                {
                    Name = $"Travel to {floor.name}",
                    OnSelect = () => TravelTo(floor.name)
                });
            }
        }
        if (currentColours.Contains(Color.blue) && currentColours.Contains(Color.green) && currentColours.Contains(Color.red))
        {
            options.Add(new InteractionOption
            {
                Name = $"Travel to F5",
                OnSelect = () => TravelTo("F5")
            });
        }

        // Take gem back
        foreach (var gem in currentGems)
        {
            options.Add(new InteractionOption
            {
                Name = $"Take Back {gem.color} core",
                OnSelect = () => TakeGem(gem)
            });
        }



        return options;
    }

    // ?? Machine Actions ???????????????????????????????????????????????????

    private void PlaceGem(GemEntry gem)
    {
        if (currentColours.Contains(gem.color)){ShowPopup("Gem already in machine"); return; }

        InventoryController.Instance?.RemoveItemByName(gem.name, 1);
        currentGems.Add(gem);
        currentColours.Add(gem.color);

        SoundEffectManager.Play("PickUp");
        Debug.Log($"[Elevator] Placed '{gem.name}', '{gem.color}'");
    }


    private void TakeGem(GemEntry gem)
    {
        var dict = FindAnyObjectByType<ItemDictionary>();
        if (dict == null) return;

        var prefab = dict.GetItemPrefabByName(gem.name);
        if (prefab != null && InventoryController.Instance != null)
        {
            if (InventoryController.Instance.AddItem(prefab))
            {
                ItemPickupUIController.Instance?.ShowItemPickup(
                    prefab.GetComponent<Item>().Name,
                    prefab.GetComponent<SpriteRenderer>()?.sprite);
            }
            currentColours.Remove(gem.color);
            currentGems.Remove(gem);
        }
        Debug.Log($"[Elevator] Took '{gem.name}', '{gem.color}'");
    }



    private void TravelTo(string floorName){

        // Store where to spawn in the new scene
        s_PendingPosition = targetPosition;
        s_HasPendingPosition = true;
        s_UseSpawnMarker = useSpawnMarker;

        // Ensure pause state is cleared before switching scenes
        PauseController.SetPause(false);

        SceneManager.LoadScene(floorName);
    }

}
