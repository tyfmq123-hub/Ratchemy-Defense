using TMPro;
using UnityEngine;

/// <summary>
/// 화면 상단 중앙에 남은 시간을 표시하는 UI입니다.
///
/// 기존에는 원형 웨이브 게이지 이미지도 함께 변경했지만,
/// 이제는 게이지를 사용하지 않고 타이머 숫자만 표시합니다.
///
/// 클래스 이름은 기존 WaveManager 연결을 유지하기 위해
/// EnvironmentGaugeUI 그대로 사용합니다.
/// </summary>
public class EnvironmentGaugeUI : MonoBehaviour
{
    [Header("연결할 상단 타이머 텍스트")]
    [Tooltip("Canvas 아래에 만든 BattleTimerText를 연결하세요.")]
    public TextMeshProUGUI timerText;

    /// <summary>
    /// WaveManager가 매 프레임 호출합니다.
    /// 남은 시간을 01:25 형식으로 표시합니다.
    /// </summary>
    public void SetRemainingTime(
        float remainingTime,
        float totalWaitingTime
    )
    {
        // 시간이 0보다 작아지지 않도록 제한합니다.
        remainingTime = Mathf.Max(
            0f,
            remainingTime
        );

        // 연결이 빠져 있다면 오류 없이 종료합니다.
        if (timerText == null)
        {
            return;
        }

        // 5.2초가 남았다면 6초로 표시합니다.
        // 화면에서 시간이 너무 일찍 0초로 보이는 것을 방지합니다.
        int totalSeconds =
            Mathf.CeilToInt(remainingTime);

        // 전체 초를 분과 초로 나눕니다.
        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        // 예: 85초 → 01:25
        timerText.text =
            $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 일반 웨이브 시작 시 호출됩니다.
    ///
    /// WAVE 1, WAVE 2 같은 문구는
    /// WaveAnnouncementUI가 화면 중앙에 따로 표시합니다.
    /// 따라서 상단 타이머 문구는 변경하지 않습니다.
    /// </summary>
    public void ShowWaveStart()
    {
        // 아무 작업도 하지 않습니다.
    }

    /// <summary>
    /// 보스 스테이지 진입 시 호출됩니다.
    ///
    /// 보스 경고 연출은 BossStageWarningUI가 담당하므로
    /// 상단 타이머 문구는 변경하지 않습니다.
    /// </summary>
    public void ShowBossStage()
    {
        // 아무 작업도 하지 않습니다.
    }

    /// <summary>
    /// 모든 웨이브가 끝났을 때 호출됩니다.
    /// </summary>
    public void ShowAllWavesComplete()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = "00:00";
    }
}