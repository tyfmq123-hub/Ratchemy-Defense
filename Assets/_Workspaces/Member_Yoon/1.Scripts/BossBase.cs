using System.Collections;
using UnityEngine;

// 보스 유닛 - EnemyUnit 기반, 이동 없음
// Inspector에서 enemyUnitData 슬롯에 BossData 에셋을 연결해야 합니다.
// Inspector에서 bossHealthUI 슬롯에 BossHealthUI 컴포넌트를 연결해야 합니다.
// Animator Parameters: Trigger "Attack", Trigger "Die"
public class BossBase : EnemyUnit
{
    [Header("체력 UI")]
    [SerializeField] private BossHealthUI bossHealthUI;

    [Header("안전관리 점수")]
    [Tooltip("SafetyScoreManager가 붙어 있는 오브젝트를 연결하세요.")]
    [SerializeField] private SafetyScoreManager safetyScoreManager;

    protected BossData bossData;
    protected Animator animator;

    // 투사체가 비행 중일 때 true → 착탄 전까지 추가 발사 차단
    private bool isProjectileActive = false;

    // 화염 오라 버프
    private bool hasAuraBuff = true;
    private Transform auraEffectTransform;

    // 보스 사망 시 다른 시스템이 구독할 수 있는 이벤트
    public static event System.Action OnBossDead;

    private WaveManager waveManager;
    private WaveUI waveUI;
    private int lastWaveIndex;
    private bool bossStageReduced;

    protected override void Start()
    {
        base.Start();

        bossData = CastData<BossData>("BossData");

        animator = GetComponent<Animator>();

        auraEffectTransform = transform.Find("AuraEffect");
        if (auraEffectTransform != null)
            auraEffectTransform.gameObject.SetActive(true);
        else
            Debug.LogWarning("[BossBase] 자식 오브젝트 'AuraEffect'를 찾을 수 없습니다.");

        waveManager = FindObjectOfType<WaveManager>();
        waveUI = FindObjectOfType<WaveUI>();

        // Inspector 연결을 빠뜨린 경우를 대비해
        // Scene 안의 SafetyScoreManager를 자동으로 찾습니다.
        if (safetyScoreManager == null)
        {
            safetyScoreManager =
                FindFirstObjectByType<SafetyScoreManager>();
        }

        // 시작 시 체력 UI 초기화
        bossHealthUI?.SetHealth(
            currentHp,
            maxHp
        );

        // 시작 시 보스 체력도 안전관리 점수에 반영합니다.
        //
        // 보스 체력이 가득 차 있으므로
        // 보스 위험도 때문에 10점을 감점합니다.
        safetyScoreManager?.SetBossHealth(
            currentHp,
            maxHp
        );

        StartCoroutine(AuraLoop());
        StartCoroutine(WaveWatchLoop());
    }

    protected override void OnHealthChanged()
    {
        // 보스 체력 게이지를 갱신합니다.
        bossHealthUI?.SetHealth(
            currentHp,
            maxHp
        );

        // 안전관리 점수도 다시 계산합니다.
        //
        // 보스 체력이 감소하면
        // 보스 위험도 감점이 줄어들기 때문에
        // 안전관리 점수가 회복됩니다.
        safetyScoreManager?.SetBossHealth(
            currentHp,
            maxHp
        );
    }

    // 웨이브 매니저 등 외부에서 호출 → 오라 버프 제거 + AuraEffect 비활성화
    public void RemoveAuraBuff()
    {
        if (!hasAuraBuff) return;

        hasAuraBuff = false;

        if (auraEffectTransform != null)
            auraEffectTransform.gameObject.SetActive(false);
    }

    private IEnumerator AuraLoop()
    {
        while (!IsDead())
        {
            yield return new WaitForSeconds(bossData != null ? bossData.auraInterval : 2f);

            if (!hasAuraBuff || IsDead()) continue;

            DealAuraDamage();
        }
    }

    private void DealAuraDamage()
    {
        if (bossData == null) return;

        EnemyCombatUtility.DamagePlayersInRadius(transform.position, bossData.auraRadius, targetLayer, bossData.auraDamage);
    }

    private IEnumerator WaveWatchLoop()
    {
        // 한 프레임 대기 → WaveManager.Start() 완료 후 초기 웨이브 인덱스 기준 설정
        yield return null;
        lastWaveIndex = waveManager != null ? waveManager.CurrentWaveIndex : 0;

        while (!IsDead())
        {
            yield return new WaitForSeconds(0.5f);

            if (waveManager != null)
            {
                int current = waveManager.CurrentWaveIndex;
                if (current > lastWaveIndex)
                {
                    lastWaveIndex = current;
                    ReduceHPByPercent(20);
                }
            }

            // 4번째 웨이브 클리어 → 보스 스테이지 진입 감지
            if (!bossStageReduced && waveUI != null && waveUI.waveText.text == "BOSS")
            {
                bossStageReduced = true;
                ReduceHPByPercent(20);
            }
        }
    }

    private void ReduceHPByPercent(int percent)
    {
        int reduction = Mathf.RoundToInt(maxHp * percent / 100f);
        TakeDamage(reduction);
    }

    // 보스는 이동하지 않음
    protected override void MoveLeft() { }

    protected override void Attack(Collider2D target)
    {
        if (isDying) return;
        if (isProjectileActive) return;

        animator?.SetTrigger("Attack");

        if (bossData?.projectilePrefab == null)
        {
            Debug.LogWarning("[BossBase] projectilePrefab이 BossData에 연결되지 않았습니다. 근접 공격으로 대체합니다.");
            base.Attack(target);
            return;
        }

        isProjectileActive = true;
        FireProjectile(target.bounds.center);
    }

    private void FireProjectile(Vector2 targetPos)
    {
        GameObject proj = Instantiate(bossData.projectilePrefab, transform.position, Quaternion.identity);

        BossProjectile projectile = proj.GetComponent<BossProjectile>();
        if (projectile != null)
        {
            float dist = Vector2.Distance(transform.position, targetPos);
            float travelTime = dist / Mathf.Max(bossData.projectileSpeed, 0.1f);

            projectile.Initialize(
                transform.position,
                targetPos,
                bossData.arcHeight,
                damage,
                bossData.explosionRadius,
                targetLayer,
                travelTime,
                bossData.explosionEffectPrefab,
                bossData.effectLifetime,
                () => isProjectileActive = false
            );
        }
        else
        {
            Debug.LogWarning("[BossBase] projectilePrefab에 BossProjectile 컴포넌트가 없습니다.");
            isProjectileActive = false;
            Destroy(proj);
        }
    }

    protected override void OnDie()
    {
        if (!BeginDeath()) return;

        if (animator != null)
            StartCoroutine(DieRoutine());
        else
            BossDead();
    }

    private IEnumerator DieRoutine()
    {
        animator.SetTrigger("Die");

        yield return new WaitForSeconds(bossData != null ? bossData.dieAnimDuration : 1f);

        BossDead();
    }

    private void BossDead()
    {
        hasAuraBuff = false;
        if (auraEffectTransform != null)
            auraEffectTransform.gameObject.SetActive(false);

        // 보스 사망 후에는 보스 위험도 감점을 완전히 제거합니다.
        safetyScoreManager?.SetBossHealth(
            0f,
            maxHp
        );

        OnBossDead?.Invoke();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (bossData != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, bossData.explosionRadius);
        }
    }
}
