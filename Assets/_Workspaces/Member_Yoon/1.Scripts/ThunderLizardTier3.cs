using UnityEngine;

// 번개도마뱀 3단계 - 2단계 능력 유지 + 체인 라이트닝
// 첫 번째 대상 적중 후 chainRange 내 최대 chainCount명에게 연쇄 데미지
// Inspector에서 enemyUnitData 슬롯에 ThunderLizardTier3Data 에셋을 연결해야 합니다.
public class ThunderLizardTier3 : ThunderLizardTier2
{
    private ThunderLizardTier3Data thunderData3;

    protected override void Start()
    {
        base.Start();

        thunderData3 = enemyUnitData as ThunderLizardTier3Data;
        if (thunderData3 == null)
            Debug.LogError($"[ThunderLizardTier3] enemyUnitData에 ThunderLizardTier3Data를 연결해주세요. ({gameObject.name})");
    }

    // LightningProjectile 오버라이드 - 체인 파라미터를 포함해 투사체 초기화
    public override void LightningProjectile()
    {
        if (pendingTarget == null) return;
        if (thunderData2 == null || thunderData2.lightningPrefab == null)
        {
            Debug.LogWarning("[ThunderLizardTier3] lightningPrefab이 연결되지 않았습니다.");
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogWarning("[ThunderLizardTier3] AttackPoint가 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos  = attackPoint.position;
        Vector3 targetPos = pendingTarget.bounds.center;
        Vector3 dir       = (targetPos - spawnPos).normalized;
        GameObject proj = Instantiate(thunderData2.lightningPrefab, spawnPos, Quaternion.identity);

        LightningProjectile projectile = proj.GetComponent<LightningProjectile>();
        if (projectile != null)
        {
            int chain   = thunderData3 != null ? thunderData3.chainCount : 0;
            float range = thunderData3 != null ? thunderData3.chainRange  : 0f;
            projectile.Initialize(dir, thunderData2.projectileSpeed, damage, chain, range,
                GetComponent<Collider2D>(), thunderData3);
        }

        pendingTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (thunderData3 == null) return;
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, thunderData3.chainRange);
    }
}
