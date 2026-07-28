using UnityEngine;

/// <summary>
/// Resolves combat between the player and an enemy.
/// Combat is one-sided: the entity moving into the other's square deals the damage.
/// </summary>
public static class CombatHandler
{
    public static void PlayerAttacks(GridTurnPlayer player, GridEnemy enemy)
    {
        if (player == null || enemy == null) return;

        float critRate = PlayerEquipment.Instance != null ? PlayerEquipment.Instance.CritRate : 0.05f;
        float critDmgMult = PlayerEquipment.Instance != null ? PlayerEquipment.Instance.CritDamage : 1.5f;

        bool isCrit = Random.value < critRate;
        int finalDamage = isCrit ? Mathf.RoundToInt(player.attackDamage * critDmgMult) : player.attackDamage;

        enemy.TakeDamage(finalDamage);

        string log;
        if (enemy.hp <= 0)
        {
            log = $"<color=#88FF88>You defeated the {enemy.enemyType}!</color>";
        }
        else
        {
            if (isCrit)
                log = $"<color=#FFD700>CRITICAL HIT! Hit {enemy.enemyType} for {finalDamage}!</color>";
            else
                log = $"<color=#88FF88>Hit {enemy.enemyType} for {finalDamage}.</color>";
        }

        DungeonHUD.Instance?.ShowCombatLog(log);
    }

    public static void EnemyAttacks(GridEnemy enemy, GridTurnPlayer player)
    {
        if (player == null || enemy == null) return;

        player.TakeDamage(enemy.attackDamage);

        string log = $"<color=#FF4444>{enemy.enemyType} hits you for {enemy.attackDamage}!</color>";
        DungeonHUD.Instance?.ShowCombatLog(log);
    }
}
