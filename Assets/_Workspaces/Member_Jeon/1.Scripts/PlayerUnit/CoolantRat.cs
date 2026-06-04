using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 냉각수 쥐 — 정면 3연속 점사 어택 + 스킬 시 이펙트 연사
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
    [SerializeField] private Vector2 attackDirection = Vector2.right;
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
    private readonly List<EnemyUnit> skillTargetBuffer = new List<EnemyUnit>();

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyLayer.value == 0)
            enemyLayer = LayerMask.GetMask("Enemy");

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

        EnemyUnit target = FindNearestEnemyInRange(attackRange, enemyLayer);
        if (target == null)
        {
            SetCombatIdle(false);
            Move();
            return;
        }

        if (isBurstAttacking || isSkillFiring)
        {
            SetCombatIdle(true);
            return;
        }

        if (skillCooldownTimer <= 0f)
        {
            SetCombatIdle(false);
            StartCoroutine(FireSkillBarrage());
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
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;
    }

    private void BasicAttack()
    {
        if (attackCooldown > 0f || isBurstAttacking || isSkillFiring)
            return;

        StartCoroutine(FireBurstAttack());
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
        Vector2 direction = attackDirection.sqrMagnitude > 0.0001f
            ? attackDirection.normalized
            : Vector2.right;

        int shotCount = Mathf.Max(attackProjectileCount, 1);
        float interval = Mathf.Max(attackBurstInterval, 0f);

        for (int shot = 0; shot < shotCount; shot++)
        {
            if (IsDead)
                break;

            FireAttackTrigger();
            SpawnAttackProjectile(spawnPosition, direction, attackPower);

            if (shot < shotCount - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        isBurstAttacking = false;
    }

    private IEnumerator FireSkillBarrage()
    {
        isSkillFiring = true;
        skillCooldownTimer = skillCooldown;

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

        Vector2 direction = attackDirection.sqrMagnitude > 0.0001f
            ? attackDirection.normalized
            : Vector2.right;

        while (elapsed < duration)
        {
            if (IsDead)
                break;

            Transform spawnPoint = attackEffectSpawnPoint != null ? attackEffectSpawnPoint : transform;
            FireAttackTrigger();
            SpawnAttackProjectile(spawnPoint.position, direction, skillProjectileDamage);

            yield return new WaitForSeconds(shotInterval);
            elapsed += shotInterval;
        }

        isSkillFiring = false;
    }

    private void SpawnAttackProjectile(Vector3 spawnPosition, Vector2 direction, int damage)
    {
        GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, Quaternion.identity);

        CoolantAttackProjectile projectile = effect.GetComponent<CoolantAttackProjectile>();
        if (projectile == null)
        {
            Destroy(effect, 1.5f);
            return;
        }

        projectile.Initialize(damage, enemyLayer, direction, attackProjectileSpeed);

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
