using System.Collections;
using UnityEngine;

// 궁극 쥐 — 사거리 내 근접 공격, 맵 전체 궁극 스킬
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
    [SerializeField] private float skillCooldown = 15f;
    [SerializeField] private int skillHitCount = 2;
    [SerializeField] private float skillHitInterval = 0.2f;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        enemyLayer = LayerMask.GetMask("Enemy");
        skillCooldownTimer = skillCooldown;
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
        if (IsDead)
            return;

        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        if (skillCooldownTimer <= 0f && !isCastingSkill && HasAliveEnemy())
            StartCoroutine(UseMapWideUltimate());

        if (isCastingSkill)
            return;

        EnemyUnit enemy = FindNearestEnemyInRange(attackRange, enemyLayer);

        if (enemy != null)
            Attack(enemy);
        else
            Move();
    }

    private bool HasAliveEnemy()
    {
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);

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

        if (target == null || target.IsDead())
            return;

        FireAttackTrigger();
        target.TakeDamage(attackPower);
        Debug.Log($"[UltimateRat] {target.name} 공격 → 데미지: {attackPower}");
        attackCooldown = GetAttackCooldownDuration();
    }

    private IEnumerator UseMapWideUltimate()
    {
        isCastingSkill = true;
        skillCooldownTimer = skillCooldown;
        FireSkillTrigger();

        int hitCount = Mathf.Max(skillHitCount, 1);
        float interval = Mathf.Max(skillHitInterval, 0f);

        for (int hit = 0; hit < hitCount; hit++)
        {
            if (IsDead)
            {
                isCastingSkill = false;
                yield break;
            }

            EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
            int damagedCount = 0;

            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy == null || enemy.IsDead())
                    continue;

                enemy.TakeDamage(attackPower);
                damagedCount++;
            }

            Debug.Log($"[UltimateRat] 궁극 스킬 {hit + 1}/{hitCount}타 → {damagedCount}명, 데미지 {attackPower}");

            if (hit < hitCount - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        isCastingSkill = false;
    }

    private void FireAttackTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(attackTriggerName))
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

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
