using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string mapBoundary; //The boundary name for the map
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> equipInventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public List<ChestSaveData> chestSaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handinQuestIDs;

    public int playerGold;
    public List<ShopInstanceData> shopStates = new();

}

[System.Serializable]
public class ChestSaveData
{
    public string chestID;
    public bool isOpened;
}

[System.Serializable]
public class ShopInstanceData
{
    public string shopID;
    public List<ShopItemData> stock = new();
}

[System.Serializable]
public class ShopItemData
{
    public int itemID;
    public int quantity;
}
