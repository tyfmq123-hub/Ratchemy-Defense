using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 냉각수 쥐 — 적 추적 점사 어택 + 스킬 시 이펙트 연사
public class CoolantRat : PlayerUnitBase
{
    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string idleBoolName = "isidle";

    [Header("공격")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private Transform attackEffectSpawnPoint;
    [SerializeField] private int attackProjectileCount = 3;
    [SerializeField] private float attackProjectileSpeed = 6f;
    [SerializeField] private float attackBurstInterval = 0.12f;

    [Header("스킬")]
    [SerializeField] private float skillRange = 4f;
    [SerializeField] private int skillTargetCount = 3;
    [SerializeField] private int skillProjectileDamage = 8;
    [SerializeField] private float skillDuration = 5f;
    [SerializeField] private float skillFireRate = 5f;
    [SerializeField] private float skillCooldown = 15f;
    [SerializeField] private float debuffDuration = 3f;

    private float attackCooldown;
    private float skillCooldownTimer;
    private bool isBurstAttacking;
    private bool isSkillFiring;
    private Coroutine burstAttackCoroutine;
    private readonly List<EnemyUnit> skillTargetBuffer = new List<EnemyUnit>();

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyLayer.value == 0)
            enemyLayer = LayerMask.GetMask("Enemy");

        skillCooldownTimer = skillCooldown;
        ResolveAttackEffectSpawnPoint();
    }

    private void ResolveAttackEffectSpawnPoint()
    {
        Transform spawnChild = transform.Find("Attack Effect Spawn Point");
        if (spawnChild == null)
            spawnChild = transform.Find("Skill Effect Spawn Point");

        if (spawnChild != null)
            attackEffectSpawnPoint = spawnChild;
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

        EnemyUnit target = FindFrontEnemyInRange(attackRange);
        if (target == null)
        {
            SetCombatIdle(false);
            Move();
            return;
        }

        if (isSkillFiring)
        {
            SetCombatIdle(true);
            return;
        }

        if (skillCooldownTimer <= 0f)
        {
            SetCombatIdle(false);
            BeginSkillBarrage();
            return;
        }

        if (isBurstAttacking)
        {
            SetCombatIdle(true);
            return;
        }

        if (attackCooldown > 0f)
            SetCombatIdle(true);
        else
        {
            SetCombatIdle(false);
            BasicAttack();
        }
    }

    private void TickCooldowns()
    {
        if (!isSkillFiring && attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;
    }

    private void BasicAttack()
    {
        if (attackCooldown > 0f || isBurstAttacking || isSkillFiring)
            return;

        burstAttackCoroutine = StartCoroutine(FireBurstAttack());
    }

    private void BeginSkillBarrage()
    {
        CancelBurstAttack();

        isSkillFiring = true;
        skillCooldownTimer = skillCooldown;
        StartCoroutine(FireSkillBarrage());
    }

    private void CancelBurstAttack()
    {
        if (burstAttackCoroutine != null)
        {
            StopCoroutine(burstAttackCoroutine);
            burstAttackCoroutine = null;
        }

        isBurstAttacking = false;
    }

    private IEnumerator FireBurstAttack()
    {
        isBurstAttacking = true;
        attackCooldown = GetAttackCooldownDuration();

        if (attackEffectPrefab == null)
        {
            isBurstAttacking = false;
            yield break;
        }

        ResolveAttackEffectSpawnPoint();

        Transform spawnPoint = attackEffectSpawnPoint != null ? attackEffectSpawnPoint : transform;
        Vector3 spawnPosition = spawnPoint.position;

        int shotCount = Mathf.Max(attackProjectileCount, 1);
        float interval = Mathf.Max(attackBurstInterval, 0f);

        for (int shot = 0; shot < shotCount; shot++)
        {
            if (IsDead)
                break;

            EnemyUnit target = FindFrontEnemyInRange(attackRange);
            if (target == null)
                break;

            FireAttackTrigger();
            SpawnAttackProjectile(spawnPosition, target, attackPower);

            if (shot < shotCount - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        isBurstAttacking = false;
        burstAttackCoroutine = null;
    }

    private IEnumerator FireSkillBarrage()
    {
        ApplySkillDebuffsInRange();

        if (attackEffectPrefab == null)
        {
            isSkillFiring = false;
            yield break;
        }

        ResolveAttackEffectSpawnPoint();

        float duration = Mathf.Max(skillDuration, 0f);
        float fireRate = Mathf.Max(skillFireRate, 0.01f);
        float shotInterval = 1f / fireRate;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsDead)
                break;

            EnemyUnit target = FindFrontEnemyInRange(skillRange);
            if (target != null)
            {
                Transform spawnPoint = attackEffectSpawnPoint != null ? attackEffectSpawnPoint : transform;
                FireAttackTrigger();
                SpawnAttackProjectile(spawnPoint.position, target, skillProjectileDamage);
            }

            yield return new WaitForSeconds(shotInterval);
            elapsed += shotInterval;
        }

        isSkillFiring = false;
        attackCooldown = GetAttackCooldownDuration();
    }

    private void SpawnAttackProjectile(Vector3 spawnPosition, EnemyUnit target, int damage)
    {
        if (target == null || target.IsDead())
            return;

        GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, Quaternion.identity);

        CoolantAttackProjectile projectile = effect.GetComponent<CoolantAttackProjectile>();
        if (projectile == null)
        {
            Destroy(effect, 1.5f);
            return;
        }

        projectile.Initialize(damage, enemyLayer, target.transform, attackProjectileSpeed);

        SpriteRenderer unitSprite = GetComponent<SpriteRenderer>();
        if (unitSprite != null)
            projectile.ApplySortingOrder(unitSprite.sortingOrder + 1);
    }

    private void ApplySkillDebuffsInRange()
    {
        FillSkillTargetsByDistance(skillTargetCount);

        foreach (EnemyUnit enemy in skillTargetBuffer)
        {
            if (!enemy.TryGetComponent(out FlameSlime flameSlime))
                continue;

            StartCoroutine(ApplyExplosionDisableDebuff(flameSlime));
        }
    }

    private void FillSkillTargetsByDistance(int maxCount)
    {
        skillTargetBuffer.Clear();
        CollectEnemiesInRange(skillRange, skillTargetBuffer);

        skillTargetBuffer.Sort(CompareEnemyFrontPriority);

        if (skillTargetBuffer.Count > maxCount)
            skillTargetBuffer.RemoveRange(maxCount, skillTargetBuffer.Count - maxCount);
    }

    private EnemyUnit FindFrontEnemyInRange(float range)
    {
        EnemyUnit frontEnemy = null;
        float frontX = float.MinValue;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
            if (enemy == null || enemy.IsDead())
                continue;

            float enemyX = enemy.transform.position.x;
            if (enemyX > frontX)
            {
                frontX = enemyX;
                frontEnemy = enemy;
            }
        }

        return frontEnemy;
    }

    private void CollectEnemiesInRange(float range, List<EnemyUnit> buffer)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
            if (enemy == null || enemy.IsDead())
                continue;

            buffer.Add(enemy);
        }
    }

    private int CompareEnemyFrontPriority(EnemyUnit a, EnemyUnit b)
    {
        return b.transform.position.x.CompareTo(a.transform.position.x);
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
