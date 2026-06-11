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

        thunderData3 = CastData<ThunderLizardTier3Data>("ThunderLizardTier3Data");
    }

    // LightningProjectile 오버라이드 - 체인 파라미터를 포함해 투사체 초기화
    public override void LightningProjectile()
    {
        if (!ValidateLightningFire()) return;

        Vector3 spawnPos = attackPoint.position;
        Vector3 dir      = (pendingTarget.bounds.center - spawnPos).normalized;
        PlayAttackSound();
        GameObject proj = Instantiate(thunderData2.lightningPrefab, spawnPos, Quaternion.identity);

        LightningProjectile projectile = proj.GetComponent<LightningProjectile>();
        if (projectile != null)
        {
            int chain   = thunderData3 != null ? thunderData3.chainCount : 0;
            float range = thunderData3 != null ? thunderData3.chainRange  : 0f;
            projectile.Initialize(dir, thunderData2.projectileSpeed, damage, chain, range,
                GetComponent<Collider2D>(), thunderData3);
        }

        ClearRangedAttack();
    }

    private void OnDrawGizmosSelected()
    {
        if (thunderData3 == null) return;
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, thunderData3.chainRange);
    }
}
