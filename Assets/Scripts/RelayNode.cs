using System.Collections.Generic;
using UnityEngine;

public class RelayNode : MonoBehaviour
{
    [Header("Open Ports (Relative to Block)")]
    public bool topPort = true;
    public bool bottomPort = true;
    public bool rightPort = false;
    public bool leftPort = false;

    // Evaluates if the incoming light hit an open port
    public bool CanAcceptLight(Vector2 hitNormal)
    {
        // hitNormal points AWAY from the surface the light hit.
        // If light hits the top face, the normal points UP.
        // We use Vector2.Dot to compare the hit surface against the block's local rotation.
        if (topPort && Vector2.Dot(hitNormal, transform.up) > 0.5f) return true;
        if (bottomPort && Vector2.Dot(hitNormal, -transform.up) > 0.5f) return true;
        if (rightPort && Vector2.Dot(hitNormal, transform.right) > 0.5f) return true;
        if (leftPort && Vector2.Dot(hitNormal, -transform.right) > 0.5f) return true;

        return false; // Hit a solid wall with no port
    }

    // Returns all directions light should shoot out of
    public List<Vector2> GetOutputDirections(Vector2 hitNormal)
    {
        List<Vector2> outputs = new List<Vector2>();

        // Output from all open ports EXCEPT the one the light just came in through
        if (topPort && Vector2.Dot(hitNormal, transform.up) < 0.5f) outputs.Add(transform.up);
        if (bottomPort && Vector2.Dot(hitNormal, -transform.up) < 0.5f) outputs.Add(-transform.up);
        if (rightPort && Vector2.Dot(hitNormal, transform.right) < 0.5f) outputs.Add(transform.right);
        if (leftPort && Vector2.Dot(hitNormal, -transform.right) < 0.5f) outputs.Add(-transform.right);

        return outputs;
    }
}
