using System.Collections;
using UnityEngine;

// 보스 포물선 투사체 - 발사 후 목표 지점까지 호를 그리며 이동, 착탄 시 범위 데미지
// Collider2D (IsTrigger) 불필요 - 착탄 판정은 Physics2D.OverlapCircleAll 사용
public class BossProjectile : MonoBehaviour
{
    private int damage;
    private float explosionRadius;
    private LayerMask targetLayer;
    private GameObject explosionEffectPrefab;
    private float effectLifetime;
    private System.Action onLanded;

    // startPos → targetPos 까지 포물선 이동 후 범위 데미지
    // arcHeight  : 포물선 정점 높이 (클수록 높이 날아감)
    // travelTime : 도달 시간 (초)
    // onLanded   : 착탄 완료 시 호출할 콜백 (보스가 다음 공격 잠금 해제에 사용)
    public void Initialize(Vector2 startPos, Vector2 targetPos,
                           float arcHeight, int dmg,
                           float expRadius, LayerMask layer, float travelTime,
                           GameObject effectPrefab = null, float effLifetime = 1f,
                           System.Action landedCallback = null)
    {
        damage                = dmg;
        explosionRadius       = expRadius;
        targetLayer           = layer;
        explosionEffectPrefab = effectPrefab;
        effectLifetime        = effLifetime;
        onLanded              = landedCallback;

        StartCoroutine(FlyRoutine(startPos, targetPos, arcHeight, travelTime));
    }

    private IEnumerator FlyRoutine(Vector2 start, Vector2 end, float arcHeight, float travelTime)
    {
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            // x: 선형 보간  /  y: 선형 보간 + 포물선 오프셋
            float x = Mathf.Lerp(start.x, end.x, t);
            float y = Mathf.Lerp(start.y, end.y, t) + arcHeight * 4f * t * (1f - t);

            transform.position = new Vector3(x, y, transform.position.z);

            // 비행 방향으로 회전 (스프라이트가 날아가는 방향을 바라보게)
            if (elapsed + Time.deltaTime < travelTime)
            {
                float nextT   = Mathf.Clamp01((elapsed + Time.deltaTime) / travelTime);
                float nextX   = Mathf.Lerp(start.x, end.x, nextT);
                float nextY   = Mathf.Lerp(start.y, end.y, nextT) + arcHeight * 4f * nextT * (1f - nextT);
                Vector2 dir   = new Vector2(nextX - x, nextY - y);
                float angle   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        // 목표 지점 정확히 맞춤
        transform.position = new Vector3(end.x, end.y, transform.position.z);

        Explode(end);
    }

    private void Explode(Vector2 center)
    {
        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab,
                                            new Vector3(center.x, center.y, transform.position.z),
                                            Quaternion.identity);

            // 1순위: ParticleSystem → 재생 완료 후 자동 제거
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
            }
            else
            {
                // 2순위: Animator → 첫 번째 클립 길이를 읽어 자동 제거
                Animator anim = effect.GetComponent<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
                    float clipLength = clips.Length > 0 ? clips[0].length : effectLifetime;
                    Destroy(effect, clipLength);
                }
                else
                {
                    // 3순위: effectLifetime 수동 지정
                    Destroy(effect, effectLifetime);
                }
            }
        }

        // 범위 데미지
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius, targetLayer);
        foreach (Collider2D col in hits)
        {
            PlayerUnitBase player = col.GetComponentInParent<PlayerUnitBase>();
            if (player != null)
                player.TakeDamage(damage);
        }

        // 보스에게 착탄 완료 알림 → 다음 공격 잠금 해제
        onLanded?.Invoke();

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
