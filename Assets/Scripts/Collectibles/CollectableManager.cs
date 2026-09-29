
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class CollectableManager : MonoBehaviour
{
    [SerializeField]
    public Dictionary<string, bool> CollectableFlags = new();
    public Dictionary<string, int> CollectableStacks = new();

    public static CollectableManager Instance { get; private set; }

    private string collectablesFlagsFile;
    private string collectablesStacksFile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null) { 
            Instance = this;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Assert(Instance != null);


        collectablesFlagsFile = Application.persistentDataPath + "/collectablesFlags.json";


        if (!File.Exists(collectablesFlagsFile)){
            File.Create(collectablesFlagsFile);
            Debug.Log("Collectable Flag Save file created");
        } else
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(collectablesFlagsFile), CollectableFlags);
            Debug.Log("Saved Flags Recovered");
        }

        collectablesStacksFile = Application.persistentDataPath + "/collectablesStacks.json";
        if (!File.Exists(collectablesStacksFile))
        {
            File.Create(collectablesStacksFile);
            Debug.Log("Collectable Stack Save file created");
        }
        else
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(collectablesStacksFile), CollectableStacks);
            Debug.Log("Saved Stacks Recovered");
        }

    }



    public void save()
    {
        if (!File.Exists(collectablesFlagsFile))
        {
            File.Create(collectablesFlagsFile);
            Debug.Log("Collectable Flag Save file created");
        }

        if (!File.Exists(collectablesStacksFile))
        {
            File.Create(collectablesStacksFile);
            Debug.Log("Collectable Stack Save file created");
        }


        File.WriteAllText(collectablesFlagsFile, JsonUtility.ToJson(CollectableFlags));
        File.WriteAllText(collectablesStacksFile, JsonUtility.ToJson(CollectableStacks));
    }



    // Update is called once per frame
    //void Update()
    //{
        
    //}


    // Basic Flags
    public void SetFlag(string flag, bool value)
    {
        CollectableFlags[flag] = value;
        toString();
    }

    public bool GetFlag(string flag) {
        bool value;
        if (CollectableFlags.TryGetValue(flag, out value))
        {
            return value;
        }
        else
        {
            return false;
        }
    }

    // Counters
    public void IncrementStack(string stackName, int amount = 1)
    {
        if (!CollectableStacks.ContainsKey(stackName))
        {
            CollectableStacks[stackName] = amount;
        }
        else 
        {
            CollectableStacks[stackName] += amount;
        }
        toString();

    }

    public int GetStacks(string stackName)
    {
        int value;
        if (CollectableStacks.TryGetValue(stackName, out value))
        {
            return value;
        }
        else
        {
            return 0;
        }
    }

    public void toString()
    {
        foreach (KeyValuePair<string, bool> pair in CollectableFlags)
        {
            Debug.Log($"{pair.Key}, {pair.Value}");
        }
        foreach (KeyValuePair<string, int> pair in CollectableStacks)
        {
            Debug.Log($"{pair.Key}, {pair.Value}");
        }
    }
}

