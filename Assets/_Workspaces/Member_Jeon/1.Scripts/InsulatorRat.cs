using UnityEngine;

public class InsulatorRat : PlayerUnitBase
{
    private LayerMask enemyLayer;
    private float attackTimer;

    protected override void Awake()
    {
        maxHp = 80f;
        attackPower = 12f;
        moveSpeed = 3f;
        attackSpeed = 1.2f;
        attackRange = 1f;

        base.Awake();

        enemyLayer = LayerMask.GetMask("Enemy");
    }

    protected override void Update()
    {
        Collider2D enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);

        if (enemy != null)
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= 1f / attackSpeed)
            {
                Attack(enemy);
                attackTimer = 0f;
            }
        }
        else
        {
            Move();
        }
    }

    private void Attack(Collider2D target)
    {
        EnemyUnit enemy = target.GetComponent<EnemyUnit>();

        if (enemy != null)
        {
            enemy.TakeDamage((int)attackPower);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}