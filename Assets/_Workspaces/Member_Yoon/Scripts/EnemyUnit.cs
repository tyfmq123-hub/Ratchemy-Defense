using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] private EnemyUnitData enemyUnitData;
    [SerializeField] private LayerMask targetLayer;

    private int currentHp;
    private float attackTimer;

    private int Damage;
    private int MaxHp;
    private float AttackRange;
    private float AttackSpeed;
    private float MoveSpeed;

    void Start()
    {
        Damage      = enemyUnitData.damage;
        MaxHp       = enemyUnitData.maxHp;
        AttackRange = enemyUnitData.attackRange;
        AttackSpeed = enemyUnitData.attackSpeed;
        MoveSpeed   = enemyUnitData.moveSpeed;
        currentHp   = enemyUnitData.hp;
    }

    void Update()
    {
        Collider2D player = Physics2D.OverlapCircle(transform.position, AttackRange, targetLayer);

        if (player != null)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= 1f / AttackSpeed)
            {
                Attack(player);
                attackTimer = 0f;
            }
        }
        else
        {
            MoveLeft();
        }
    }

    private void MoveLeft()
    {
        if (gameObject.layer != LayerMask.NameToLayer("Enemy")) return;

        transform.Translate(Vector3.left * MoveSpeed * Time.deltaTime);
    }

    private void Attack(Collider2D target)
    {
        PlayerUnitBase player = target.GetComponent<PlayerUnitBase>();
        if (player != null)
            player.TakeDamage(Damage);
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    public bool IsDead() => currentHp <= 0;
}
