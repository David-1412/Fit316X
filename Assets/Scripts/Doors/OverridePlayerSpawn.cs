using UnityEngine;

public class OverridePlayerSpawn : MonoBehaviour
{


    public bool singleuse;

    public GameObject player; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player.GetComponent<Transform>().position = transform.position;
        if (singleuse) { Destroy(gameObject); }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
