using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    public static ElevatorTransition Instance { get; private set; }

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
    public List<Color> currentColours = new List<Color>(5) {};
    public bool blue = false;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GetComponent<Collider2D>().isTrigger = true;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    private List<Color> GetGemsInInventory()
    {
        var result = new List<Color>();
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
            if (gemComp != null && gemComp.isCore)
            {
                result.Add(gemComp.gemColor);
                continue;
            }
        }

        Debug.Log($"[BeamMachine] Found {result.Count} core(s) in inventory.");
        return result;
    }

    private string colourName(Color colour)
    {
        if (colour == Color.red) { return "Red"; }
        if (colour == Color.green) { return "Green"; }
        if (colour == Color.blue) { return "Blue"; }
        if (colour == Color.white) { return "White"; }
        if (colour == Color.black) { return "Black";  }
        else { return "Unknown"; }
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
        var invColours = GetGemsInInventory();
        foreach (var coreColour in invColours)
        {
            if (!currentColours.Any((c) => c == coreColour))
            {
                Debug.LogWarning($"{currentColours} {coreColour}");
                options.Add(new InteractionOption
                {
                    Name = $"Place {colourName(coreColour)} core",
                    OnSelect = () => PlaceGem(coreColour)
                });
            }
        }

        // Travel
        foreach (var floor in floorDetails)
        {
            if (currentColours.Any((c) => c == floor.colour))
            {
                options.Add(new InteractionOption
                {
                    Name = $"Travel to {floor.name}",
                    OnSelect = () => TravelTo(floor.name)
                });
            }
        }
        if (currentColours.Any((c) => c == Color.blue) && currentColours.Any((c) => c == Color.green) && currentColours.Any((c) => c == Color.red))
        {
            options.Add(new InteractionOption
            {
                Name = $"Travel to F5",
                OnSelect = () => TravelTo("F5")
            });
        }


        // Take gem back
        foreach (var colour in currentColours)
        {
            options.Add(new InteractionOption
            {
                Name = $"Take Back {colourName(colour)} core",
                OnSelect = () => TakeGem(colour)
            });
        }



        return options;
    }

    // ?? Machine Actions ???????????????????????????????????????????????????

    private void PlaceGem(Color colour)
    {
        if (currentColours.Any((c) => c == colour)){ShowPopup("Gem already in machine"); return; }

        InventoryController.Instance?.RemoveItemByName("CoreGem" + colourName(colour), 1);;
        currentColours.Add(colour);

        SoundEffectManager.Play("PickUp");
        Debug.Log($"[Elevator] Placed '{"CoreGem" + colourName(colour)}'");
    }


    private void TakeGem(Color colour)
    {
        var dict = FindAnyObjectByType<ItemDictionary>();
        if (dict == null) return;

        var prefab = dict.GetItemPrefabByName(colourName(colour) + " Core");
        if (prefab != null && InventoryController.Instance != null)
        {
            if (InventoryController.Instance.AddItem(prefab))
            {
                ItemPickupUIController.Instance?.ShowItemPickup(
                    prefab.GetComponent<Item>().Name,
                    prefab.GetComponent<SpriteRenderer>()?.sprite);
            }
            currentColours.Remove(colour);
        }
        Debug.Log($"[Elevator] Took '{colourName(colour)} Core'");
    }



    private void TravelTo(string floorName){


        // Check it is 'safe' to travel


        // Store where to spawn in the new scene
        s_PendingPosition = targetPosition;
        s_HasPendingPosition = true;
        s_UseSpawnMarker = useSpawnMarker;


        // Update gem locations
        

        // Ensure pause state is cleared before switching scenes
        PauseController.SetPause(false);
        SceneManager.LoadScene(floorName);
    }








    // Preventing Softlocks

    private List<HashSet<Color>> coreTracker = new(7)
    {
        new(5) { Color.white}, // floor0
        new(5) { Color.green }, // floor1
        new(5) { Color.blue }, // floor2
        new(5) { Color.red }, // floor3
        new(5) { }, // floor4
        new(5) { Color.black }, // floor5
    };


    private int currentFloor = 1;

    private void UpdateCoreTracker(List<Color> movingColours, int destination)
    {
        coreTracker[currentFloor].ExceptWith(movingColours);
        coreTracker[destination].Union(movingColours);
    }


    private int toFloor(Color c)
    {
        if (c == Color.red) { return 4; }
        if (c == Color.green) { return 2; }
        if (c == Color.blue) { return 3; }
        if (c == Color.white) { return 5; }
        if (c == Color.black) { return 0; }
        else { return 10; }
    }

    private bool CheckAccess(List<Color> movingColours)
    {
        List<Color> access = new();
        List<Color> check = new();
        check.Union(movingColours);

        while (check.Count() > 0)
        {
            Color floor = check[0];
            check.Union(coreTracker[toFloor(floor)]);
            
            check.RemoveAt(0);
            access.Add(floor);

            if (access.Any((c) => c == Color.blue) && access.Any((c) => c == Color.green) && access.Any((c) => c == Color.red))
            {
                check.Add(Color.white);
            }
        }
        return access.Count() == 5;
    }

    private List<Color> FindMissing(List<Color> movingColours)
    {
        List<Color> test = new();
        test.Union(movingColours);


        foreach (Color c in coreTracker[currentFloor])
        {
            if (test.Any((a) => a == c)) { continue; }
            test.Add(c);
            if (CheckAccess(movingColours)) { return new() { c }; }
            test.Remove(c);
        }

        List<Color> missing = new();

        foreach (Color c in coreTracker[currentFloor])
        {
            if (test.Any((a) => a == c)) { continue; }
            test.Add(c);
            missing.Add(c);

            foreach (Color c2 in coreTracker[currentFloor])
            {
                if (test.Any((a) => a == c2)) { continue; }
                test.Add(c2);
                if (CheckAccess(movingColours))
                {
                    missing.Add(c2);
                    return missing;
                }
                missing.Remove(c2);
            }
        }

        Debug.LogError("Softlock");
        return test;

    }

}
