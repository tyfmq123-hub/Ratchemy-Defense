using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "EnemyUnit/BossData")]
public class BossData : EnemyUnitData
{
    [Header("보스 전용")]
    [Tooltip("사망 애니메이션 재생 시간 (초)")]
    public float dieAnimDuration = 1.0f;

    [Header("포물선 투사체")]
    [Tooltip("발사할 투사체 프리팹 (BossProjectile 컴포넌트 필요)")]
    public GameObject projectilePrefab;

    [Tooltip("착탄 시 범위 데미지 반지름")]
    public float explosionRadius = 2.0f;

    [Tooltip("포물선 정점 높이 (클수록 높이 날아감)")]
    public float arcHeight = 3.0f;

    [Tooltip("투사체 속도 (유닛/초) - 거리에 따라 도달 시간 자동 계산")]
    public float projectileSpeed = 10f;

    [Header("폭발 이펙트")]
    [Tooltip("착탄 시 생성할 이펙트 프리팹 (없으면 이펙트 생략)")]
    public GameObject explosionEffectPrefab;

    [Tooltip("이펙트 자동 제거 시간 (초) — Particle Stop Action이 없는 경우 사용")]
    public float effectLifetime = 1.0f;

    [Header("화염 오라 버프")]
    [Tooltip("오라 데미지 반지름")]
    public float auraRadius = 3.0f;

    [Tooltip("오라 1틱 데미지")]
    public int auraDamage = 5;

    [Tooltip("오라 틱 간격 (초)")]
    public float auraInterval = 2.0f;
}
