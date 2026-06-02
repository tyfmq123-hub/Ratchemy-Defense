using System.Collections;
using UnityEngine;

// 보스 유닛 - EnemyUnit 기반, 이동 없음
// Inspector에서 enemyUnitData 슬롯에 BossData 에셋을 연결해야 합니다.
// Animator Parameters: Trigger "Attack", Trigger "Die"
public class BossBase : EnemyUnit
{
    protected BossData bossData;
    protected Animator animator;

    // 투사체가 비행 중일 때 true → 착탄 전까지 추가 발사 차단
    private bool isProjectileActive = false;

    // 화염 오라 버프
    private bool hasAuraBuff = true;
    private Transform auraEffectTransform;

    // 보스 사망 시 다른 시스템이 구독할 수 있는 이벤트
    public static event System.Action OnBossDead;

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

        StartCoroutine(AuraLoop());
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
            float dist       = Vector2.Distance(transform.position, targetPos);
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
