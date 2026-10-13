using UnityEngine;

public class GrateDoor : MonoBehaviour
{

    public float distance = 4;
    public Vector2 direction = Vector2.up;
    public float speed = 1;
    

    private Transform transformComp;
    public GameObject wall;
    private bool open = false;
    private float movement = 0;
 

    private void Start()
    {
        transformComp = gameObject.GetComponent<Transform>();
        distance += transformComp.position.y;
    }



    private void Update()
    {
        if (open)
        {
            
            transformComp.Translate(direction * Time.deltaTime * speed);
            movement += speed * Time.deltaTime;

            if(movement >= distance) { 
                gameObject.SetActive(false);
                Destroy(wall);
            }
        } 
    }

    public void OpenGrateDoor()
    {
        open = true;
        
    }
}
