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
        damage      = enemyUnitData.damage;
        maxHp       = enemyUnitData.maxHp;
        attackRange = enemyUnitData.attackRange;
        attackSpeed = enemyUnitData.attackSpeed;
        moveSpeed   = enemyUnitData.moveSpeed;
        currentHp   = enemyUnitData.hp;

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
        PlayerUnitBase player = target.GetComponent<PlayerUnitBase>();
        if (player != null)
            player.TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        Debug.Log($"[{gameObject.name}] 데미지 -{amount} / HP: {currentHp} / {maxHp}");
        if (IsDead())
        {
            Debug.Log($"[{gameObject.name}] 사망 → OnDie() 호출");
            OnDie();
        }
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
