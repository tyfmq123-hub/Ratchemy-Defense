using UnityEngine;

// 번개도마뱀 3단계 전용 데이터 - 2단계 스탯 + 체인 라이트닝
[CreateAssetMenu(fileName = "ThunderLizardData_T3", menuName = "EnemyUnit/ThunderLizardData_T3")]
public class ThunderLizardTier3Data : ThunderLizardTier2Data
{
    [Header("체인 라이트닝 (3단계)")]
    [Tooltip("첫 번째 대상 적중 후 추가로 튀는 최대 체인 횟수")]
    public int chainCount = 3;
    [Tooltip("체인이 튀는 탐색 반경")]
    public float chainRange = 3f;

    [Header("체인 라이트닝 선 (3단계)")]
    [Tooltip("번개 선 색상")]
    public Color lightningColor = new Color(1f, 0.95f, 0.3f, 1f);
    [Tooltip("번개 선 굵기")]
    public float lightningWidth = 0.05f;
    [Tooltip("번개 선이 유지되는 시간 (초)")]
    public float lightningDuration = 0.15f;
    [Tooltip("번개 지그재그 진폭")]
    public float lightningNoise = 0.2f;
}
