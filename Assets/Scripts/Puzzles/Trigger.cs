using UnityEngine;

using System.Collections.Generic;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{

    public List<LightGoal> goals;
    public UnityEvent GoalsTriggered;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (LightGoal goal in goals)
        {
            if (!goal.IsActivated) { Debug.Log($"{goal.name}"); return; }
        }
         GoalsTriggered.Invoke();
    }
}
