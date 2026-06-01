using System.Collections;
using UnityEngine;

// 궁극 쥐(Ultimate Rat) — PlayerUnitBase 상속
// - 기본: 사거리 내 근접 공격, 적 없으면 전진
// - 스킬: 15초마다 맵에 있는 모든 EnemyUnit에게 attackPower 데미지를 2회
public class UltimateRat : PlayerUnitBase
{
    private LayerMask enemyLayer;
    private float attackCooldown;
    private float skillCooldownTimer;
    private bool isCastingSkill;

    [Header("궁극 스킬")]
    [SerializeField] private float skillCooldown = 15f;   // 스킬 재사용 대기(초)
    [SerializeField] private int skillHitCount = 2;         // 타격 횟수 (공격력 × 2회)
    [SerializeField] private float skillHitInterval = 0.2f; // 1타·2타 사이 간격(초)

    protected override void Awake()
    {
        base.Awake();
        enemyLayer = LayerMask.GetMask("Enemy");
        skillCooldownTimer = skillCooldown; // 첫 스킬은 15초 후 (즉시 발동 방지)
    }

    private void Reset()
    {
        maxHp = 120f;
        currentHp = maxHp;
        attackPower = 18;
        moveSpeed = 2.5f;
        attackSpeed = 1.5f;
        attackRange = 1.2f;
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        // 사거리와 무관하게 15초마다 전장 스킬 (이동 중에도 발동)
        if (skillCooldownTimer <= 0f && !isCastingSkill)
            StartCoroutine(UseMapWideUltimate());

        Collider2D enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);

        if (enemy != null)
            Attack(enemy);
        else
            Move();
    }

    private void Attack(Collider2D target)
    {
        if (attackCooldown > 0f)
            return;

        EnemyUnit enemyUnit = target.GetComponent<EnemyUnit>();

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(attackPower);
            Debug.Log($"[UltimateRat] {target.name} 공격 → 데미지: {attackPower}");
        }

        attackCooldown = 1f / attackSpeed;
    }

    // 맵 전체 EnemyUnit에게 attackPower를 skillHitCount번 입힘
    private IEnumerator UseMapWideUltimate()
    {
        isCastingSkill = true;
        skillCooldownTimer = skillCooldown;

        for (int hit = 0; hit < skillHitCount; hit++)
        {
            EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>();
            int damagedCount = 0;

            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy == null || enemy.IsDead())
                    continue;

                enemy.TakeDamage(attackPower);
                damagedCount++;
            }

            Debug.Log($"[UltimateRat] 궁극 스킬 {hit + 1}/{skillHitCount}타 → {damagedCount}명, 데미지 {attackPower}");

            if (hit < skillHitCount - 1 && skillHitInterval > 0f)
                yield return new WaitForSeconds(skillHitInterval);
        }

        isCastingSkill = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
