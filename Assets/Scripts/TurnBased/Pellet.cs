using UnityEngine;

/// <summary>
/// A collectible pellet the player walks over to collect.
/// Registers itself with PelletSystem on Start.
/// </summary>
public class Pellet : MonoBehaviour
{
    private void Start()
    {
        PelletSystem.Instance?.Register(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            PelletSystem.Instance?.Collect(gameObject);
    }
}
