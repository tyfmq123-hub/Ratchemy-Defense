using UnityEngine;

// 화염슬라임 2단계 전용 데이터 - 1단계 스탯 + 오라
[CreateAssetMenu(fileName = "FlameSlimeData_T2", menuName = "EnemyUnit/FlameSlimeData_T2")]
public class FlameSlimeTier2Data : FlameSlimeData
{
    [Header("오라 (2단계)")]
    public float auraRadius = 2f;
    public float auraDamage = 5f;
    public float auraInterval = 3f;
}
