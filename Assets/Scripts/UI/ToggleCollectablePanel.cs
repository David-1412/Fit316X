using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ToggleCollectablePanel : MonoBehaviour
{
    public GameObject panel;
    private bool IsActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panel.SetActive(false);
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public void toggle()
    {
        IsActive = !IsActive;
        panel.SetActive(IsActive);
    }
}
