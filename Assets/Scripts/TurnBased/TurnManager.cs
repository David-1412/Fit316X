using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the turn cycle for Dungeon_Floor2:
///   PlayerTurn → wait for WASD → EnemyTurn → all enemies step → repeat
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum TurnState { PlayerTurn, EnemyTurn, GameOver, Victory }
    public TurnState State { get; private set; } = TurnState.PlayerTurn;

    [Header("Timing")]
    [Tooltip("Pause between each enemy's move animation")]
    public float enemyStepDelay = 0.12f;

    [Header("Events — wire these in the Inspector or via DungeonSetup")]
    public UnityEvent OnPlayerTurnStart;
    public UnityEvent OnEnemyTurnStart;
    public UnityEvent<bool> OnGameEnd;   // true = victory

    private GridEnemy[] _enemies;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        RefreshEnemyList();
        BeginPlayerTurn();
    }

    // ── Called by GridTurnPlayer after it finishes moving ────────────
    public void PlayerTurnDone()
    {
        if (State != TurnState.PlayerTurn) return;
        StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        State = TurnState.EnemyTurn;
        OnEnemyTurnStart?.Invoke();

        RefreshEnemyList();

        int playerRoom = 1;
        if (GridTurnPlayer.Instance != null)
            playerRoom = GetRoomIndex(GridTurnPlayer.Instance.transform.position.y);

        foreach (var e in _enemies)
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;

            // Skip enemies that are not in the current room
            if (GetRoomIndex(e.transform.position.y) != playerRoom) continue;

            e.DoTurnStep();
            yield return new WaitForSeconds(enemyStepDelay);
        }

        if (State != TurnState.GameOver)
            BeginPlayerTurn();
    }

    private int GetRoomIndex(float y)
    {
        if (y >= 34f) return 3;
        if (y >= 16f) return 2;
        return 1;
    }

    public void BeginPlayerTurn()
    {
        State = TurnState.PlayerTurn;
        OnPlayerTurnStart?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (State == TurnState.GameOver) return;
        State = TurnState.GameOver;
        OnGameEnd?.Invoke(false);
    }

    public void TriggerVictory()
    {
        State = TurnState.Victory;
        OnGameEnd?.Invoke(true);
    }

    public void RefreshEnemyList()
        => _enemies = FindObjectsByType<GridEnemy>(FindObjectsInactive.Exclude);
}
