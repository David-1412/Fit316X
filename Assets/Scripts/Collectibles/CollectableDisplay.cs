using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class CollectableDisplay : MonoBehaviour
{

    public List<string> RequisiteFlags;
    public List<string> RequisiteCounterKeys;
    public List<int> RequisiteCounterValues;


    private Image ImageRender;
    private EventTrigger EventListener;
    private Dictionary<string, int> RequisiteCounters = new();

    // Awake runs before OnEnable, so the references below are ready when OnEnable checks them
    void Awake()
    {
        ImageRender = gameObject.GetComponent<Image>();

        for (int i = 0; i < RequisiteCounterKeys.Count; i++)
        {
            RequisiteCounters.Add(RequisiteCounterKeys[i], RequisiteCounterValues[i]);
        }

        EventListener = gameObject.GetComponent<EventTrigger>();
        EventListener.enabled = false;
    }


    void OnEnable()
    {
        // Manager may not exist yet (e.g. UI enabled during scene load); check again next time the page opens
        if (CollectableManager.Instance == null)
        {
            return;
        }

        if (RequisiteFlags.Any(flag => !CollectableManager.Instance.GetFlag(flag)))
        {
            foreach (string flag in RequisiteFlags)
            {
                Debug.Log(flag);
            }
            return;
        }

        if (RequisiteCounters.Any(counter => counter.Value < CollectableManager.Instance.GetStacks(counter.Key)))
        {
            return;
        }

        ImageRender.material = null;
        EventListener.enabled = true;
        Destroy(this);
    }

    public void SetMaterial(Material lockedMaterial)
    {
        if (!ImageRender)
        {
            ImageRender = gameObject.GetComponent<Image>();
        }
        ImageRender.material = lockedMaterial;
    }
}
