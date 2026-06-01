using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 번개도마뱀 원거리 투사체
// chainCount = 0 → 단일 타겟 적중 후 소멸 (2단계)
// chainCount > 0 → 첫 타겟 적중 후 chainRange 내 최대 chainCount명에게 연쇄 (3단계)
// 프리팹 필수 설정: Collider2D (IsTrigger = true) + Rigidbody2D (Kinematic, Gravity Scale = 0)
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LightningProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private int chainCount;
    private float chainRange;

    private GameObject chainHitEffectPrefab;
    private Color lightningColor  = new Color(1f, 0.95f, 0.3f, 1f);
    private float lightningWidth   = 0.05f;
    private float lightningDuration = 0.15f;
    private float lightningNoise   = 0.2f;

    [SerializeField] private float lifetime = 5f;

    public void Initialize(Vector3 dir, float spd, int dmg, int chain, float range, Collider2D ownerCollider = null,
        ThunderLizardTier3Data effectData = null)
    {
        direction  = dir;
        speed      = spd;
        damage     = dmg;
        chainCount = chain;
        chainRange = range;

        if (ownerCollider != null)
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), ownerCollider);

        if (effectData != null)
        {
            chainHitEffectPrefab = effectData.chainHitEffectPrefab;
            lightningColor       = effectData.lightningColor;
            lightningWidth       = effectData.lightningWidth;
            lightningDuration    = effectData.lightningDuration;
            lightningNoise       = effectData.lightningNoise;
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerUnitBase player = other.GetComponentInParent<PlayerUnitBase>();
        if (player == null) return;

        player.TakeDamage(damage);
        Debug.Log($"[LightningProjectile] 적중 → {other.gameObject.name} (데미지: {damage})");

        if (chainCount > 0)
            ChainLightning(player);

        Destroy(gameObject);
    }

    private void ChainLightning(PlayerUnitBase firstHit)
    {
        LayerMask playerLayer = LayerMask.GetMask("Player");
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, chainRange, playerLayer);

        Debug.Log($"[ChainLightning] 탐색 시작 - 범위: {chainRange} / 감지된 콜라이더 수: {nearby.Length} / 최대 체인: {chainCount}");

        System.Array.Sort(nearby, (a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        List<PlayerUnitBase> alreadyHit = new List<PlayerUnitBase> { firstHit };
        int chains = 0;

        foreach (Collider2D col in nearby)
        {
            if (chains >= chainCount) break;

            PlayerUnitBase target = col.GetComponentInParent<PlayerUnitBase>();
            if (target == null)
            {
                Debug.Log($"[ChainLightning] 건너뜀 - PlayerUnitBase 없음 ({col.gameObject.name})");
                continue;
            }
            if (alreadyHit.Contains(target))
            {
                Debug.Log($"[ChainLightning] 건너뜀 - 이미 맞은 대상 ({col.gameObject.name})");
                continue;
            }

            Vector3 prevPos = alreadyHit[alreadyHit.Count - 1].transform.position;
            Vector3 targetPos = target.transform.position;

            SpawnLightningLine(prevPos, targetPos);
            SpawnHitEffect(targetPos);

            target.TakeDamage(damage);
            alreadyHit.Add(target);
            chains++;

            Debug.Log($"[ChainLightning] 체인 {chains}/{chainCount} 적중 → {col.gameObject.name} (데미지: {damage})");
        }

        if (chains == 0)
            Debug.Log("[ChainLightning] 체인 대상 없음 - 범위 내 추가 타겟이 없거나 모두 이미 맞음");
        else
            Debug.Log($"[ChainLightning] 완료 - 총 {chains}명 추가 적중");
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (chainHitEffectPrefab == null) return;
        Instantiate(chainHitEffectPrefab, position, Quaternion.identity);
    }

    private void SpawnLightningLine(Vector3 from, Vector3 to)
    {
        GameObject obj = new GameObject("LightningLine");
        LineRenderer lr = obj.AddComponent<LineRenderer>();

        lr.material        = new Material(Shader.Find("Sprites/Default"));
        lr.startColor      = lightningColor;
        lr.endColor        = lightningColor;
        lr.startWidth      = lightningWidth;
        lr.endWidth        = lightningWidth;
        lr.useWorldSpace   = true;
        lr.sortingOrder    = 10;

        // 지그재그 포인트 생성
        int segments = 8;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t      = (float)i / (segments - 1);
            Vector3 pos  = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments - 1)
            {
                pos += (Vector3)(Random.insideUnitCircle * lightningNoise);
            }
            lr.SetPosition(i, pos);
        }

        Destroy(obj, lightningDuration);
    }
}
