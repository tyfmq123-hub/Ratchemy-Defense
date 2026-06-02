using System.Collections;
using UnityEngine;

public class CoolantRat : PlayerUnitBase
{
    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolName = "isattack";
    [SerializeField] private string idleBoolName = "isidle";

    [Header("공격 설정")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float skillRange = 4f;
    [SerializeField] private int skillTargetCount = 3;

    [Header("스킬 설정")]
    [SerializeField] private int skillDamage = 8;        // 스킬 데미지 (EnemyUnit.TakeDamage int)
    [SerializeField] private float skillCooldown = 6f;
    [SerializeField] private float debuffDuration = 3f;

    private float attackCooldown;
    private float skillCooldownTimer;

    protected override void Awake()
    {
        base.Awake();
        if (animator == null)
            animator = GetComponent<Animator>();
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
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        EnemyUnit target = FindNearestEnemy();

        if (target != null)
        {
            if (skillCooldownTimer <= 0f)
            {
                SetCombatAnimation(isAttacking: true, isIdle: false);
                UseSkill();
            }
            else
            {
                // 기본 공격 쿨 대기 중이면 Idle 유지, 쿨이 끝났으면 공격
                bool waitingAttackDelay = attackCooldown > 0f;
                if (waitingAttackDelay)
                {
                    SetCombatAnimation(isAttacking: false, isIdle: true);
                }
                else
                {
                    SetCombatAnimation(isAttacking: true, isIdle: false);
                    BasicAttack(target);
                }
            }
        }
        else
        {
            SetCombatAnimation(isAttacking: false, isIdle: false);
            Move();
        }
    }

    private EnemyUnit FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (hits.Length == 0)
            return null;

        EnemyUnit nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();
            if (enemyUnit == null || enemyUnit.IsDead())
                continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemyUnit;
            }
        }

        return nearestEnemy;
    }

    private void BasicAttack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;

        target.TakeDamage(attackPower);
        Debug.Log($"[CoolantRat] 기본 원거리 공격 → {target.name}, 데미지: {attackPower}");

        attackCooldown = 1f / attackSpeed;
    }

    private void UseSkill()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);

        int hitCount = 0;

        foreach (Collider2D hit in hits)
        {
            if (hitCount >= skillTargetCount)
                break;

            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();

            if (enemyUnit != null && !enemyUnit.IsDead())
            {
                enemyUnit.TakeDamage(skillDamage);
                Debug.Log($"[CoolantRat] 냉각 범위 스킬 → {hit.name}, 데미지: {skillDamage}");

                FlameSlime flameSlime = hit.GetComponent<FlameSlime>();

                if (flameSlime != null)
                {
                    StartCoroutine(ApplyExplosionDisableDebuff(flameSlime));
                }

                hitCount++;
            }
        }

        skillCooldownTimer = skillCooldown;
    }

    private IEnumerator ApplyExplosionDisableDebuff(FlameSlime flameSlime)
    {
        flameSlime.ApplyDebuff(FlameSlimeDebuff.ExplosionDisabled);

        yield return new WaitForSeconds(debuffDuration);

        if (flameSlime != null)
        {
            flameSlime.RemoveDebuff(FlameSlimeDebuff.ExplosionDisabled);
        }
    }

    // 전투 상태에 맞춰 공격/대기 bool을 제어
    private void SetCombatAnimation(bool isAttacking, bool isIdle)
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(attackBoolName))
            animator.SetBool(attackBoolName, isAttacking);

        if (!string.IsNullOrEmpty(idleBoolName))
            animator.SetBool(idleBoolName, isIdle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}