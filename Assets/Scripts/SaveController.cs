using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;
    private Chest[] chests;
    private ShopNPC[] shops; //Track shops in scene

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeComponents();
        LoadGame();
    }

    private void InitializeComponents()
    {
        saveLocation = Application.persistentDataPath + "/saveData.json";
        inventoryController = UnityEngine.Object.FindAnyObjectByType<InventoryController>();
        hotbarController = UnityEngine.Object.FindAnyObjectByType<HotbarController>();
        chests = Object.FindObjectsByType<Chest>();
        shops = Object.FindObjectsByType<ShopNPC>();
    }

    public void SaveGame()
    {
        string boundaryName = "DefaultBoundary";
        var confiner = UnityEngine.Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D != null)
        {
            boundaryName = confiner.BoundingShape2D.gameObject.name;
        }

        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            mapBoundary = boundaryName,
            inventorySaveData = InventoryController.Instance != null ? InventoryController.Instance.GetInventoryItems() : new List<InventorySaveData>(),
            equipInventorySaveData = InventoryController.EquipInstance != null ? InventoryController.EquipInstance.GetInventoryItems() : new List<InventorySaveData>(),
            hotbarSaveData = hotbarController != null ? hotbarController.GetHotbarItems() : new List<InventorySaveData>(),
            chestSaveData = GetChestsState(),
            questProgressData = QuestController.Instance != null ? QuestController.Instance.activateQuests : new List<QuestProgress>(),
            handinQuestIDs = QuestController.Instance != null ? QuestController.Instance.handinQuestIDs : new List<string>(),
            playerGold = CurrencyController.Instance != null ? CurrencyController.Instance.GetGold() : 0,
            shopStates = GetShopStates()
        };

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        ScreenCapture.CaptureScreenshot(Application.persistentDataPath + "/saveScreen.png");
    }

    private List<ShopInstanceData> GetShopStates()
    {
        List<ShopInstanceData> shopStates = new List<ShopInstanceData>();
        foreach(var shop in shops)
        {
            ShopInstanceData shopData = new ShopInstanceData
            {
                shopID = shop.shopID,
                stock = new List<ShopItemData>()
            };

            foreach(var stockItem in shop.GetCurrentStock())
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

    private List<ChestSaveData> GetChestsState()
    {
        List<ChestSaveData> chestStates = new List<ChestSaveData>();

        foreach(Chest chest in chests)
        {
            ChestSaveData chestSaveData = new ChestSaveData
            {
                chestID = chest.ChestID,
                isOpened = chest.IsOpened
            };
            chestStates.Add(chestSaveData);
        }

        return chestStates;
    }

    public void LoadGame()
    {
        if (File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            if (!string.IsNullOrEmpty(saveData.mapBoundary))
            {
                GameObject boundaryObj = GameObject.Find(saveData.mapBoundary);
                
                // Only teleport the player if the save file's map actually exists in this scene
                // (Prevents teleporting them into the void if they load a SampleScene save in Dungeon_Floor2)
                if (boundaryObj != null || saveData.mapBoundary == "DefaultBoundary")
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null) player.transform.position = saveData.playerPosition;
                }

                if (boundaryObj != null && saveData.mapBoundary != "DefaultBoundary")
                {
                    PolygonCollider2D savedMapBoundry = boundaryObj.GetComponent<PolygonCollider2D>();
                    var confiner = UnityEngine.Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineConfiner2D>();
                    if (confiner != null)
                    {
                        confiner.BoundingShape2D = savedMapBoundry;
                        confiner.InvalidateBoundingShapeCache();
                    }

                    MapController_Manual.Instance?.HighlightArea(saveData.mapBoundary);
                    MapController_Dynamic.Instance?.GenerateMap(savedMapBoundry);
                }
                else
                {
                    MapController_Dynamic.Instance?.GenerateMap();
                }
            }

            if (InventoryController.Instance != null && saveData.inventorySaveData != null) 
                InventoryController.Instance.SetInventoryItems(saveData.inventorySaveData);
            if (InventoryController.EquipInstance != null && saveData.equipInventorySaveData != null) 
                InventoryController.EquipInstance.SetInventoryItems(saveData.equipInventorySaveData);
            if (hotbarController != null)
                hotbarController.SetHotbarItems(saveData.hotbarSaveData);

            LoadChestStates(saveData.chestSaveData);
            LoadShopStates(saveData.shopStates);

            if (CurrencyController.Instance != null)
                CurrencyController.Instance.SetGold(saveData.playerGold);

            if (QuestController.Instance != null)
            {
                QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
                QuestController.Instance.handinQuestIDs = saveData.handinQuestIDs;
            }
        }
        else
        {
            SaveGame();

            if (InventoryController.Instance != null) InventoryController.Instance.SetInventoryItems(new List<InventorySaveData>());
            if (InventoryController.EquipInstance != null) InventoryController.EquipInstance.SetInventoryItems(new List<InventorySaveData>());
            if (hotbarController != null) hotbarController.SetHotbarItems(new List<InventorySaveData>());

            MapController_Dynamic.Instance?.GenerateMap();
        }
    }

    private void LoadShopStates(List<ShopInstanceData> shopStates)
    {
        if (shopStates == null) return;

        foreach(var shop in shops)
        {
            ShopInstanceData shopData = shopStates.FirstOrDefault(s => s.shopID == shop.shopID);

            if(shopData != null)
            {
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
    }


    private void LoadChestStates(List<ChestSaveData> chestStates)
    {
        foreach(Chest chest in chests)
        {
            ChestSaveData chestSaveData = chestStates.FirstOrDefault(c => c.chestID == chest.ChestID);

            if (chestSaveData != null)
            {
                chest.SetOpened(chestSaveData.isOpened);
            }
        }
    }
}
