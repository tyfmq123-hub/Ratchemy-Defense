using System;
using System.Collections;
using UnityEngine;

// 탱커 쥐 — 근접 기본 공격 + 범위 밀치기 스킬
public class TankRat : PlayerUnitBase
{
    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string skillTriggerName = "skill";
    [SerializeField] private string idleBoolName = "isidle";

    private LayerMask enemyLayer;
    private float attackCooldown;
    private float skillCooldownTimer;
    private bool isCastingSkill;
    private bool skillEventReceived;

    [Header("스킬 설정")]
    [SerializeField] private float skillCooldown = 5f;
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [SerializeField] private float skillRange = 1.6f;
    [SerializeField] private int skillDamage = 3;

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
        maxHp = 220f;
        currentHp = maxHp;
        attackPower = 4;
        moveSpeed = 2.2f;
        attackSpeed = 0.7f;
        attackRange = 1.1f;
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        if (isCastingSkill)
            return;

        EnemyUnit enemy = FindNearestEnemyInRange(attackRange, enemyLayer);

        if (enemy == null)
        {
            SetCombatIdle(false);
            Move();
            return;
        }

        if (skillCooldownTimer <= 0f)
        {
            SetCombatIdle(false);
            StartCoroutine(CastKnockbackSkillByEvent());
            return;
        }

        // 교전 중 + 스킬 쿨타임 동안 idle
        SetCombatIdle(true);
        BasicAttack(enemy);
    }

    protected override void Die()
    {
        SetCombatIdle(false);
        isCastingSkill = false;
        base.Die();
    }

    private void BasicAttack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;

        target.TakeDamage(attackPower);
        Debug.Log($"[TankRat] 기본 공격 → {target.name}, 데미지: {attackPower}");
        attackCooldown = GetAttackCooldownDuration();
    }

    private IEnumerator CastKnockbackSkillByEvent()
    {
        isCastingSkill = true;
        skillEventReceived = false;
        skillCooldownTimer = skillCooldown;
        SetCombatIdle(false);
        FireSkillTrigger();

        float timeout = 2.5f;
        while (!skillEventReceived && timeout > 0f)
        {
            if (IsDead)
            {
                EndSkillCast();
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        if (IsDead)
        {
            EndSkillCast();
            yield break;
        }

        if (!skillEventReceived)
            Debug.LogWarning("[TankRat] Skill Animation Event 미수신. 이벤트를 확인해 주세요.");

        ApplyKnockbackSkillDamage();
        EndSkillCast();
    }

    private void EndSkillCast()
    {
        isCastingSkill = false;
    }

    public void OnSkillAnimationEvent()
    {
        if (!isCastingSkill || IsDead)
            return;

        skillEventReceived = true;
    }

    private void ApplyKnockbackSkillDamage()
    {
        if (IsDead)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();
            if (enemyUnit == null || enemyUnit.IsDead())
                continue;

            enemyUnit.TakeDamage(skillDamage);

            if (CanBeKnockedBack(hit, enemyUnit))
                StartCoroutine(KnockbackEnemy(hit.transform));

            Debug.Log($"[TankRat] 밀치기 스킬 → {hit.name}");
        }
    }

    private void FireSkillTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(skillTriggerName))
            return;

        animator.ResetTrigger(skillTriggerName);
        animator.SetTrigger(skillTriggerName);
    }

    private void SetCombatIdle(bool isIdle)
    {
        if (animator == null || string.IsNullOrEmpty(idleBoolName))
            return;

        animator.SetBool(idleBoolName, isIdle);
    }

    private bool CanBeKnockedBack(Collider2D hit, EnemyUnit enemy)
    {
        if (HasBossScript(enemy.gameObject))
            return false;

        if (hit.gameObject != enemy.gameObject && HasBossScript(hit.gameObject))
            return false;

        return true;
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

    private IEnumerator KnockbackEnemy(Transform enemy)
    {
        if (enemy == null)
            yield break;

        Vector3 startPos = enemy.position;
        Vector3 endPos = startPos + Vector3.right * knockbackDistance;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (enemy == null)
                yield break;

            elapsed += Time.deltaTime;
            enemy.position = Vector3.Lerp(startPos, endPos, elapsed / knockbackDuration);
            yield return null;
        }

        if (enemy != null)
            enemy.position = endPos;
    }

    public override bool HasSkillCooldown => true;

    public override float SkillCooldownFill =>
        skillCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(skillCooldownTimer / skillCooldown);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}
