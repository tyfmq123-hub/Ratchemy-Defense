using UnityEngine;

public class PlayerUnitBase : MonoBehaviour
{
    [Header("기본 능력치")]
    [SerializeField] protected float maxHp = 100f;
    [SerializeField] protected float attackPower = 10f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float attackSpeed = 1f;
    [SerializeField] protected float attackRange = 1.5f;

    public float CurrentHp { get; protected set; }
    public float MaxHp => maxHp;
    public float AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public float AttackRange => attackRange;

    protected virtual void Awake()
    {
        CurrentHp = maxHp;
    }

    protected virtual void Update()
    {
        Move();
    }

    protected virtual void Move()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    protected virtual void Attack() { }

    public virtual void TakeDamage(float damage)
    {
        CurrentHp -= damage;
        if (CurrentHp <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
