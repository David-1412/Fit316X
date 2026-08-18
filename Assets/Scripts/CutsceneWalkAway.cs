using System.Collections;
using UnityEngine;

public class CutsceneWalkAway : MonoBehaviour
{
    [Header("Cutscene Settings")]
    public float walkSpeed = 2f;
    public float walkDuration = 4f;
    public Vector2 walkDirection = Vector2.up;

    public void StartCutscene()
    {
        StartCoroutine(WalkAndDisappear());
    }

    private void Start()
    {
        // Automatically hook this cutscene into the NPC dialogue!
        if (TryGetComponent(out NPC npc))
        {
            npc.onDialogueEnd.AddListener(StartCutscene);
        }
    }

    private IEnumerator WalkAndDisappear()
    {
        // 1. Lock the player by pausing input
        PauseController.SetPause(true);
        
        // 2. Disable NPC interaction and physical collision so player can't block them
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // 3. Try to trigger a "Walk" animation if an animator exists
        if (TryGetComponent(out Animator anim))
        {
            // Typical 2D top down animator parameters
            anim.SetBool("isWalking", true);
            anim.SetFloat("InputX", walkDirection.x);
            anim.SetFloat("InputY", walkDirection.y);
            anim.SetFloat("LastInputX", walkDirection.x);
            anim.SetFloat("LastInputY", walkDirection.y);
        }

        // 4. Walk for X seconds
        float timer = 0;
        while (timer < walkDuration)
        {
            transform.Translate(walkDirection.normalized * walkSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // 5. Unlock the player
        PauseController.SetPause(false);

        // 6. Disappear!
        Destroy(gameObject);
    }
}
