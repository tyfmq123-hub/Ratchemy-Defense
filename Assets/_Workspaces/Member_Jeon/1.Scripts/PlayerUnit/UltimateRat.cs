using System.Collections;
using UnityEngine;

// 궁극 쥐(Ultimate Rat) — PlayerUnitBase 상속
// - 기본: 사거리 내 근접 공격, 적 없으면 전진
// - 스킬: 15초마다 맵에 있는 모든 EnemyUnit에게 attackPower 데미지를 2회
public class UltimateRat : PlayerUnitBase
{
    private LayerMask enemyLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string skillTriggerName = "skill";
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
        if (animator == null)
            animator = GetComponent<Animator>();
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

        // 전장에 살아있는 적이 있을 때만 스킬 사용
        if (skillCooldownTimer <= 0f && !isCastingSkill && HasAliveEnemy())
            StartCoroutine(UseMapWideUltimate());

        // 스킬 애니메이션 재생 중에는 기본 공격/이동 중지
        if (isCastingSkill)
            return;

        EnemyUnit enemy = GetEnemyInAttackRange();

        if (enemy != null)
            Attack(enemy);
        else
            Move();
    }

    private EnemyUnit GetEnemyInAttackRange()
    {
        Collider2D collider = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);
        if (collider == null)
            return null;

        EnemyUnit enemyUnit = collider.GetComponent<EnemyUnit>();
        if (enemyUnit == null || enemyUnit.IsDead())
            return null;

        return enemyUnit;
    }

    private bool HasAliveEnemy()
    {
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>();
        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
                return true;
        }

        return false;
    }

    private void Attack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;

        FireAttackTrigger();
        target.TakeDamage(attackPower);
        Debug.Log($"[UltimateRat] {target.name} 공격 → 데미지: {attackPower}");

        attackCooldown = 1f / attackSpeed;
    }

    // 맵 전체 EnemyUnit에게 attackPower를 skillHitCount번 입힘
    private IEnumerator UseMapWideUltimate()
    {
        isCastingSkill = true;
        skillCooldownTimer = skillCooldown;
        FireSkillTrigger();

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

    // 한 번의 공격 애니메이션만 재생되도록 트리거 발동
    private void FireAttackTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(attackTriggerName))
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

    // 한 번의 스킬 애니메이션만 재생되도록 트리거 발동
    private void FireSkillTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(skillTriggerName))
            return;

        animator.ResetTrigger(skillTriggerName);
        animator.SetTrigger(skillTriggerName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
