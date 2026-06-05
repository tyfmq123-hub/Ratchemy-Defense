using UnityEngine;

// BalanceBattleSimulator가 스폰한 유닛에 붙여 생존 시간·사망 시점을 기록합니다.
public class BalanceUnitTracker : MonoBehaviour
{
    public string PrefabName { get; private set; }
    public bool IsPlayerSide { get; private set; }
    public float SpawnTime { get; private set; }
    public float DeathTime { get; private set; } = -1f;

    public bool IsUnitDead
    {
        get
        {
            if (DeathTime >= 0f)
                return true;

            if (IsPlayerSide)
            {
                PlayerUnitBase player = GetComponent<PlayerUnitBase>();
                return player == null || player.IsDead;
            }

            EnemyUnit enemy = GetComponent<EnemyUnit>();
            return enemy == null || enemy.IsDead();
        }
    }

    public void Initialize(string prefabName, bool isPlayerSide)
    {
        PrefabName = prefabName;
        IsPlayerSide = isPlayerSide;
        SpawnTime = Time.time;
    }

    public void MarkDeath()
    {
        if (DeathTime >= 0f)
            return;

        DeathTime = Time.time;
    }

    public float GetSurvivalTime(float roundEndTime)
    {
        float endTime = DeathTime >= 0f ? DeathTime : roundEndTime;
        return Mathf.Max(0f, endTime - SpawnTime);
    }

    public bool IsAliveAt(float roundEndTime)
    {
        return DeathTime < 0f || DeathTime > roundEndTime;
    }

    private void Update()
    {
        if (DeathTime >= 0f)
            return;

        if (IsPlayerSide)
        {
            PlayerUnitBase player = GetComponent<PlayerUnitBase>();
            if (player != null && player.IsDead)
                MarkDeath();
        }
        else
        {
            EnemyUnit enemy = GetComponent<EnemyUnit>();
            if (enemy != null && enemy.IsDead())
                MarkDeath();
        }
    }
}
