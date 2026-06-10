using System;
using UnityEngine;

// 안전관리소장 쥐 — 사거리 내 공격, 없으면 전진 / 달리기 중 받는 데미지 감소 패시브 / 3타마다 단일 적 미침
public class SafetyManagerRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolName = "isattack";
    [SerializeField] private string attackStateName = "SafetyManager_attack";

    [Header("패시브 — 달리기")]
    [SerializeField] private float runDamageReduction = 0.8f;

    [Header("3타 미침")]
    [SerializeField] private int attacksPerFrenzy = 3;
    [SerializeField] private float frenzyKnockbackDistance = 0.35f;
    [SerializeField] private float frenzyKnockbackDuration = 0.12f;
    [SerializeField] private float frenzySkillDamageMultiplier = 2f;

    private LayerMask enemyLayer;
    private float attackCooldown;
    private bool isRunning;
    private int attackComboCount;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        enemyLayer = LayerMask.GetMask("Enemy");
    }

    private void Reset()
    {
        maxHp = 80f;
        currentHp = maxHp;
        attackPower = 10;
        moveSpeed = 3.5f;
        attackSpeed = 0.8f;
        attackRange = 1.8f;
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        EnemyUnit enemy = FindNearestEnemyInRange(attackRange, enemyLayer);

        if (enemy != null)
        {
            isRunning = false;
            SetAttackAnimation(true);
            AttackEnemy(enemy);
        }
        else
        {
            isRunning = true;
            SetAttackAnimation(false);
            Move();
        }
    }

    public override void TakeDamage(float damage)
    {
        if (isRunning)
            damage *= 1f - Mathf.Clamp01(runDamageReduction);

        base.TakeDamage(damage);
    }

    private void AttackEnemy(EnemyUnit enemyUnit)
    {
        if (attackCooldown > 0f)
            return;

        attackComboCount++;

        if (attackComboCount >= attacksPerFrenzy)
        {
            int skillDamage = Mathf.RoundToInt(attackPower * frenzySkillDamageMultiplier);
            enemyUnit.TakeDamage(skillDamage);
            PlayAttackSound();
            TryApplyFrenzyToTarget(enemyUnit);
            attackComboCount = 0;
            Debug.Log($"[SafetyManagerRat] 3타 미침 → {enemyUnit.name}, 데미지: {skillDamage}");
        }
        else
        {
            enemyUnit.TakeDamage(attackPower);
            PlayAttackSound();
            Debug.Log($"[SafetyManagerRat] 창 공격 {attackComboCount}/{attacksPerFrenzy} → {enemyUnit.name}");
        }

        attackCooldown = GetAttackCooldownDuration();
        PlayAttackFromStart();
    }

    private void TryApplyFrenzyToTarget(EnemyUnit enemy)
    {
        if (enemy == null || enemy.IsDead())
            return;

        if (HasBossScript(enemy.gameObject))
            return;

        EnemyFrenzyEffect.Apply(enemy.gameObject, frenzyKnockbackDistance, frenzyKnockbackDuration);
    }

    private static bool HasBossScript(GameObject target)
    {
        MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            if (behaviour.GetType().Name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void SetAttackAnimation(bool isAttacking)
    {
        if (animator == null || string.IsNullOrEmpty(attackBoolName))
            return;

        animator.SetBool(attackBoolName, isAttacking);
    }

    private void PlayAttackFromStart()
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(attackBoolName))
            animator.SetBool(attackBoolName, true);

        if (!string.IsNullOrEmpty(attackStateName))
            animator.Play(attackStateName, 0, 0f);
    }

    public override bool HasSkillCooldown => true;

    public override float SkillCooldownFill =>
        attacksPerFrenzy <= 0 ? 1f : (float)attackComboCount / attacksPerFrenzy;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
