using System.Collections;
using UnityEngine;

// 화염슬라임 3단계 - 2단계 능력 유지 + 원거리 공격 + 사망 시 폭발
// 사망 시 즉시 Destroy되지 않고 explosionDelay(기본 3초) 후 범위 폭발 데미지 후 제거
// 공격은 근접 대신 투사체(FlameProjectile) 발사
// Inspector에서 enemyUnitData 슬롯에 FlameSlimeTier3Data 에셋을 연결해야 합니다.
public class FlameSlimeTier3 : FlameSlimeTier2
{
    private FlameSlimeTier3Data flameData3;
    private Collider2D pendingTarget;

    protected override void Start()
    {
        base.Start();

        flameData3 = enemyUnitData as FlameSlimeTier3Data;
        if (flameData3 == null)
            Debug.LogError($"[FlameSlimeTier3] enemyUnitData에 FlameSlimeTier3Data를 연결해주세요. ({gameObject.name})");
    }

    protected override void Update()
    {
        if (isDying) return;
        base.Update();
    }

    protected override void Attack(Collider2D target)
    {
        if (flameData3.projectilePrefab == null)
        {
            Debug.LogWarning("[FlameSlimeTier3] projectilePrefab이 FlameSlimeTier3Data에 연결되지 않았습니다.");
            return;
        }

        pendingTarget = target;
        animator?.SetBool("IsWalking", false);
        animator?.SetTrigger("Attack");
    }

    // 애니메이션 이벤트에서 호출 - T3_Attack 클립의 원하는 프레임에 이벤트 추가
    // Animation 창 → Add Event → Function: FireProjectile
    public void FireProjectile()
    {
        if (pendingTarget == null || isDying) return;

        Vector3 dir = (pendingTarget.transform.position - transform.position).normalized;
        GameObject proj = Instantiate(flameData3.projectilePrefab, transform.position, Quaternion.identity);

        FlameProjectile projectile = proj.GetComponent<FlameProjectile>();
        if (projectile != null)
            projectile.Initialize(dir, flameData3.projectileSpeed, damage);

        pendingTarget = null;
    }

    protected override void OnDie()
    {
        if (isDying) return;
        isDying = true;

        Debug.Log($"[FlameSlimeTier3] OnDie 호출 - isSkillDisabled: {isSkillDisabled}");

        moveSpeed = 0f;
        attackSpeed = 0f;
        animator?.SetBool("IsWalking", false);

        // 타겟 불가 처리 - 플레이어 유닛의 감지에서 제외
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (!isSkillDisabled)
        {
            animator?.SetBool("IsExploding", true);
        }

        StartCoroutine(DeathExplosion());
    }

    private IEnumerator DeathExplosion()
    {
        Debug.Log($"[FlameSlimeTier3] DeathExplosion 시작 - isSkillDisabled: {isSkillDisabled}");

        if (!isSkillDisabled)
        {
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
            Debug.Log($"[FlameSlimeTier3] PreExplode 깜박임 시작 - duration: {duration}");

            while (elapsed < duration)
            {
                if (sr != null) sr.enabled = false;
                yield return new WaitForSeconds(blinkInterval);
                if (sr != null) sr.enabled = true;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval * 2f;
            }

            if (sr != null) sr.enabled = true;

            Debug.Log("[FlameSlimeTier3] Explode 트리거 발동");
            animator?.ResetTrigger("Die");
            animator?.SetTrigger("Explode");

            // Explode 애니메이션 재생 후 데미지
            yield return new WaitForSeconds(flameData3.explosionAnimDuration);

            Vector2 explosionCenter = (Vector2)transform.position + flameData3.explosionOffset;
            Collider2D[] targets = Physics2D.OverlapCircleAll(explosionCenter, flameData3.explosionRadius, targetLayer);
            foreach (Collider2D col in targets)
            {
                PlayerUnitBase player = col.GetComponent<PlayerUnitBase>();
                if (player != null)
                    player.TakeDamage(flameData3.explosionDamage);
            }
        }
        else
        {
            // 디버프 있음 → Die 애니메이션만 재생 후 제거
            Debug.Log("[FlameSlimeTier3] 스킬 봉인 - 폭발 없이 Die 애니메이션 재생");
            animator?.SetTrigger("Die");
            yield return new WaitForSeconds(dieAnimDuration);
        }

        Destroy(gameObject);
    }

    [SerializeField] private float blinkInterval = 0.1f;

    private void OnDrawGizmosSelected()
    {
        if (flameData3 == null) return;
        Gizmos.color = new Color(1f, 0.1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, flameData3.explosionRadius);
    }
}
