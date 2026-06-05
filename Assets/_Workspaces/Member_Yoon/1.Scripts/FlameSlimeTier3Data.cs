using UnityEngine;

// 화염슬라임 3단계 전용 데이터 - 2단계 스탯 + 폭발 + 원거리 공격
[CreateAssetMenu(fileName = "FlameSlimeData_T3", menuName = "EnemyUnit/FlameSlimeData_T3")]
public class FlameSlimeTier3Data : FlameSlimeTier2Data
{
    [Header("폭발 (3단계)")]
    public float explosionDelay = 3f;
    public float explosionAnimDuration = 0.5f;
    public float explosionRadius = 3f;
    public int explosionDamage = 20;
    [Tooltip("폭발 판정 중심점 오프셋 (슬라임 피벗이 발 아래일 때 Y값을 올려 시각적 중심에 맞춤)")]
    public Vector2 explosionOffset = Vector2.zero;

    [Header("원거리 공격 (3단계)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 5f;
}
