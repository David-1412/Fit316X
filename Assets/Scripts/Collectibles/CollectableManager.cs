
using System.Collections.Generic;
using UnityEngine;


public class CollectableManager : MonoBehaviour
{
    [SerializeField]
    public Dictionary<string, bool> CollectableFlags = new();
    public Dictionary<string, int> CollectableStacks = new();

    public static CollectableManager Instance { get; private set; }

    // Register in Awake so other scripts can use Instance from their OnEnable/Start
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ── Save support (stored inside the main save by SaveController) ──────
    public List<StringBoolPair> ExportFlags()
    {
        var list = new List<StringBoolPair>();
        foreach (KeyValuePair<string, bool> pair in CollectableFlags)
            list.Add(new StringBoolPair { key = pair.Key, value = pair.Value });
        return list;
    }

    public List<StringIntPair> ExportStacks()
    {
        var list = new List<StringIntPair>();
        foreach (KeyValuePair<string, int> pair in CollectableStacks)
            list.Add(new StringIntPair { key = pair.Key, value = pair.Value });
        return list;
    }

    public void Import(List<StringBoolPair> flags, List<StringIntPair> stacks)
    {
        CollectableFlags.Clear();
        CollectableStacks.Clear();
        if (flags != null) foreach (StringBoolPair pair in flags) CollectableFlags[pair.key] = pair.value;
        if (stacks != null) foreach (StringIntPair pair in stacks) CollectableStacks[pair.key] = pair.value;
    }

    /// <summary>Kept for existing callers; collectables are now written by SaveController.</summary>
    public void save()
    {
        SaveController.Instance?.SaveGame();
    }



    // Update is called once per frame
    //void Update()
    //{
        
    //}


    // Basic Flags
    public bool HasFlag(string flag) => CollectableFlags.ContainsKey(flag);

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

