using UnityEngine;

public class InsulatorRat : PlayerUnitBase
{
    private LayerMask enemyLayer;
    private float attackCooldown;

    protected override void Awake()
    {
        base.Awake();

        enemyLayer = LayerMask.GetMask("Enemy");

        maxHp = 80f;
        attackPower = 12f;
        moveSpeed = 3f;
        attackSpeed = 1.2f;
        attackRange = 1f;

        CurrentHp = maxHp;
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        Attack();
    }

    protected override void Attack()
    {
        if (attackCooldown > 0f)
            return;

        // TODO: 범위 내 적 감지 및 데미지 적용
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        Debug.Log($"[InsulatorRat] 공격 시도 | 감지된 콜라이더: {hits.Length}개 | 사거리: {attackRange} | 레이어마스크: {enemyLayer.value}");
        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerUnitBase>()?.TakeDamage(attackPower);
            Debug.Log($"[InsulatorRat] {hit.name} 공격 → 데미지: {attackPower}");
        }

        attackCooldown = 1f / attackSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
