using UnityEngine;

/// <summary>
/// 안전관리 점수의 실제 계산을 담당합니다.
///
/// SafetyScoreUI는 숫자를 화면에 표시하는 역할만 담당하고,
/// 점수가 몇 점이어야 하는지는 이 스크립트가 계산합니다.
///
/// 현재 1차 구현 범위:
/// 1. 보스가 살아 있기 때문에 기본적으로 10점을 감점합니다.
/// 2. 배터리 온도가 30도를 넘으면 점수를 추가로 감점합니다.
/// 3. 온도가 높아질수록 감점 폭이 점점 커집니다.
///
/// 다음 단계에서는 살아 있는 일반 적의 누적 압박도 추가합니다.
/// </summary>
public class SafetyScoreManager : MonoBehaviour
{
    [Header("연결할 안전관리 점수 UI")]
    [Tooltip("SafetyScorePanel에 붙어 있는 SafetyScoreUI를 연결하세요.")]
    public SafetyScoreUI safetyScoreUI;

    [Header("배터리 온도 기준")]
    [Tooltip("이 온도까지는 안전관리 점수를 깎지 않습니다.")]
    public float safeTemperature = 30f;

    [Tooltip("열폭주가 발생하는 최대 온도입니다.")]
    public float maxTemperature = 130f;

    [Header("보스 위험도")]
    [Tooltip("보스가 살아 있을 때 적용할 최대 감점입니다.")]
    public float maxBossPenalty = 10f;

    [Tooltip("현재 보스가 살아 있는지 여부입니다. 보스 연결 전에는 체크 상태로 둡니다.")]
    public bool isBossAlive = true;

    [Header("온도 위험도")]
    [Tooltip("온도가 최대치에 도달했을 때 적용할 최대 감점입니다.")]
    public float maxTemperaturePenalty = 45f;

    // 현재 배터리 온도입니다.
    private float currentTemperature;

    private void Start()
    {
        // 게임 시작 시 온도 0도를 기준으로
        // 안전관리 점수를 처음 계산합니다.
        //
        // 보스가 살아 있으므로 시작 점수는
        // 100 - 10 = 90점이 됩니다.
        RecalculateScore();
    }

    /// <summary>
    /// BaseHealth에서 배터리 온도가 변경될 때 호출합니다.
    /// </summary>
    public void SetTemperature(float newTemperature)
    {
        currentTemperature =
            Mathf.Clamp(
                newTemperature,
                0f,
                maxTemperature
            );

        RecalculateScore();
    }

    /// <summary>
    /// 보스가 사망했을 때 호출할 메서드입니다.
    ///
    /// 아직은 BossBase와 연결하지 않습니다.
    /// 다음 단계에서 보스 HP 기반 점수 계산으로 확장합니다.
    /// </summary>
    public void SetBossAlive(bool alive)
    {
        isBossAlive = alive;

        RecalculateScore();
    }

    /// <summary>
    /// 현재 상태를 기준으로 안전관리 점수를 다시 계산합니다.
    /// </summary>
    private void RecalculateScore()
    {
        if (safetyScoreUI == null)
        {
            Debug.LogError(
                "[SafetyScoreManager] SafetyScoreUI가 연결되지 않았습니다."
            );

            return;
        }

        // -----------------------------
        // 1. 보스 위험도 계산
        // -----------------------------
        //
        // 현재는 보스 HP를 아직 연결하지 않았기 때문에
        // 보스가 살아 있으면 10점,
        // 죽으면 0점을 감점합니다.
        float bossPenalty =
            isBossAlive
                ? maxBossPenalty
                : 0f;

        // -----------------------------
        // 2. 온도 위험도 계산
        // -----------------------------
        //
        // 30도까지는 감점하지 않습니다.
        //
        // 30도를 넘은 이후부터
        // 130도에 가까워질수록 감점 폭이 점점 커집니다.
        float temperaturePenalty =
            CalculateTemperaturePenalty();

        // -----------------------------
        // 3. 최종 점수 계산
        // -----------------------------
        //
        // 일반 적 누적 위험도는 다음 단계에서 추가합니다.
        float calculatedScore =
            100f
            - bossPenalty
            - temperaturePenalty;

        int roundedScore =
            Mathf.RoundToInt(
                calculatedScore
            );

        safetyScoreUI.SetScore(
            roundedScore
        );
    }

    /// <summary>
    /// 현재 배터리 온도를 기준으로
    /// 온도 위험도 감점 값을 계산합니다.
    /// </summary>
    private float CalculateTemperaturePenalty()
    {
        // 안전 구간이라면 감점하지 않습니다.
        if (currentTemperature <= safeTemperature)
        {
            return 0f;
        }

        // 30도부터 130도 사이에서
        // 현재 온도가 어느 정도 위치인지
        // 0 ~ 1 사이 값으로 변환합니다.
        float temperatureRatio =
            Mathf.InverseLerp(
                safeTemperature,
                maxTemperature,
                currentTemperature
            );

        // 온도가 낮을 때는 완만하게,
        // 온도가 높을 때는 빠르게 감점되도록
        // 1.5 제곱 곡선을 적용합니다.
        float curvedRatio =
            Mathf.Pow(
                temperatureRatio,
                1.5f
            );

        return
            maxTemperaturePenalty
            * curvedRatio;
    }
}