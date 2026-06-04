using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 냉각수 쥐 — 원거리 기본 공격 + 범위 스킬(가까운 적 우선, 최대 3명)
public class CoolantRat : PlayerUnitBase
{
    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string idleBoolName = "isidle";

    [Header("공격")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float skillRange = 4f;
    [SerializeField] private int skillTargetCount = 3;

    [Header("스킬")]
    [SerializeField] private int skillDamage = 8;
    [SerializeField] private float skillCooldown = 6f;
    [SerializeField] private float debuffDuration = 3f;

    private float attackCooldown;
    private float skillCooldownTimer;
    private readonly List<EnemyUnit> skillTargetBuffer = new List<EnemyUnit>();

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyLayer.value == 0)
            enemyLayer = LayerMask.GetMask("Enemy");
    }

    private void Reset()
    {
        maxHp = 60f;
        currentHp = maxHp;
        attackPower = 8;
        moveSpeed = 2.2f;
        attackSpeed = 1f;
        attackRange = 3.5f;
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        TickCooldowns();

        EnemyUnit target = FindNearestEnemyInRange(attackRange, enemyLayer);
        if (target == null)
        {
            SetCombatIdle(false);
            Move();
            return;
        }

        if (skillCooldownTimer <= 0f)
        {
            SetCombatIdle(false);
            FireAttackTrigger();
            UseSkillOnNearestTargets();
            return;
        }

        if (attackCooldown > 0f)
            SetCombatIdle(true);
        else
        {
            SetCombatIdle(false);
            FireAttackTrigger();
            BasicAttack(target);
        }
    }

    private void TickCooldowns()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;
    }

    private void BasicAttack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;

        target.TakeDamage(attackPower);
        Debug.Log($"[CoolantRat] 기본 원거리 공격 → {target.name}, 데미지: {attackPower}");
        attackCooldown = GetAttackCooldownDuration();
    }

    private void UseSkillOnNearestTargets()
    {
        FillSkillTargetsByDistance(skillTargetCount);

        foreach (EnemyUnit enemy in skillTargetBuffer)
        {
            enemy.TakeDamage(skillDamage);
            Debug.Log($"[CoolantRat] 냉각 범위 스킬 → {enemy.name}, 데미지: {skillDamage}");

            if (enemy.TryGetComponent(out FlameSlime flameSlime))
                StartCoroutine(ApplyExplosionDisableDebuff(flameSlime));
        }

        skillCooldownTimer = skillCooldown;
    }

    private void FillSkillTargetsByDistance(int maxCount)
    {
        skillTargetBuffer.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
            if (enemy != null && !enemy.IsDead())
                skillTargetBuffer.Add(enemy);
        }

        skillTargetBuffer.Sort(CompareEnemyDistance);

        if (skillTargetBuffer.Count > maxCount)
            skillTargetBuffer.RemoveRange(maxCount, skillTargetBuffer.Count - maxCount);
    }

    private int CompareEnemyDistance(EnemyUnit a, EnemyUnit b)
    {
        float aSqr = (a.transform.position - transform.position).sqrMagnitude;
        float bSqr = (b.transform.position - transform.position).sqrMagnitude;
        return aSqr.CompareTo(bSqr);
    }

    private IEnumerator ApplyExplosionDisableDebuff(FlameSlime flameSlime)
    {
        flameSlime.ApplyDebuff(FlameSlimeDebuff.ExplosionDisabled);
        yield return new WaitForSeconds(debuffDuration);

        if (flameSlime != null)
            flameSlime.RemoveDebuff(FlameSlimeDebuff.ExplosionDisabled);
    }

    private void SetCombatIdle(bool isIdle)
    {
        if (animator == null || string.IsNullOrEmpty(idleBoolName))
            return;

        animator.SetBool(idleBoolName, isIdle);
    }

    private void FireAttackTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(attackTriggerName))
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}
