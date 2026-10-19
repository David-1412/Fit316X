using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Single-slot save system.
///
///   - New Game / Continue / Load Game are started from the menu through the static
///     methods (NewGame, ContinueGame) because no SaveController exists there.
///   - The Tab menu's "SaveButton" saves manually; the game also autosaves on arriving
///     at each floor and when the game closes (only in sessions started from the menu,
///     so pressing Play on a floor in the editor never overwrites your save).
///   - Each floor's puzzle progress (ISaveableState components) and picked-up world
///     items are kept while you move between floors and written into the save.
/// </summary>
public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    public const string FirstFloorScene = "F1";
    private static readonly string[] FloorScenes = { "F1", "F2", "F3", "F4", "F5" };

    public static string SaveFilePath => Application.persistentDataPath + "/saveData.json";
    public static string SaveScreenPath => Application.persistentDataPath + "/saveScreen.png";

    // ------------------------------------------------------------------
    // Session state (static so it survives scene loads and the menu)
    // ------------------------------------------------------------------

    // Puzzle progress of every floor visited this session
    private static readonly Dictionary<string, FloorSaveData> s_Floors = new Dictionary<string, FloorSaveData>();

    // World pickups registered when the current floor loaded: id -> object (null once picked up)
    private static readonly Dictionary<string, GameObject> s_TrackedPickups = new Dictionary<string, GameObject>();

    // IDs worked out when the floor loaded, so later pickups/destroys can't change them
    private static readonly Dictionary<int, string> s_ObjectIds = new Dictionary<int, string>();

    // Save chosen with Continue / Load Game, applied when the floor has loaded
    private static SaveData s_PendingLoad;

    // Elevator cores kept here too, so they survive floors without an elevator (F5)
    private static List<string> s_ElevatorCores = new List<string>();

    // True when the session was started from the menu; enables autosaves
    private static bool s_SessionFromMenu;

    private bool loadedInitialState;

    // ------------------------------------------------------------------
    // Menu entry points
    // ------------------------------------------------------------------

    public static bool HasSave() => File.Exists(SaveFilePath);

    /// <summary>True while a floor's saved puzzle progress is being re-applied.</summary>
    public static bool IsRestoring { get; private set; }

    /// <summary>Reads the save file, or returns null if there is none or it's unreadable.</summary>
    public static SaveData ReadSave()
    {
        if (!HasSave()) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveFilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Could not read save file: {e.Message}");
            return null;
        }
    }

    /// <summary>Deletes the save and starts again on Floor 1.</summary>
    public static void NewGame()
    {
        DeleteSave();
        ResetSession();
        s_SessionFromMenu = true;
        SceneManager.LoadScene(FirstFloorScene);
    }

    /// <summary>Loads the floor stored in the save; the rest is applied once it has loaded.</summary>
    public static void ContinueGame()
    {
        SaveData data = ReadSave();
        if (data == null)
        {
            NewGame();
            return;
        }

        ResetSession();
        s_SessionFromMenu = true;
        s_PendingLoad = data;
        s_ElevatorCores = data.elevatorCores != null ? new List<string>(data.elevatorCores) : new List<string>();
        if (data.floors != null)
        {
            foreach (FloorSaveData floor in data.floors) s_Floors[floor.sceneName] = floor;
        }
        ElevatorTransition.SetVisitedFloors(data.visitedFloors);

        string scene = string.IsNullOrEmpty(data.sceneName) ? FirstFloorScene : data.sceneName;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"[Save] Saved scene '{scene}' is not in the Build Profile scene list. Loading {FirstFloorScene}.");
            scene = FirstFloorScene;
        }
        SceneManager.LoadScene(scene);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
        if (File.Exists(SaveScreenPath)) File.Delete(SaveScreenPath);
    }

    private static void ResetSession()
    {
        s_Floors.Clear();
        s_TrackedPickups.Clear();
        s_ObjectIds.Clear();
        s_PendingLoad = null;
        s_ElevatorCores = new List<string>();
        ElevatorTransition.SetVisitedFloors(null);
    }

    /// <summary>
    /// Call just before loading another scene from a floor, so this floor's puzzle
    /// progress is remembered when the player comes back.
    /// </summary>
    public static void CaptureCurrentFloor()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!IsFloor(scene.name)) return;

        if (ElevatorTransition.Instance != null)
            s_ElevatorCores = ElevatorTransition.Instance.GetPlacedCoreNames();

        s_Floors[scene.name] = CaptureFloor(scene);
    }

    private static bool IsFloor(string sceneName) => Array.IndexOf(FloorScenes, sceneName) >= 0;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += OnApplicationQuitting;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnApplicationQuitting;
    }

    private void Start()
    {
        // Persistent object: Start runs once, on the first floor of the session
        if (s_PendingLoad != null)
        {
            ApplySave(s_PendingLoad);
            s_PendingLoad = null;
        }
        else
        {
            StartFresh();
        }
        loadedInitialState = true;

        WireSaveButtons();
    }

    /// <summary>Runs after each scene's Awake/OnEnable and before its Start.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsFloor(scene.name)) return;

        RegisterSceneObjects(scene);
        RestoreFloor(scene);

        // Cores were kept while on a floor without an elevator; hand them back
        if (ElevatorTransition.Instance != null && s_ElevatorCores.Count > 0 &&
            ElevatorTransition.Instance.GetPlacedCoreNames().Count == 0)
        {
            ElevatorTransition.Instance.SetPlacedCores(s_ElevatorCores);
        }

        WireSaveButtons();

        if (s_SessionFromMenu) StartCoroutine(AutosaveAfterLoad());
    }

    private IEnumerator AutosaveAfterLoad()
    {
        // Let every Start (including the Continue load above) finish first
        yield return null;
        yield return new WaitUntil(() => loadedInitialState);
        yield return null;

        SaveGame(takeScreenshot: true);
        Debug.Log($"[Save] Autosaved on arriving at {SceneManager.GetActiveScene().name}.");
    }

    private void OnApplicationQuitting()
    {
        if (!s_SessionFromMenu) return;
        SaveGame(takeScreenshot: false); // can't render a screenshot while closing
    }

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------

    /// <summary>Called by the Tab menu's Save button.</summary>
    public void SaveGameFromMenu()
    {
        if (SaveGame(takeScreenshot: true))
            ItemPickupUIController.Instance?.ShowItemPickup("Game Saved", null);
    }

    /// <summary>Writes the save file. Returns false if there was nothing to save.</summary>
    public bool SaveGame() => SaveGame(takeScreenshot: true);

    public bool SaveGame(bool takeScreenshot)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!IsFloor(scene.name) || player == null) return false;

        CaptureCurrentFloor();

        string boundaryName = "DefaultBoundary";
        var confiner = FindAnyObjectByType<Unity.Cinemachine.CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D != null)
        {
            boundaryName = confiner.BoundingShape2D.gameObject.name;
        }

        HotbarController hotbar = FindAnyObjectByType<HotbarController>();

        SaveData saveData = new SaveData
        {
            sceneName = scene.name,
            saveTime = DateTime.Now.ToString("o"),
            playerPosition = player.transform.position,
            mapBoundary = boundaryName,
            inventorySaveData = InventoryController.Instance != null ? InventoryController.Instance.GetInventoryItems() : new List<InventorySaveData>(),
            equipInventorySaveData = InventoryController.EquipInstance != null ? InventoryController.EquipInstance.GetInventoryItems() : new List<InventorySaveData>(),
            hotbarSaveData = hotbar != null ? hotbar.GetHotbarItems() : new List<InventorySaveData>(),
            questProgressData = QuestController.Instance != null ? QuestController.Instance.activateQuests : new List<QuestProgress>(),
            handinQuestIDs = QuestController.Instance != null ? QuestController.Instance.handinQuestIDs : new List<string>(),
            playerGold = CurrencyController.Instance != null ? CurrencyController.Instance.GetGold() : 0,
            shopStates = GetShopStates(),
            elevatorCores = new List<string>(s_ElevatorCores),
            visitedFloors = ElevatorTransition.GetVisitedFloors(),
            floors = s_Floors.Values.ToList()
        };

        if (CollectableManager.Instance != null)
        {
            saveData.collectableFlags = CollectableManager.Instance.ExportFlags();
            saveData.collectableStacks = CollectableManager.Instance.ExportStacks();
        }

        try
        {
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Could not write save file: {e.Message}");
            return false;
        }

        if (takeScreenshot && isActiveAndEnabled) StartCoroutine(CaptureScreenshot());
        return true;
    }

    /// <summary>
    /// Renders the game camera only, so the Tab menu and HUD aren't in the preview.
    /// </summary>
    private IEnumerator CaptureScreenshot()
    {
        yield return null;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        const int width = 640, height = 360;
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previousTarget = cam.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D tex = null;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(SaveScreenPath, tex.EncodeToPNG());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] Camera screenshot failed ({e.Message}); using a full-screen capture.");
            ScreenCapture.CaptureScreenshot(SaveScreenPath);
        }
        finally
        {
            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);
            if (tex != null) Destroy(tex);
        }
    }

    private List<ShopInstanceData> GetShopStates()
    {
        List<ShopInstanceData> shopStates = new List<ShopInstanceData>();
        foreach (var shop in FindObjectsByType<ShopNPC>())
        {
            ShopInstanceData shopData = new ShopInstanceData
            {
                shopID = shop.shopID,
                stock = new List<ShopItemData>()
            };

            foreach (var stockItem in shop.GetCurrentStock())
            {
                shopData.stock.Add(new ShopItemData
                {
                    itemID = stockItem.itemID,
                    quantity = stockItem.quantity
                });
            }

            shopStates.Add(shopData);
        }

        return shopStates;
    }

    // ------------------------------------------------------------------
    // Loading
    // ------------------------------------------------------------------

    /// <summary>Kept for existing UnityEvent references; reloads the save into this floor.</summary>
    public void LoadGame()
    {
        SaveData data = ReadSave();
        if (data != null) ApplySave(data);
    }

    private void StartFresh()
    {
        // Empty inventories still need their slots built
        HotbarController hotbar = FindAnyObjectByType<HotbarController>();
        if (InventoryController.Instance != null) InventoryController.Instance.SetInventoryItems(new List<InventorySaveData>());
        if (InventoryController.EquipInstance != null) InventoryController.EquipInstance.SetInventoryItems(new List<InventorySaveData>());
        if (hotbar != null) hotbar.SetHotbarItems(new List<InventorySaveData>());

        MapController_Dynamic.Instance?.GenerateMap();
    }

    private void ApplySave(SaveData saveData)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Position only applies on the floor it was saved on
        if (saveData.sceneName == currentScene || string.IsNullOrEmpty(saveData.sceneName))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = saveData.playerPosition;
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.position = saveData.playerPosition;
            }
        }

        RestoreMapBoundary(saveData.mapBoundary);

        HotbarController hotbar = FindAnyObjectByType<HotbarController>();
        if (InventoryController.Instance != null)
            InventoryController.Instance.SetInventoryItems(saveData.inventorySaveData ?? new List<InventorySaveData>());
        if (InventoryController.EquipInstance != null)
            InventoryController.EquipInstance.SetInventoryItems(saveData.equipInventorySaveData ?? new List<InventorySaveData>());
        if (hotbar != null)
            hotbar.SetHotbarItems(saveData.hotbarSaveData ?? new List<InventorySaveData>());

        LoadShopStates(saveData.shopStates);

        if (CurrencyController.Instance != null)
            CurrencyController.Instance.SetGold(saveData.playerGold);

        if (QuestController.Instance != null)
        {
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData ?? new List<QuestProgress>());
            QuestController.Instance.handinQuestIDs = saveData.handinQuestIDs ?? new List<string>();
        }

        if (CollectableManager.Instance != null)
            CollectableManager.Instance.Import(saveData.collectableFlags, saveData.collectableStacks);

        ElevatorTransition.SetVisitedFloors(saveData.visitedFloors);
        s_ElevatorCores = saveData.elevatorCores != null ? new List<string>(saveData.elevatorCores) : new List<string>();
        if (ElevatorTransition.Instance != null)
            ElevatorTransition.Instance.SetPlacedCores(s_ElevatorCores);
    }

    private void RestoreMapBoundary(string mapBoundary)
    {
        GameObject boundaryObj = string.IsNullOrEmpty(mapBoundary) || mapBoundary == "DefaultBoundary"
            ? null : GameObject.Find(mapBoundary);

        if (boundaryObj != null)
        {
            PolygonCollider2D savedMapBoundry = boundaryObj.GetComponent<PolygonCollider2D>();
            var confiner = FindAnyObjectByType<Unity.Cinemachine.CinemachineConfiner2D>();
            if (confiner != null && savedMapBoundry != null)
            {
                confiner.BoundingShape2D = savedMapBoundry;
                confiner.InvalidateBoundingShapeCache();
            }

            MapController_Manual.Instance?.HighlightArea(mapBoundary);
            MapController_Dynamic.Instance?.GenerateMap(savedMapBoundry);
        }
        else
        {
            MapController_Dynamic.Instance?.GenerateMap();
        }
    }

    private void LoadShopStates(List<ShopInstanceData> shopStates)
    {
        if (shopStates == null) return;

        foreach (var shop in FindObjectsByType<ShopNPC>())
        {
            ShopInstanceData shopData = shopStates.FirstOrDefault(s => s.shopID == shop.shopID);
            if (shopData == null) continue;

            List<ShopNPC.ShopStockItem> loadedStock = new List<ShopNPC.ShopStockItem>();
            foreach (var itemData in shopData.stock)
            {
                loadedStock.Add(new ShopNPC.ShopStockItem
                {
                    itemID = itemData.itemID,
                    quantity = itemData.quantity
                });
            }

            shop.SetStock(loadedStock);
        }
    }

    // ------------------------------------------------------------------
    // Save button (Tab menu > Settings)
    // ------------------------------------------------------------------

    /// <summary>
    /// Hooks every button named "SaveButton" to this controller in code, so it works on
    /// every floor even where the Inspector link is missing (F2) or points at a
    /// destroyed duplicate.
    /// </summary>
    private void WireSaveButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button.name != "SaveButton") continue;

            // Turn off the Inspector-assigned SaveGame call so it doesn't save twice
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == nameof(SaveGame))
                    button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }

            button.onClick.RemoveListener(SaveGameFromMenu);
            button.onClick.AddListener(SaveGameFromMenu);
        }
    }

    // ------------------------------------------------------------------
    // Floor state (puzzles and world pickups)
    // ------------------------------------------------------------------

    /// <summary>
    /// A stable ID for a scene object: component type plus its hierarchy path, where each
    /// step is the object's name and its position among same-named siblings, e.g.
    /// "BeamRedirector:/Puzzle#0/Redirector#2". Names (not raw sibling order) keep IDs
    /// the same when carried-over objects like the Player are added or removed.
    /// </summary>
    private static string ObjectId(Component component)
    {
        if (s_ObjectIds.TryGetValue(component.GetInstanceID(), out string cached)) return cached;
        return BuildObjectId(component);
    }

    private static string BuildObjectId(Component component)
    {
        StringBuilder path = new StringBuilder();
        for (Transform t = component.transform; t != null; t = t.parent)
        {
            path.Insert(0, $"/{t.name}#{SameNameIndex(t)}");
        }
        return component.GetType().Name + ":" + path;
    }

    private static int SameNameIndex(Transform t)
    {
        int index = 0;
        if (t.parent != null)
        {
            for (int i = 0; i < t.GetSiblingIndex(); i++)
            {
                if (t.parent.GetChild(i).name == t.name) index++;
            }
        }
        else
        {
            foreach (GameObject root in t.gameObject.scene.GetRootGameObjects())
            {
                if (root.transform == t) break;
                if (root.name == t.name) index++;
            }
        }
        return index;
    }

    /// <summary>World pickups are items lying in the scene, not inside the UI.</summary>
    private static bool IsWorldPickup(Component component)
    {
        return component.GetComponentInParent<Canvas>(true) == null;
    }

    private static IEnumerable<T> SceneComponents<T>(Scene scene) where T : class
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
                yield return component;
        }
    }

    /// <summary>
    /// Fixes the IDs of this floor's saveable objects and pickups as they are at load,
    /// and remembers every pickup present, so missing ones later count as collected.
    /// </summary>
    private static void RegisterSceneObjects(Scene scene)
    {
        s_TrackedPickups.Clear();
        s_ObjectIds.Clear();

        foreach (MonoBehaviour behaviour in SceneComponents<MonoBehaviour>(scene))
        {
            if (behaviour is ISaveableState)
                s_ObjectIds[behaviour.GetInstanceID()] = BuildObjectId(behaviour);
        }

        foreach (Item item in SceneComponents<Item>(scene))
        {
            if (!IsWorldPickup(item)) continue;
            string id = BuildObjectId(item);
            s_ObjectIds[item.GetInstanceID()] = id;
            s_TrackedPickups[id] = item.gameObject;
        }
        foreach (BasicCollectible collectible in SceneComponents<BasicCollectible>(scene))
        {
            string id = BuildObjectId(collectible);
            s_ObjectIds[collectible.GetInstanceID()] = id;
            s_TrackedPickups[id] = collectible.gameObject;
        }
    }

    private static FloorSaveData CaptureFloor(Scene scene)
    {
        FloorSaveData data = new FloorSaveData { sceneName = scene.name };

        foreach (KeyValuePair<string, GameObject> pickup in s_TrackedPickups)
        {
            if (pickup.Value == null) data.collectedPickups.Add(pickup.Key);
        }

        foreach (MonoBehaviour behaviour in SceneComponents<MonoBehaviour>(scene))
        {
            if (behaviour is ISaveableState saveable)
            {
                data.objects.Add(new SavedObjectState { id = ObjectId(behaviour), state = saveable.CaptureState() });
            }
        }

        return data;
    }

    private static void RestoreFloor(Scene scene)
    {
        if (!s_Floors.TryGetValue(scene.name, out FloorSaveData data)) return;

        // Remove pickups already collected
        HashSet<string> collected = new HashSet<string>(data.collectedPickups ?? new List<string>());
        foreach (KeyValuePair<string, GameObject> pickup in s_TrackedPickups)
        {
            if (pickup.Value != null && collected.Contains(pickup.Key))
            {
                pickup.Value.SetActive(false);
                Destroy(pickup.Value);
            }
        }

        // Puzzle pieces. Goals and puzzle controllers go last: restoring them re-fires
        // their "solved" events, which must see doors and machines already restored.
        Dictionary<string, string> states = new Dictionary<string, string>();
        foreach (SavedObjectState saved in data.objects ?? new List<SavedObjectState>())
            states[saved.id] = saved.state;

        List<MonoBehaviour> saveables = SceneComponents<MonoBehaviour>(scene)
            .Where(b => b is ISaveableState)
            .OrderBy(b => b is LightGoal || b is LightPuzzleController ? 1 : 0)
            .ToList();

        IsRestoring = true;
        try
        {
            foreach (MonoBehaviour behaviour in saveables)
            {
                if (!states.TryGetValue(ObjectId(behaviour), out string state)) continue;
                try
                {
                    ((ISaveableState)behaviour).RestoreState(state);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Save] Could not restore {behaviour.name}: {e.Message}");
                }
            }
        }
        finally
        {
            IsRestoring = false;
        }
    }
}
