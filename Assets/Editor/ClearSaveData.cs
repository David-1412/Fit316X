using UnityEditor;
using UnityEngine;
using System.IO;

public class ClearSaveData
{
    [MenuItem("Tools/Clear Save Data")]
    public static void ClearSave()
    {
        string savePath = Application.persistentDataPath + "/saveData.json";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"<color=#00FF00>Successfully deleted save file at: {savePath}</color>");
        }
        else
        {
            Debug.Log($"<color=#FFFF00>No save file found at: {savePath}. You are already starting fresh!</color>");
        }
    }
}
