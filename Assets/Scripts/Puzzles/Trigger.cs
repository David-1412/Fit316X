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
        if (goals.TrueForAll((goal) => goal.IsActivated))
        {
            GoalsTriggered.Invoke();
        }
    }
}
