using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [SerializeField] protected EnemyUnitData enemyUnitData;
    [SerializeField] private EnemyHPBar hpBar;

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

    private SpriteRenderer spriteRenderer;
    private Color _originalColor;
    private bool _isFlashing;
    private float _flashTimer;
    private const float FlashDuration = 0.1f;
    private const float FlashAlpha = 215 / 255f;

    protected virtual int SortingOrderBase => 0;

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

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            _originalColor = spriteRenderer.color;

        hpBar?.UpdateHP(currentHp, maxHp);
    }

    protected virtual void Update()
    {
        if (isDying) return;

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + SortingOrderBase + (GetInstanceID() % 10);

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
        {
            player.TakeDamage(damage);
            DamageTextSpawner.Spawn(damage, player.GetComponentInChildren<SpriteRenderer>(), isAllyHit: true);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead()) return;

        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        OnHealthChanged();
        StartHitFlash();
        DamageTextSpawner.Spawn(amount, spriteRenderer, isAllyHit: false);
        if (IsDead())
            OnDie();
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        OnHealthChanged();
    }

    // 체력이 변경될 때 호출됩니다. 하위 클래스에서 오버라이드하여 UI 등을 갱신하세요.
    protected virtual void OnHealthChanged()
    {
        hpBar?.UpdateHP(currentHp, maxHp);
    }

    protected virtual void OnDie()
    {
        hpBar?.Hide();
        Destroy(gameObject);
    }

    public bool IsDead() => currentHp <= 0;

    private void StartHitFlash()
    {
        if (spriteRenderer == null) return;
        if (!_isFlashing)
            _originalColor = spriteRenderer.color;
        _isFlashing = true;
        _flashTimer = 0f;
    }

    // Animator 이후(LateUpdate)에 색상을 덮어써야 Animator 애니메이션 클립과 충돌 없이 동작함
    private void LateUpdate()
    {
        if (!_isFlashing || spriteRenderer == null || isDying) return;

        _flashTimer += Time.deltaTime;

        if (_flashTimer >= FlashDuration)
        {
            _isFlashing = false;
            spriteRenderer.color = _originalColor;
            return;
        }

        spriteRenderer.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, FlashAlpha);
    }

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
        attackTimer = 0f;
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
