using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Replaces PlayerMovement for turn-based grid movement in Dungeon_Floor2.
/// Attaches itself to the existing player, disables PlayerMovement while active.
/// WASD moves one tile per turn. Same Animator params as the existing player.
/// </summary>
public class GridTurnPlayer : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp => PlayerEquipment.Instance != null ? PlayerEquipment.Instance.MaxHp : 5;
    public int attackDamage => PlayerEquipment.Instance != null ? PlayerEquipment.Instance.AttackDamage : 2;

    [Header("Feel")]
    public float slideTime   = 0.1f;   // seconds to animate the tile slide

    public int  CurrentHp { get; private set; }
    public static GridTurnPlayer Instance { get; private set; }
    public Vector2Int GridPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

    private Animator       _anim;
    private PlayerMovement _pm;
    private Rigidbody2D    _rb;
    private bool           _moving;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        Instance   = this;
        CurrentHp  = maxHp;
        _anim      = GetComponent<Animator>();
        _rb        = GetComponent<Rigidbody2D>();

        // Freeze physics — movement is purely grid-based here
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Suspend normal free-roam movement
        _pm = GetComponent<PlayerMovement>();
        if (_pm != null) _pm.enabled = false;

        // Snap to grid
        var p = transform.position;
        transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);
    }

    private void OnDestroy()
    {
        // Restore normal movement when leaving Floor2
        if (_pm  != null) _pm.enabled = true;
        if (_rb  != null) _rb.bodyType = RigidbodyType2D.Dynamic;
        if (Instance == this) Instance = null;
    }

    // ── Input (only during PlayerTurn) ───────────────────────────────

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        
        bool pressedMove = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame ||
                           kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                           kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame ||
                           kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;

        if (TurnManager.Instance == null) {
            if (pressedMove) Debug.LogWarning("Cannot move: TurnManager.Instance is NULL!");
            return;
        }
        if (TurnManager.Instance.State != TurnManager.TurnState.PlayerTurn) {
            if (pressedMove) Debug.LogWarning($"Cannot move: It is currently {TurnManager.Instance.State}, waiting for enemies to finish moving!");
            return;
        }
        if (_moving) {
            return;
        }
        if (PauseController.IsGamePaused) {
            if (pressedMove) Debug.LogWarning("Cannot move: The game is currently PAUSED!");
            return;
        }

        Vector2Int dir = Vector2Int.zero;
        if      (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)    dir = Vector2Int.up;
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)  dir = Vector2Int.down;
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  dir = Vector2Int.left;
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) dir = Vector2Int.right;

        if (dir != Vector2Int.zero) TryMove(dir);
    }

    // ── Movement ─────────────────────────────────────────────────────

    private void TryMove(Vector2Int dir)
    {
        Face(dir);

        Vector2Int target  = GridPos + dir;
        Vector2 worldTgt = new Vector2(target.x, target.y);

        // Check for enemies directly by GridPos
        var allEnemies = Object.FindObjectsByType<GridEnemy>(FindObjectsInactive.Exclude);
        foreach (var enemy in allEnemies)
        {
            if (enemy.GridPos == target)
            {
                Face(dir);
                CombatHandler.PlayerAttacks(this, enemy);
                TurnManager.Instance.PlayerTurnDone();
                return;
            }
        }

        // Scan the target tile for solid walls/obstacles
        var hits = Physics2D.OverlapCircleAll(worldTgt, 0.3f);
        bool blocked = false;

        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;  // skip self

            var interactable = h.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                Face(dir);
                interactable.Interact();
                TurnManager.Instance.PlayerTurnDone();
                return;
            }

            // Solid collider → wall / obstacle
            if (!h.isTrigger)
            {
                blocked = true;
                Debug.LogWarning($"Cannot move: Blocked by solid physics object '{h.gameObject.name}'!");
                break;
            }
        }

        if (blocked) return;

        StartCoroutine(SlideTo(worldTgt, target));
    }

    private IEnumerator SlideTo(Vector2 world, Vector2Int grid)
    {
        _moving = true;
        _anim?.SetBool("isWalking", true);

        if (_rb != null) _rb.WakeUp(); // Ensure physics engine knows we are moving

        Vector2 start = transform.position;
        float   elapsed = 0f;

        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, world, elapsed / slideTime);
            yield return null;
        }

        transform.position = world;

        _anim?.SetBool("isWalking", false);
        _moving = false;

        TurnManager.Instance?.PlayerTurnDone();
    }

    // ── Damage ───────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        DungeonHUD.Instance?.RefreshHP();

        if (CurrentHp <= 0)
            TurnManager.Instance?.TriggerGameOver();
    }

    public void SetCurrentHp(int hp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, maxHp);
        DungeonHUD.Instance?.RefreshHP();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void Face(Vector2Int dir)
    {
        if (_anim == null) return;
        _anim.SetFloat("InputX",     dir.x);
        _anim.SetFloat("InputY",     dir.y);
        _anim.SetFloat("LastInputX", dir.x);
        _anim.SetFloat("LastInputY", dir.y);
    }
}
