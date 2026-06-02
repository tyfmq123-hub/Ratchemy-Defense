using System.Collections;
using UnityEngine;

// 화염슬라임 3단계 - 2단계 능력 유지 + 원거리 공격 + 사망 시 폭발
// 사망 시 즉시 Destroy되지 않고 explosionDelay(기본 3초) 후 범위 폭발 데미지 후 제거
// 공격은 근접 대신 투사체(FlameProjectile) 발사
// Inspector에서 enemyUnitData 슬롯에 FlameSlimeTier3Data 에셋을 연결해야 합니다.
public class FlameSlimeTier3 : FlameSlimeTier2
{
    private FlameSlimeTier3Data flameData3;

    protected override void Start()
    {
        base.Start();

        flameData3 = CastData<FlameSlimeTier3Data>("FlameSlimeTier3Data");
    }

    protected override void Attack(Collider2D target)
    {
        if (flameData3 == null || flameData3.projectilePrefab == null)
        {
            Debug.LogWarning("[FlameSlimeTier3] projectilePrefab이 FlameSlimeTier3Data에 연결되지 않았습니다.");
            return;
        }

        BeginRangedAttack(target);
        animator?.SetBool("IsWalking", false);
        animator?.SetTrigger("Attack");
    }

    // 애니메이션 이벤트에서 호출 - T3_Attack 클립의 원하는 프레임에 이벤트 추가
    // Animation 창 → Add Event → Function: FireProjectile
    public void FireProjectile()
    {
        if (!CanFireRangedAttack())
        {
            ClearRangedAttack();
            return;
        }

        Vector3 dir = (pendingTarget.transform.position - transform.position).normalized;
        GameObject proj = Instantiate(flameData3.projectilePrefab, transform.position, Quaternion.identity);

        FlameProjectile projectile = proj.GetComponent<FlameProjectile>();
        if (projectile != null)
            projectile.Initialize(dir, flameData3.projectileSpeed, damage);

        ClearRangedAttack();
    }

    protected override void OnDie()
    {
        if (!BeginDeath()) return;

        animator?.SetBool("IsWalking", false);

        // 타겟 불가 처리 - 플레이어 유닛의 감지에서 제외
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(DeathExplosion());
    }

    private IEnumerator DeathExplosion()
    {
        // CoolantRat 스킬 즉사 시 ApplyDebuff가 같은 프레임에 실행되도록 1프레임 대기
        yield return null;

        if (isSkillDisabled)
            yield return PlaySealedDeath();
        else
            yield return PlayExplosionSequence();

        Destroy(gameObject);
    }

    private IEnumerator PlaySealedDeath()
    {
        animator?.SetTrigger("Die");
        yield return new WaitForSeconds(dieAnimDuration);
    }

    private IEnumerator PlayExplosionSequence()
    {
        animator?.SetBool("IsExploding", true);

        // Fuming ~ Explode 구간 동안 AuraEffect 비활성화
        if (auraEffectTransform != null)
            auraEffectTransform.gameObject.SetActive(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showDebugCircles)
        {
            LineRenderer expCircle = CreateDebugCircle("ExplosionRangeCircle", flameData3.explosionRadius,
                new Color(1f, 0.1f, 0f, 0.8f), width: 0.07f);
            if (expCircle != null)
                expCircle.transform.localPosition = (Vector3)flameData3.explosionOffset;
        }
#endif

        // PreExplode 트리거 → Fuming 애니메이션 재생 + explosionDelay 동안 깜박임
        animator?.SetTrigger("PreExplode");

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        float elapsed = 0f;
        float duration = flameData3.explosionDelay;
        while (elapsed < duration)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(blinkInterval);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval * 2f;
        }

        if (sr != null) sr.enabled = true;

        animator?.ResetTrigger("Die");
        animator?.SetTrigger("Explode");

        // Explode 애니메이션 재생 후 데미지
        yield return new WaitForSeconds(flameData3.explosionAnimDuration);

        Vector2 explosionCenter = (Vector2)transform.position + flameData3.explosionOffset;
        EnemyCombatUtility.DamagePlayersInRadius(explosionCenter, flameData3.explosionRadius, targetLayer, flameData3.explosionDamage);
    }

    [SerializeField] private float blinkInterval = 0.1f;

    private void OnDrawGizmosSelected()
    {
        if (flameData3 == null) return;
        Gizmos.color = new Color(1f, 0.1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, flameData3.explosionRadius);
    }
}
