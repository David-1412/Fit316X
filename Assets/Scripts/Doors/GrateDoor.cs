using UnityEngine;

public class GrateDoor : MonoBehaviour
{

    public float distance = 4;
    public Vector2 direction = Vector2.up;
    public float speed = 1;
    

    private Transform transformComp;
    public GameObject wall;
    private bool opening = false;
    private bool closeing = false;
    private float movement = 0;
    private bool announced = false;
 

    private void Start()
    {
        transformComp = gameObject.GetComponent<Transform>();
    }



    private void Update()
    {
        if (opening)
        {
            
            transformComp.Translate(direction * Time.deltaTime * speed);
            movement += speed * Time.deltaTime;

            if(movement >= distance) { 
                wall.SetActive(false);
                opening = false;
            }
        } 
        if (closeing)
        {
            transformComp.Translate(-1 * direction * Time.deltaTime * speed);
            movement -= speed * Time.deltaTime;

            if (movement <= 0)
            {
                wall.SetActive(true);
                closeing = false;
            }
        }
    }

    public void OpenGrateDoor()
    {
        opening = true;
        closeing = false;

        // The room's puzzle opens this door: announce it the first time only
        // (Trigger calls this every frame, and beams can re-open it later)
        if (!announced)
        {
            announced = true;
            PuzzleSolved.Announce();
        }
    }


    public void CloseGrateDoor()
    {
        opening = false;
        closeing = true;
    }
}
