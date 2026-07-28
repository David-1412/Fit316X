using UnityEngine;

/// <summary>
/// Makes a portal gently pulse in scale to draw the player's attention.
/// Attach to the portal GameObject.
/// </summary>
public class PortalPulse : MonoBehaviour
{
    [Tooltip("How fast the portal pulses")]
    public float speed = 1.5f;

    [Tooltip("How much it grows/shrinks (0.1 = 10%)")]
    public float amount = 0.08f;

    private Vector3 _baseScale;

    private void Start()
    {
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * speed) * amount;
        transform.localScale = _baseScale * pulse;
    }
}
