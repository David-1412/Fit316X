using UnityEngine;

public class EchoPlayback : MonoBehaviour
{
    public EchoRecorder recorder;
    public float delaySeconds = 5f;
    
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 1f, 1f, 0.5f); // Cyan, translucent
        sr.sortingOrder = 5;

        // Ensure the player's sprite is copied if none assigned
        var playerSR = Object.FindObjectOfType<EchoRecorder>()?.GetComponent<SpriteRenderer>();
        if (sr.sprite == null && playerSR != null) sr.sprite = playerSR.sprite;

        // Add trigger collider so the Echo physically activates pressure plates
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.8f);
            col = box;
        }
        // Rigidbody2D is REQUIRED for OnTriggerEnter2D to fire — must be kinematic
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Tag as EchoGhost so PressurePlates can detect this echo
        // (Add "EchoGhost" tag in Edit → Project Settings → Tags and Layers first)
        try { gameObject.tag = "EchoGhost"; }
        catch { Debug.LogWarning("[EchoPlayback] 'EchoGhost' tag not found. Add it via Edit → Project Settings → Tags and Layers."); }
    }

    void FixedUpdate()
    {
        if (recorder == null || recorder.history.Count == 0) 
        {
            if (sr != null) sr.enabled = false;
            return;
        }

        float targetTime = Time.time - delaySeconds;
        if (targetTime <= recorder.history[0].timestamp) 
        {
            if (sr != null) sr.enabled = false;
            return; 
        }

        if (sr != null) sr.enabled = true; 

        EchoSnapshot before = recorder.history[0];
        EchoSnapshot after = recorder.history[recorder.history.Count - 1];

        if (targetTime > after.timestamp)
        {
            transform.position = new Vector3(after.position.x, after.position.y, transform.position.z);
            rb.position = after.position;
            return;
        }

        for (int i = 0; i < recorder.history.Count - 1; i++)
        {
            if (recorder.history[i].timestamp <= targetTime && recorder.history[i+1].timestamp >= targetTime)
            {
                before = recorder.history[i];
                after = recorder.history[i+1];
                break;
            }
        }

        float t = Mathf.InverseLerp(before.timestamp, after.timestamp, targetTime);
        Vector2 targetPos = Vector2.Lerp(before.position, after.position, t);

        // Set both transform and rb.position so sprite is visible AND physics triggers fire
        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        rb.position = targetPos;

        if (before.isInteracting || after.isInteracting)
        {
            TriggerInteraction();
        }
    }

    void TriggerInteraction()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
        foreach(var hit in hits)
        {
            var interactable = hit.GetComponent<ITemporalInteractable>();
            if (interactable != null)
            {
                interactable.TemporalInteract(this.gameObject);
            }
        }
    }
}
