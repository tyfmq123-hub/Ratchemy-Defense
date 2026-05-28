using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] private EnemyUnitData enemyUnitData;

    private int currentHp;
    private float attackTimer;
    private LayerMask targetLayer;

    private int damage;
    private int maxHp;
    private float attackRange;
    private float attackSpeed;
    private float moveSpeed;

    void Start()
    {
        damage      = enemyUnitData.damage;
        maxHp       = enemyUnitData.maxHp;
        attackRange = enemyUnitData.attackRange;
        attackSpeed = enemyUnitData.attackSpeed;
        moveSpeed   = enemyUnitData.moveSpeed;
        currentHp   = enemyUnitData.hp;

        targetLayer = LayerMask.GetMask("Player");
    }

    void Update()
    {
        Collider2D player = Physics2D.OverlapCircle(transform.position, attackRange, targetLayer);

        if (player != null)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= 1f / attackSpeed)
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
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
    }

    private void Attack(Collider2D target)
    {
        PlayerUnitBase player = target.GetComponent<PlayerUnitBase>();
        if (player != null)
            player.TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        if (IsDead()) Destroy(gameObject);
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    public bool IsDead() => currentHp <= 0;
}
