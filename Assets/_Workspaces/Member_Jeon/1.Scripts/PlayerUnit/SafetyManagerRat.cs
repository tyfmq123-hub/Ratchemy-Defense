using UnityEngine;

// 안전관리소장 쥐 — 사거리 내 공격, 없으면 전진 / 달리기 중 받는 데미지 감소 패시브
public class SafetyManagerRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolName = "isattack";
    [SerializeField] private string attackStateName = "SafetyManager_attack";

    [Header("패시브 — 달리기")]
    [SerializeField] private float runDamageReduction = 0.8f;

    private LayerMask enemyLayer;
    private float attackCooldown;
    private bool isRunning;

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

        enemyUnit.TakeDamage(attackPower);
        Debug.Log($"[SafetyManagerRat] 창 공격 → {enemyUnit.name}, 데미지: {attackPower}");
        attackCooldown = GetAttackCooldownDuration();
        PlayAttackFromStart();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
