using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Camera Mode")]
    public bool lockToRooms = true;
    public float roomHeight = 18f; // Distance between room centers
    public Vector2 firstRoomCenter = new Vector2(7f, 7f);

    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            return;
        }

        Vector3 desiredPosition;

        if (lockToRooms)
        {
            // Dynamically calculate which room the player is in (supports infinite rooms going up or down)
            int roomIndexY = Mathf.RoundToInt((target.position.y - firstRoomCenter.y) / roomHeight);
            int roomIndexX = Mathf.RoundToInt((target.position.x - firstRoomCenter.x) / roomHeight); // Optional if they expand sideways
            
            // By default, just lock X to the first room and move Y. 
            // If they want X to shift too, they can use roomIndexX * roomWidth.
            float targetY = firstRoomCenter.y + (roomIndexY * roomHeight);
            
            // To keep the exact old behavior where X is always 7f:
            Vector3 roomCenter = new Vector3(firstRoomCenter.x, targetY, 0f);
            desiredPosition = roomCenter + offset;
        }
        else
        {
            // Smoothly follow the player directly
            desiredPosition = target.position + offset;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
