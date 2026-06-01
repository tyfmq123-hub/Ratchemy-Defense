using UnityEngine;

/// <summary>
/// 안전관리소장 쥐.
/// 창을 사용하는 기본 유닛.
/// 일반 근접 유닛보다 공격 범위가 조금 길고,
/// 공격속도는 살짝 느리지만 이동속도가 빠릅니다.
/// </summary>
public class SafetyManagerRat : PlayerUnitBase
{
    private LayerMask enemyLayer;
    private float attackCooldown;

    protected override void Awake()
    {
        // 안전관리소장 쥐 스탯
        maxHp = 80f;
        attackPower = 10;
        moveSpeed = 3.5f;      // 다른 유닛보다 빠르게 전진
        attackSpeed = 0.8f;    // 공격속도는 조금 느림
        attackRange = 1.8f;    // 창이라 일반 근접보다 살짝 긴 범위

        base.Awake();

        enemyLayer = LayerMask.GetMask("Enemy");
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
        }

        Collider2D enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);

        if (enemy != null)
        {
            AttackEnemy(enemy);
        }
        else
        {
            Move();
        }
    }

    private void AttackEnemy(Collider2D target)
    {
        if (attackCooldown > 0f)
            return;

        EnemyUnit enemyUnit = target.GetComponent<EnemyUnit>();

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(attackPower);
            Debug.Log($"[SafetyManagerRat] 창 공격 → {target.name}, 데미지: {attackPower}");
        }

        attackCooldown = 1f / attackSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}