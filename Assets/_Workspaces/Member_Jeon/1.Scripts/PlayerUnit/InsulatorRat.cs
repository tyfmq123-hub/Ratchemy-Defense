using UnityEngine;

// 절연체 쥐 — 적 없음: run만 재생, 적 있음: attack만 (Animator bool 없이 Play로 제어)
public class InsulatorRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string attackStateName = "InsulatorRat_attack";
    [SerializeField] private string runStateName = "InsulatorRat_run";
    [SerializeField] private float runClipLength = 0.5f;

    private LayerMask enemyLayer;
    private float attackCooldown;
    private bool wasInCombat;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer.value == 0)
            Debug.LogWarning("[InsulatorRat] 'Enemy' 레이어가 없습니다. Edit → Project Settings → Tags and Layers 확인.");

        wasInCombat = false;
        PlayRunState();
    }

    private void Reset()
    {
        maxHp = 80f;
        currentHp = maxHp;
        attackPower = 12;
        moveSpeed = 3f;
        attackSpeed = 1.2f;
        attackRange = 1f;
    }

    protected override void Die()
    {
        PlayRunState();
        base.Die();
    }

    protected override void Update()
    {
        if (IsDead)
            return;

        if (animator != null)
            animator.speed = 1f;

        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        EnemyUnit enemy = FindNearestEnemyInRange(attackRange, enemyLayer);
        if (enemy == null)
        {
            if (wasInCombat)
            {
                wasInCombat = false;
                PlayRunState();
            }
            else
            {
                // 매 프레임 run 시간 진행 (Animator 전이에 안 막혀도 반드시 run 재생)
                PlayRunState();
            }

            Move();
            return;
        }

        wasInCombat = true;

        if (attackCooldown <= 0f)
            TryAttack(enemy);
    }

    private void TryAttack(EnemyUnit target)
    {
        if (attackCooldown > 0f || target == null || target.IsDead())
            return;

        FireAttackTrigger();
        target.TakeDamage(attackPower);
        PlayAttackSound();
        Debug.Log($"[InsulatorRat] {target.name} 공격 → 데미지: {attackPower}");
        attackCooldown = GetAttackCooldownDuration();
    }

    private void FireAttackTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(attackTriggerName))
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);

        if (!string.IsNullOrEmpty(attackStateName))
            animator.Play(attackStateName, 0, 0f);
    }

    private void PlayRunState()
    {
        if (animator == null || string.IsNullOrEmpty(runStateName))
            return;

        if (!string.IsNullOrEmpty(attackTriggerName))
            animator.ResetTrigger(attackTriggerName);

        float length = Mathf.Max(runClipLength, 0.01f);
        float normalized = (Time.time % length) / length;
        animator.Play(runStateName, 0, normalized);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
