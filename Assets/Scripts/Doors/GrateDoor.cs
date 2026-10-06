using UnityEngine;

public class GrateDoor : MonoBehaviour
{

    public float height = 4;

    private Transform transformComp;
    public GameObject wall;
    private bool open = false;
 

    private void Start()
    {
        transformComp = gameObject.GetComponent<Transform>();
        height += transformComp.position.y;
    }



    private void Update()
    {
        if (open)
        {
            
            transformComp.Translate(Vector2.up * Time.deltaTime);

            if(transformComp.position.y >= height) { gameObject.SetActive(false); }
        } 
    }

    public void OpenGrateDoor()
    {
        open = true;
        Destroy(wall);
    }
}
