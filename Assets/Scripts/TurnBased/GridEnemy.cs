using System.Collections;
using UnityEngine;

/// <summary>
/// Turn-based enemy that advances one step along its patrol waypoints per enemy turn.
/// Uses the same Animator parameters (InputX, InputY, isWalking) as SampleScene NPCs.
///
/// Guard   — horizontal back-and-forth   HP=2  ATK=1
/// Soldier — square clockwise loop       HP=3  ATK=2
/// </summary>
public class GridEnemy : MonoBehaviour
{
    [Header("Type & Stats")]
    public string enemyType    = "Guard";  // "Guard" or "Soldier"
    public int    hp           = 2;
    public int    attackDamage = 1;

    [Header("Patrol Waypoints")]
    [Tooltip("Parent transform containing waypoint child objects. Their world positions will be rounded to integers.")]
    public Transform waypointParent;

    public Vector2Int GridPos => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

    private int         _wpIndex;
    private Animator    _anim;
    private SpriteRenderer _sr;

    // Tint colours so enemy type is visually distinct
    private static readonly Color ColourGuard   = new Color(0.9f, 0.2f, 0.2f, 1f);  // red
    private static readonly Color ColourSoldier = new Color(0.9f, 0.5f, 0.1f, 1f);  // orange

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        _sr   = GetComponentInChildren<SpriteRenderer>();

        // Tint sprite so player can tell types apart
        if (_sr != null)
            _sr.color = enemyType == "Soldier" ? ColourSoldier : ColourGuard;
    }

    private void Start()
    {
        // Snap to tile grid
        var p = transform.position;
        transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);

        // If the user left the waypoints attached as children, detach them!
        // This locks their world positions so the enemy doesn't 'chase its tail' infinitely.
        if (waypointParent != null && waypointParent.IsChildOf(transform))
        {
            waypointParent.SetParent(null);
        }
    }

    // ── Called once per enemy turn by TurnManager ────────────────────

    public void DoTurnStep()
    {
        if (waypointParent == null || waypointParent.childCount == 0) return;

        bool isChasing = IsPlayerOnPath();

        // If we are currently at the target waypoint (and not chasing), select the next one
        if (!isChasing && GridPos == NextWaypointGrid())
        {
            _wpIndex = (_wpIndex + 1) % waypointParent.childCount;
        }

        Vector2Int target = isChasing && GridTurnPlayer.Instance != null 
            ? GridTurnPlayer.Instance.GridPos 
            : NextWaypointGrid();

        Vector2Int step = CardinalStep(GridPos, target);

        if (step == Vector2Int.zero) return;

        Vector2Int newPos = GridPos + step;

        // If stepping onto player → trigger combat instead of moving
        if (GridTurnPlayer.Instance != null && newPos == GridTurnPlayer.Instance.GridPos)
        {
            CombatHandler.EnemyAttacks(this, GridTurnPlayer.Instance);
            return;
        }

        Face(step);
        StartCoroutine(SlideAnimation(new Vector2(newPos.x, newPos.y)));
    }

    // ── Damage / Death ───────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        hp -= amount;
        DungeonHUD.Instance?.RefreshEnemyHP(this);

        if (hp <= 0) Die();
    }

    private void Die()
    {
        DungeonHUD.Instance?.OnEnemyDied(this);
        gameObject.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private Vector2Int NextWaypointGrid()
    {
        if (waypointParent == null || waypointParent.childCount == 0) return GridPos;
        Vector3 wp = waypointParent.GetChild(_wpIndex).position;
        return new Vector2Int(Mathf.RoundToInt(wp.x), Mathf.RoundToInt(wp.y));
    }

    private bool IsPlayerOnPath()
    {
        if (GridTurnPlayer.Instance == null) return false;
        if (waypointParent == null || waypointParent.childCount < 2) return false;

        Vector2Int p = GridTurnPlayer.Instance.GridPos;
        Vector2Int e = GridPos;

        // Prioritize attacking the player: If the player is right next to the enemy, always attack!
        if (Mathf.Abs(p.x - e.x) + Mathf.Abs(p.y - e.y) <= 1) return true;

        int prevIdx = (_wpIndex - 1 + waypointParent.childCount) % waypointParent.childCount;
        Vector3 wpPrevWorld = waypointParent.GetChild(prevIdx).position;
        Vector2Int wPrev = new Vector2Int(Mathf.RoundToInt(wpPrevWorld.x), Mathf.RoundToInt(wpPrevWorld.y));

        Vector2Int wNext = NextWaypointGrid();

        // If player is standing on the path the enemy just walked from
        if (IsPointOnSegment(p, wPrev, e)) return true;

        // If player is standing on the path the enemy is currently walking towards
        if (IsPointOnSegment(p, e, wNext)) return true;

        return false;
    }

    private bool IsPointOnSegment(Vector2Int p, Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        if (p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY)
        {
            if (a.x == b.x && p.x != a.x) return false;
            if (a.y == b.y && p.y != a.y) return false;
            return true;
        }
        return false;
    }

    private static Vector2Int CardinalStep(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        if (diff == Vector2Int.zero) return Vector2Int.zero;

        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            return new Vector2Int((int)Mathf.Sign(diff.x), 0);
        return new Vector2Int(0, (int)Mathf.Sign(diff.y));
    }

    private IEnumerator SlideAnimation(Vector2 target)
    {
        _anim?.SetBool("isWalking", true);
        Vector2 start = transform.position;
        float dur = 0.1f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(start, target, t / dur);
            yield return null;
        }
        transform.position = target;
        _anim?.SetBool("isWalking", false);
    }

    private void Face(Vector2Int dir)
    {
        if (_anim == null) return;
        _anim.SetFloat("InputX",     dir.x);
        _anim.SetFloat("InputY",     dir.y);
        _anim.SetFloat("LastInputX", dir.x);
        _anim.SetFloat("LastInputY", dir.y);
    }
}
