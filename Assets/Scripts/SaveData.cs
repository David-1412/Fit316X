using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // Where and when
    public string sceneName;            //Floor the player saved on (F1-F5)
    public string saveTime;             //Local time of the save, ISO 8601
    public Vector3 playerPosition;
    public string mapBoundary; //The boundary name for the map

    // Player
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> equipInventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handinQuestIDs;
    public int playerGold;
    public List<ShopInstanceData> shopStates = new();

    // Lore collectables
    public List<StringBoolPair> collectableFlags = new();
    public List<StringIntPair> collectableStacks = new();

    // Elevator
    public List<string> elevatorCores = new();   //Cores placed in the elevator, by colour name
    public List<string> visitedFloors = new();   //Floors the elevator can return to

    // Puzzle progress, one entry per floor the player has been on
    public List<FloorSaveData> floors = new();
}

/// <summary>Saved state of one floor: picked-up items and puzzle pieces.</summary>
[System.Serializable]
public class FloorSaveData
{
    public string sceneName;
    public List<string> collectedPickups = new();   //IDs of world items already picked up
    public List<SavedObjectState> objects = new();  //ISaveableState components
}

[System.Serializable]
public class SavedObjectState
{
    public string id;
    public string state;
}

[System.Serializable]
public class StringBoolPair
{
    public string key;
    public bool value;
}

[System.Serializable]
public class StringIntPair
{
    public string key;
    public int value;
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
