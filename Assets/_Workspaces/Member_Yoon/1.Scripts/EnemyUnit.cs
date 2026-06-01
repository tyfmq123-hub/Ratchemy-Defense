using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] protected EnemyUnitData enemyUnitData;

    protected int currentHp;
    protected float attackTimer;
    protected LayerMask targetLayer;

    protected int damage;
    protected int maxHp;
    protected float attackRange;
    protected float attackSpeed;
    protected float moveSpeed;

    protected virtual void Start()
    {
        if (enemyUnitData == null)
        {
            Debug.LogError($"[EnemyUnit] enemyUnitData가 연결되지 않았습니다. ({gameObject.name})");
            enabled = false;
            return;
        }

        damage      = enemyUnitData.damage;
        attackRange = enemyUnitData.attackRange;
        attackSpeed = enemyUnitData.attackSpeed;
        moveSpeed   = enemyUnitData.moveSpeed;

        maxHp     = enemyUnitData.maxHp > 0 ? enemyUnitData.maxHp : enemyUnitData.hp;
        currentHp = enemyUnitData.hp > 0 ? Mathf.Min(enemyUnitData.hp, maxHp) : maxHp;

        attackTimer = 1f / attackSpeed;

        targetLayer = LayerMask.GetMask("Player");
    }

    protected virtual void Update()
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

    protected virtual void MoveLeft()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
    }

    protected virtual void Attack(Collider2D target)
    {
        PlayerUnitBase player = target.GetComponentInParent<PlayerUnitBase>();
        if (player != null)
            player.TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        if (IsDead())
            OnDie();
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    protected virtual void OnDie()
    {
        Destroy(gameObject);
    }

    public bool IsDead() => currentHp <= 0;
}
