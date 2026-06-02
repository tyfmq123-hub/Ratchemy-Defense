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

    protected bool isDying;
    protected bool isAttacking;
    protected Collider2D pendingTarget;

    private float AttackCooldown => attackSpeed > 0f ? 1f / attackSpeed : float.PositiveInfinity;

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

        attackTimer = AttackCooldown;

        targetLayer = LayerMask.GetMask("Player");
    }

    protected virtual void Update()
    {
        if (isDying) return;

        if (EnemyCombatUtility.TryFindClosestPlayer(transform.position, attackRange, targetLayer, out Collider2D player))
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= AttackCooldown && !isAttacking)
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
        if (EnemyCombatUtility.TryGetPlayer(target, out PlayerUnitBase player))
            player.TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead()) return;

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

    // 사망 처리 시작 — 이미 죽는 중이면 false, 아니면 isDying 설정 + 이동/공격 정지
    protected bool BeginDeath()
    {
        if (isDying) return false;

        isDying = true;
        moveSpeed = 0f;
        attackSpeed = 0f;
        ClearRangedAttack();
        return true;
    }

    // 원거리 공격 — 애니메이션 이벤트에서 투사체 발사
    protected void BeginRangedAttack(Collider2D target)
    {
        pendingTarget = target;
        isAttacking = true;
    }

    protected void ClearRangedAttack()
    {
        pendingTarget = null;
        isAttacking = false;
    }

    protected bool CanFireRangedAttack()
    {
        if (isDying || pendingTarget == null) return false;
        return EnemyCombatUtility.TryGetPlayer(pendingTarget, out _);
    }

    protected T CastData<T>(string expectedTypeName) where T : EnemyUnitData
    {
        if (enemyUnitData is T data)
            return data;

        Debug.LogError($"[{GetType().Name}] enemyUnitData에 {expectedTypeName}를 연결해주세요. ({gameObject.name})");
        return null;
    }
}
