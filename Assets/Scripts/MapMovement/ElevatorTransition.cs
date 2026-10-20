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

    // Floors the player has reached; once reached, the elevator can always return there
    private static readonly string[] s_FloorOrder = { "F1", "F2", "F3", "F4", "F5" };
    private static readonly HashSet<string> s_VisitedFloors = new HashSet<string>();

    private static void RecordVisit(string sceneName)
    {
        if (s_FloorOrder.Contains(sceneName)) s_VisitedFloors.Add(sceneName);
    }

    // ── Save support ───────────────────────────────────────────────────────
    public static List<string> GetVisitedFloors() => s_VisitedFloors.ToList();

    /// <summary>Replaces the visited floors (null clears them, e.g. for New Game).</summary>
    public static void SetVisitedFloors(IEnumerable<string> floors)
    {
        s_VisitedFloors.Clear();
        if (floors != null)
        {
            foreach (string floor in floors) RecordVisit(floor);
        }
        RecordVisit(SceneManager.GetActiveScene().name);
    }

    private static readonly (string name, Color colour)[] s_CoreColours =
    {
        ("Red", Color.red), ("Green", Color.green), ("Blue", Color.blue),
        ("White", Color.white), ("Black", Color.black)
    };

    /// <summary>Cores in the elevator, as colour names ("Red", "Blue"...).</summary>
    public List<string> GetPlacedCoreNames()
    {
        var names = new List<string>();
        foreach (var colour in currentColours)
        {
            foreach (var core in s_CoreColours)
            {
                if (core.colour == colour) { names.Add(core.name); break; }
            }
        }
        return names;
    }

    /// <summary>Puts cores back into the elevator from saved colour names.</summary>
    public void SetPlacedCores(IEnumerable<string> coreNames)
    {
        currentColours.Clear();
        if (coreNames == null) return;
        foreach (string coreName in coreNames)
        {
            foreach (var core in s_CoreColours)
            {
                if (core.name == coreName && !currentColours.Contains(core.colour))
                {
                    currentColours.Add(core.colour);
                    break;
                }
            }
        }
    }

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
        new FloorDetails("F5", Color.white),
        new FloorDetails("F1", Color.black),
        new FloorDetails("F3", Color.blue)
    };


    // Internal state
    public List<Color> currentColours = new List<Color>(5) {};
    public bool blue = false;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GetComponent<Collider2D>().isTrigger = true;
        RecordVisit(SceneManager.GetActiveScene().name);

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
        RecordVisit(scene.name);
        if (!s_HasPendingPosition) return;
        s_HasPendingPosition = false;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Search the destination scene, not DontDestroyOnLoad: that contains
        // the carried-over elevator at the previous floor's coordinates.
        var transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true));
        var marker = transforms.FirstOrDefault(t => t.name == "ElevatorArrival");
        if (marker == null && s_UseSpawnMarker)
            marker = transforms.FirstOrDefault(t => t.name == "PlayerSpawn");
        var destinationElevator = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ElevatorTransition>(true))
            .FirstOrDefault();
        Vector3 arrival = marker != null ? marker.position
            : destinationElevator != null ? destinationElevator.transform.position
            : (Vector3)s_PendingPosition;
        if (Instance != null && (marker != null || destinationElevator != null))
        {
            Instance.transform.position = arrival;
        }
        player.transform.position = arrival;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = arrival;
            body.linearVelocity = Vector2.zero;
        }
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

        // Travel: floors unlocked by placed cores, plus any floor already visited (so travel is two-way)
        var destinations = new HashSet<string>(s_VisitedFloors);
        foreach (var floor in floorDetails)
        {
            if (currentColours.Any((c) => c == floor.colour)) destinations.Add(floor.name);
        }
        destinations.Remove(SceneManager.GetActiveScene().name);

        // Known floors in order first, then anything else (e.g. F0)
        foreach (var floorName in s_FloorOrder.Where(destinations.Contains).Concat(destinations.Except(s_FloorOrder)))
        {
            options.Add(new InteractionOption
            {
                Name = $"Travel to {floorName}",
                OnSelect = () => TravelTo(floorName)
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

        InventoryController.Instance?.RemoveItemByName(colourName(colour) + " Core", 1);
        currentColours.Add(colour);

        SoundEffectManager.Play("PickUp");
        Debug.Log($"[Elevator] Placed {colourName(colour)} Core");
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
        

        // Remember this floor's puzzle progress before leaving it
        SaveController.CaptureCurrentFloor();

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
        if (c == Color.blue) { return 5; }
        if (c == Color.white) { return 3; }
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
