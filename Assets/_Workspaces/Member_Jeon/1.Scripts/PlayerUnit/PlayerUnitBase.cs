using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// 아군 유닛 공통 베이스
// - maxHp / currentHp: Inspector에서 직접 수정 가능 (Play 시 코드가 덮어쓰지 않음)
// - InsulatorRat 등 자식 클래스는 Move·Attack 등을 override
public class PlayerUnitBase : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] protected float maxHp = 100f;    // 최대 체력
    [SerializeField] protected float currentHp;       // 현재 체력 (Inspector에서 시작 체력 설정 가능)

    [Header("전투·이동")]
    [SerializeField] protected int attackPower = 10;  // 한 번에 주는 데미지 (EnemyUnit.TakeDamage와 동일 int)
    [SerializeField] protected float moveSpeed = 2f;       // 초당 이동 거리
    [SerializeField] protected float attackSpeed = 1f;     // 초당 공격 횟수 (쿨다운 = 1 / attackSpeed)
    [SerializeField] protected float attackRange = 1.5f;   // 공격·감지 반경

    [Header("코스트 환급")]
    [SerializeField][Range(0f, 1f)] protected float deathRefundRatio = 0.5f; // 사망 시 소환 코스트의 몇 %를 돌려줄지

    [Header("피격 연출")]
    [SerializeField][Range(0, 255)] private int hitFlashAlpha = 215;
    [SerializeField] private float hitFlashBlinkDuration = 0.1f;

    [Header("공격 사운드")]
    [SerializeField] protected AudioClip attackSound;
    [SerializeField][Range(0f, 3f)] protected float attackSoundVolume = 1.5f;
    [SerializeField][Range(0f, 3f)] protected float attackSoundVolumeBoost = 2f;

    [Header("Y축 깊이 정렬")]
    [SerializeField] private int hpBarSortingOffset = 4;

    private int spawnCost;
    private bool hasRefundedCost;
    private SpriteRenderer bodySprite;
    private SortingGroup sortingGroup;
    private Canvas[] worldSpaceCanvases;
    private Color originalSpriteColor;
    private Coroutine hitFlashCoroutine;

    protected virtual int SortingOrderBase => 0;

    public int CurrentSortingOrder { get; private set; }

    // 다른 스크립트에서 읽기 전용으로 접근
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public int AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public float AttackRange => attackRange;
    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;

    // 스킬 쿨 UI용 — 0: 막 사용함(비어 있음), 1: 사용 가능(가득 참)
    public virtual bool HasSkillCooldown => false;

    public virtual float SkillCooldownFill => 1f;

    public int SpawnCost => spawnCost;

    protected bool isDead;

    // 카드 소환 시 지불한 코스트·환급 비율을 기록 (UnitCardSpawner에서 호출)
    public void ConfigureSpawnCost(int cost, float refundRatio = -1f)
    {
        spawnCost = Mathf.Max(0, cost);

        if (refundRatio >= 0f)
            deathRefundRatio = Mathf.Clamp01(refundRatio);
    }

    // Inspector에서 숫자 바꿀 때마다 호출 → currentHp가 maxHp를 넘지 않게 맞춤
    protected virtual void OnValidate()
    {
        if (maxHp < 1f)
            maxHp = 1f;

        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        // 에디터에서 currentHp를 0으로 두었으면 maxHp와 같게 보여 줌 (미설정 처리)
        if (!Application.isPlaying && currentHp <= 0f)
            currentHp = maxHp;
    }

    protected virtual void Awake()
    {
        // Inspector에 currentHp가 있으면 그대로 사용, 0이면 만땅으로 시작
        if (currentHp <= 0f)
            currentHp = maxHp;
        else
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        BindHpBarInChildren();
        BindMpBarInChildren();
        CacheBodySprite();
        SetupDepthSorting();
        NotifyHealthChanged();
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        Move(); // 자식에서 override하면 이동 방식 변경 가능
    }

    protected virtual void LateUpdate()
    {
        if (isDead)
            return;

        ApplyDepthSorting();
    }

    // 기본 이동: 오른쪽 직진
    protected virtual void Move()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    // 자식 클래스에서 공격 구현 (베이스는 비어 있음)
    protected virtual void Attack() { }

    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0f);
        NotifyHealthChanged();

        if (damage > 0f)
            PlayHitFlash();

        if (currentHp <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        if (isDead)
            return;

        StopHitFlash();
        isDead = true;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("die");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        TryRefundCostOnDeath();
        Destroy(gameObject, 1.2f);
    }

    protected void TryRefundCostOnDeath()
    {
        if (hasRefundedCost || spawnCost <= 0 || deathRefundRatio <= 0f)
            return;

        int refundAmount = Mathf.FloorToInt(spawnCost * deathRefundRatio);
        if (refundAmount <= 0)
            return;

        CostManager costManager = FindAnyObjectByType<CostManager>();
        if (costManager == null)
            return;

        hasRefundedCost = true;
        costManager.AddCost(refundAmount);
    }

    // 사거리 안 살아 있는 적 중 가장 가까운 대상
    protected EnemyUnit FindNearestEnemyInRange(float range, LayerMask enemyLayer)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        EnemyUnit nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemy = hit.GetComponent<EnemyUnit>();
            if (enemy == null || enemy.IsDead())
                continue;

            float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    protected float GetAttackCooldownDuration()
    {
        return 1f / Mathf.Max(attackSpeed, 0.01f);
    }

    protected void PlayAttackSound()
    {
        if (attackSound == null)
            return;

        float finalVolume = Mathf.Clamp(attackSoundVolume * attackSoundVolumeBoost, 0f, 3f);
        Vector3 playPosition = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(attackSound, playPosition, finalVolume);
    }

    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    private void BindHpBarInChildren()
    {
        Hpbar[] hpBars = GetComponentsInChildren<Hpbar>(true);
        foreach (Hpbar hpBar in hpBars)
            hpBar.Bind(this);
    }

    private void BindMpBarInChildren()
    {
        Mpbar[] mpBars = GetComponentsInChildren<Mpbar>(true);
        foreach (Mpbar mpBar in mpBars)
            mpBar.Bind(this);
    }

    private void CacheBodySprite()
    {
        bodySprite = GetComponent<SpriteRenderer>();
        if (bodySprite != null)
            originalSpriteColor = bodySprite.color;
    }

    private void SetupDepthSorting()
    {
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        int worldCanvasCount = 0;
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                worldCanvasCount++;
        }

        if (worldCanvasCount > 0)
        {
            worldSpaceCanvases = new Canvas[worldCanvasCount];
            int index = 0;
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                canvas.overrideSorting = true;
                worldSpaceCanvases[index++] = canvas;
            }
        }

        ApplyDepthSorting();
    }

    private void ApplyDepthSorting()
    {
        CurrentSortingOrder = Mathf.RoundToInt(-transform.position.y * 100f)
            + SortingOrderBase
            + (GetInstanceID() % 10);

        if (sortingGroup != null)
            sortingGroup.sortingOrder = CurrentSortingOrder;
        else if (bodySprite != null)
            bodySprite.sortingOrder = CurrentSortingOrder;

        if (worldSpaceCanvases == null)
            return;

        int hpBarOrder = CurrentSortingOrder + hpBarSortingOffset;
        foreach (Canvas canvas in worldSpaceCanvases)
        {
            if (canvas != null)
                canvas.sortingOrder = hpBarOrder;
        }
    }

    private void PlayHitFlash()
    {
        if (bodySprite == null)
            return;

        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        Color flashColor = originalSpriteColor;
        flashColor.a = hitFlashAlpha / 255f;
        bodySprite.color = flashColor;

        yield return new WaitForSeconds(hitFlashBlinkDuration);

        if (bodySprite != null)
            bodySprite.color = originalSpriteColor;

        hitFlashCoroutine = null;
    }

    private void StopHitFlash()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        if (bodySprite != null)
            bodySprite.color = originalSpriteColor;
    }
}
