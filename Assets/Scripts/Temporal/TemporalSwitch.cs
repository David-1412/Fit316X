using UnityEngine;
using UnityEngine.Events;

public class TemporalSwitch : MonoBehaviour
{
    public UnityEvent OnSwitchActivated = new UnityEvent();
    public UnityEvent OnSwitchDeactivated = new UnityEvent();

    [Header("Checkpoint System")]
    public bool savesGameOnActivate = false;

    private int occupantCount = 0;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.red;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isEcho   = other.GetComponent<EchoPlayback>() != null;

        if (!isPlayer && !isEcho) return;

        occupantCount++;
        Debug.Log($"[Switch {name}] ENTER by {other.name}. OccupantCount={occupantCount}");

        if (occupantCount == 1)
        {
            if (sr) sr.color = Color.green;
            
            if (savesGameOnActivate)
            {
                if (SaveController.Instance != null)
                {
                    SaveController.Instance.SaveGame();
                    DungeonHUD.Instance?.ShowCombatLog("<color=#00FFFF>Game Saved at Switch!</color>");
                    Debug.Log("Game saved successfully via Switch!");
                }
                else
                {
                    Debug.LogWarning("Switch tried to save the game, but SaveController.Instance is missing! Please use 'Tools > Add Save Controller To Scene'.");
                }
            }
            else
            {
                Debug.Log("savesGameOnActivate is unchecked on this switch. Not saving.");
            }

            OnSwitchActivated?.Invoke();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isEcho   = other.GetComponent<EchoPlayback>() != null;

        if (!isPlayer && !isEcho) return;

        occupantCount = Mathf.Max(0, occupantCount - 1);
        Debug.Log($"[Switch {name}] EXIT by {other.name}. OccupantCount={occupantCount}");

        if (occupantCount == 0)
        {
            if (sr) sr.color = Color.red;
            OnSwitchDeactivated?.Invoke();
        }
    }
}
