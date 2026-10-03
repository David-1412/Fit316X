using UnityEngine;
using UnityEngine.UI;

public class FloorCollectionHolder : MonoBehaviour
{
    public Material lockedMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (CollectableDisplay display in GetComponentsInChildren<CollectableDisplay>(true)){
            display.SetMaterial(lockedMaterial);
            Debug.Log("Set Material");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
