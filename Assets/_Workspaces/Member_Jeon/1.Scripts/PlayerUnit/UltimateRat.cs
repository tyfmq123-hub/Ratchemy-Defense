using System.Collections;
using UnityEngine;

// 궁극 쥐 — 사거리 내 근접 공격, 앞으로 퍼지는 충격파 스킬
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
    [SerializeField] private float skillAnimLength = 0.4f;

    [Header("스킬 이펙트")]
    [SerializeField] private GameObject skillEffectPrefab;
    [SerializeField] private Transform skillEffectSpawnPoint;
    [SerializeField] private Vector2 skillEffectDirection = Vector2.right;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        enemyLayer = LayerMask.GetMask("Enemy");
        skillCooldownTimer = skillCooldown;
        ResolveSkillEffectSpawnPoint();
    }

    private void ResolveSkillEffectSpawnPoint()
    {
        Transform spawnChild = transform.Find("Skill Effect Spawn Point");
        if (spawnChild == null)
            spawnChild = transform.Find("Skilpoint");

        if (spawnChild != null)
            skillEffectSpawnPoint = spawnChild;

        if (skillEffectSpawnPoint == transform)
            skillEffectSpawnPoint = spawnChild;

        if (skillEffectSpawnPoint == null)
            Debug.LogWarning("[UltimateRat] Skill Effect Spawn Point 자식 오브젝트가 없습니다.");
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

        if (isCastingSkill)
            return;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        if (skillCooldownTimer <= 0f && HasAliveEnemy())
        {
            BeginUltimateCast();
            return;
        }

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

    private void BeginUltimateCast()
    {
        if (isCastingSkill)
            return;

        isCastingSkill = true;
        skillCooldownTimer = Mathf.Max(skillCooldown, 0.01f);
        StartCoroutine(UseShockwaveSkill());
    }

    private IEnumerator UseShockwaveSkill()
    {
        FireSkillTrigger();

        int waveCount = Mathf.Max(skillHitCount, 1);
        float interval = Mathf.Max(skillHitInterval, 0f);

        for (int wave = 0; wave < waveCount; wave++)
        {
            if (IsDead)
            {
                EndUltimateCast();
                yield break;
            }

            SpawnForwardSkillEffect();

            if (wave < waveCount - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(Mathf.Max(skillAnimLength, 0.01f));
        EndUltimateCast();
    }

    private void EndUltimateCast()
    {
        isCastingSkill = false;

        if (animator != null && !string.IsNullOrEmpty(skillTriggerName))
            animator.ResetTrigger(skillTriggerName);
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

    public void SpawnForwardSkillEffect()
    {
        if (skillEffectPrefab == null)
            return;

        ResolveSkillEffectSpawnPoint();

        if (skillEffectSpawnPoint == null || skillEffectSpawnPoint == transform)
            return;

        GameObject effect = Instantiate(skillEffectPrefab, skillEffectSpawnPoint);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = skillEffectSpawnPoint.lossyScale;
        effect.transform.localScale = new Vector3(
            parentScale.x > 0f ? 1f / parentScale.x : 1f,
            parentScale.y > 0f ? 1f / parentScale.y : 1f,
            1f
        );

        SkillEffectProjectile projectile = effect.GetComponent<SkillEffectProjectile>();
        if (projectile == null)
        {
            Destroy(effect, 1.5f);
            return;
        }

        Vector2 direction = skillEffectDirection.sqrMagnitude > 0.0001f
            ? skillEffectDirection.normalized
            : Vector2.right;
        projectile.Initialize(attackPower, enemyLayer, direction);

        SpriteRenderer unitSprite = GetComponent<SpriteRenderer>();
        SpriteRenderer effectSprite = effect.GetComponent<SpriteRenderer>();
        if (unitSprite != null && effectSprite != null)
            effectSprite.sortingOrder = unitSprite.sortingOrder + 1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (skillEffectSpawnPoint != null && skillEffectSpawnPoint != transform)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(skillEffectSpawnPoint.position, 0.12f);
        }
    }
}
