using UnityEngine;

// 번개도마뱀 2단계 전용 데이터 - 1단계 스탯 + 원거리 번개 공격
[CreateAssetMenu(fileName = "ThunderLizardData_T2", menuName = "EnemyUnit/ThunderLizardData_T2")]
public class ThunderLizardTier2Data : ThunderLizardData
{
    [Header("원거리 번개 공격 (2단계)")]
    public GameObject lightningPrefab;
    public float projectileSpeed = 6f;
    [Tooltip("투사체 발사 위치 오프셋 (피벗 기준, 예: Y=1.0이면 1유닛 위에서 발사)")]
    public Vector2 fireOffset = Vector2.zero;
}
