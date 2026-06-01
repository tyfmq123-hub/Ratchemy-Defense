using System.Collections;
using UnityEngine;

public class CoolantRat : PlayerUnitBase
{
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

        Collider2D target = FindNearestEnemy();

        if (target != null)
        {
            if (skillCooldownTimer <= 0f)
                UseSkill();
            else
                BasicAttack(target);
        }
        else
        {
            Move();
        }
    }

    private Collider2D FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (hits.Length == 0)
            return null;

        Collider2D nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = hit;
            }
        }

        return nearestEnemy;
    }

    private void BasicAttack(Collider2D target)
    {
        if (attackCooldown > 0f)
            return;

        EnemyUnit enemyUnit = target.GetComponent<EnemyUnit>();

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(attackPower);
            Debug.Log($"[CoolantRat] 기본 원거리 공격 → {target.name}, 데미지: {attackPower}");
        }

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

            if (enemyUnit != null)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}