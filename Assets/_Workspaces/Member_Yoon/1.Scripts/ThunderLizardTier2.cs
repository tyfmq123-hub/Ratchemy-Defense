using UnityEngine;

// 번개도마뱀 2단계 - 1단계 능력 유지 + 원거리 번개 단일 공격
// 투사체(LightningProjectile)를 발사해 대상 1명에게 데미지
// Inspector에서 enemyUnitData 슬롯에 ThunderLizardTier2Data 에셋을 연결해야 합니다.
// Animation 창 → Attack 클립의 원하는 프레임에 이벤트 추가 → Function: FireProjectile
public class ThunderLizardTier2 : ThunderLizard
{
    protected ThunderLizardTier2Data thunderData2;
    protected Collider2D pendingTarget;

    [SerializeField] protected Transform attackPoint;

    protected override void Start()
    {
        base.Start();

        thunderData2 = enemyUnitData as ThunderLizardTier2Data;
        if (thunderData2 == null)
            Debug.LogError($"[ThunderLizardTier2] enemyUnitData에 ThunderLizardTier2Data를 연결해주세요. ({gameObject.name})");
    }

    protected override void Attack(Collider2D target)
    {
        if (thunderData2 == null || thunderData2.lightningPrefab == null)
        {
            Debug.LogWarning("[ThunderLizardTier2] lightningPrefab이 ThunderLizardTier2Data에 연결되지 않았습니다.");
            return;
        }

        pendingTarget = target;
        animator?.SetBool("IsWalking", false);
        animator?.SetTrigger("Attack");
    }

    // 애니메이션 이벤트에서 호출
    // Animation 창 → Attack 클립의 원하는 프레임에 이벤트 추가
    // Function: FireProjectile
    public virtual void LightningProjectile()
    {
        if (pendingTarget == null) return;

        if (attackPoint == null)
        {
            Debug.LogWarning("[ThunderLizardTier2] AttackPoint가 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos  = attackPoint.position;
        Vector3 targetPos = pendingTarget.bounds.center;
        Vector3 dir       = (targetPos - spawnPos).normalized;
        GameObject proj = Instantiate(thunderData2.lightningPrefab, spawnPos, Quaternion.identity);

        LightningProjectile projectile = proj.GetComponent<LightningProjectile>();
        if (projectile != null)
            projectile.Initialize(dir, thunderData2.projectileSpeed, damage, 0, 0f,
                GetComponent<Collider2D>());

        pendingTarget = null;
    }
}
