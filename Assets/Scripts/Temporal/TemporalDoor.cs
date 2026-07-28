using System.Collections;
using UnityEngine;

public class TemporalDoor : MonoBehaviour
{
    public float dissolveSpeed = 2f;

    private Collider2D col;
    private SpriteRenderer sr;

    void Start()
    {
        col = GetComponent<Collider2D>();
        sr  = GetComponent<SpriteRenderer>();
    }

    public void OpenDoor()
    {
        StopAllCoroutines();
        StartCoroutine(DissolveOut());
    }

    public void CloseDoor()
    {
        StopAllCoroutines();
        if (col) col.enabled = true;
        if (sr)  sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }

    private IEnumerator DissolveOut()
    {
        // Disable collision immediately so player can walk through
        if (col) col.enabled = false;

        if (sr != null)
        {
            Color c = sr.color;
            while (c.a > 0f)
            {
                c.a -= Time.deltaTime * dissolveSpeed;
                sr.color = c;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
