using UnityEngine;

public class debugFriend : MonoBehaviour
{

    public bool b1 = false;
    public bool b2 = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (CollectableManager.Instance.GetFlag("b1")) { b1 = true; }
        if (CollectableManager.Instance.GetFlag("b2")) { b2 = true; }
    }
}
