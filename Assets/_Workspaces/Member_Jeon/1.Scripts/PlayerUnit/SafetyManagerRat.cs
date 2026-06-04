using UnityEngine;

// 안전관리소장 쥐 — 사거리 내 공격, 없으면 전진
public class SafetyManagerRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolName = "isattack";
    [SerializeField] private string attackStateName = "SafetyManager_attack";

    private LayerMask enemyLayer;
    private float attackCooldown;

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
            SetAttackAnimation(true);
            AttackEnemy(enemy);
        }
        else
        {
            SetAttackAnimation(false);
            Move();
        }
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
