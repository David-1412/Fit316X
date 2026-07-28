using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// In-game HUD for the turn-based dungeon:
///   • "YOUR TURN / ENEMY TURN" banner
///   • Player HP display
///   • Pellet counter
///   • Combat log (auto-hides after 2.5s)
///   • Game Over / Victory panels
/// Created programmatically by BuildDungeonFloor2.cs — no manual setup needed.
/// </summary>
public class DungeonHUD : MonoBehaviour
{
    public static DungeonHUD Instance { get; private set; }

    // Assigned by BuildDungeonFloor2 after the Canvas is constructed
    [HideInInspector] public TextMeshProUGUI turnText;
    [HideInInspector] public TextMeshProUGUI hpText;
    [HideInInspector] public TextMeshProUGUI pelletText;
    [HideInInspector] public TextMeshProUGUI combatLogText;
    [HideInInspector] public GameObject gameOverPanel;
    [HideInInspector] public GameObject victoryPanel;

    // Enemy HP labels (world-space, above each enemy head)
    private readonly Dictionary<GridEnemy, TextMeshPro> _enemyLabels
        = new Dictionary<GridEnemy, TextMeshPro>();

    private Coroutine _logRoutine;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var tm = TurnManager.Instance;
        if (tm != null)
        {
            tm.OnPlayerTurnStart.AddListener(ShowPlayerTurn);
            tm.OnEnemyTurnStart.AddListener(ShowEnemyTurn);
            tm.OnGameEnd.AddListener(HandleGameEnd);
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel)  victoryPanel.SetActive(false);
        if (combatLogText) combatLogText.gameObject.SetActive(false);

        RefreshHP();
        RefreshPellets(0, 0);
        ShowPlayerTurn();
    }

    // ── Turn indicator ───────────────────────────────────────────────

    public void ShowPlayerTurn()
    {
        if (turnText == null) return;
        turnText.text  = "YOUR TURN";
        turnText.color = new Color(0.3f, 1f, 0.4f);  // green
    }

    public void ShowEnemyTurn()
    {
        if (turnText == null) return;
        turnText.text  = "ENEMY TURN";
        turnText.color = new Color(1f, 0.3f, 0.3f);  // red
    }

    // ── HP ───────────────────────────────────────────────────────────

    public void RefreshHP()
    {
        if (hpText == null || GridTurnPlayer.Instance == null) return;
        int cur = GridTurnPlayer.Instance.CurrentHp;
        int max = GridTurnPlayer.Instance.maxHp;
        hpText.text = "HP " + cur + " / " + max;
        hpText.color = cur <= 1
            ? new Color(1f, 0.2f, 0.2f)
            : new Color(1f, 0.8f, 0.8f);
    }

    // ── Pellet counter ───────────────────────────────────────────────

    public void RefreshPellets(int collected, int total)
    {
        if (pelletText == null) return;
        pelletText.text = total > 0
            ? $"⬤ {collected} / {total}"
            : "";
    }

    // ── Combat log ───────────────────────────────────────────────────

    public void ShowCombatLog(string msg)
    {
        if (combatLogText == null) return;
        if (_logRoutine != null) StopCoroutine(_logRoutine);
        _logRoutine = StartCoroutine(ShowLogFor(msg, 2.5f));
    }

    private IEnumerator ShowLogFor(string msg, float seconds)
    {
        combatLogText.text = msg;
        combatLogText.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        combatLogText.gameObject.SetActive(false);
    }

    // ── Enemy HP labels ──────────────────────────────────────────────

    public void RefreshEnemyHP(GridEnemy enemy)
    {
        if (!_enemyLabels.TryGetValue(enemy, out var label))
        {
            label = CreateWorldLabel(enemy.gameObject);
            _enemyLabels[enemy] = label;
        }
        if (label == null) return;

        label.text  = new string('▮', Mathf.Max(0, enemy.hp));
        label.color = Color.red;
    }

    public void OnEnemyDied(GridEnemy enemy)
    {
        if (_enemyLabels.TryGetValue(enemy, out var label))
        {
            if (label != null) Destroy(label.gameObject);
            _enemyLabels.Remove(enemy);
        }
    }

    private TextMeshPro CreateWorldLabel(GameObject parent)
    {
        var go = new GameObject("EnemyHP_Label");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = new Vector3(0f, 0.7f, -0.1f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize       = 3f;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.sortingOrder   = 10;
        return tmp;
    }

    // ── Win / Lose ───────────────────────────────────────────────────

    private void HandleGameEnd(bool won)
    {
        if (won  && victoryPanel)  victoryPanel.SetActive(true);
        if (!won && gameOverPanel) gameOverPanel.SetActive(true);
    }

    // ── Button callbacks (wired in builder) ──────────────────────────

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMain()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
