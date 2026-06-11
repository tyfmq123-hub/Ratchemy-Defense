using TMPro;
using UnityEngine;

/// <summary>
/// 화면 왼쪽 아래의 안전관리 점수 UI를 담당합니다.
///
/// 다른 스크립트에서 SetScore()를 호출하면
/// 안전관리 점수 숫자가 변경됩니다.
///
/// 점수 구간에 따라 숫자 색상도 자동으로 달라집니다.
/// </summary>
public class SafetyScoreUI : MonoBehaviour
{
    [Header("연결할 안전관리 점수 텍스트")]
    [Tooltip("SafetyScorePanel 아래의 SafetyScoreText를 연결하세요.")]
    public TextMeshProUGUI safetyScoreText;

    [Header("점수 설정")]
    [Tooltip("게임 시작 시 표시할 기본 점수입니다.")]
    public int startScore = 100;

    [Header("점수별 숫자 색상")]
    [Tooltip("안전한 상태일 때의 숫자 색상입니다. 71점 이상")]
    public Color safeColor = Color.white;

    [Tooltip("경고 상태일 때의 숫자 색상입니다. 41점 이상")]
    public Color warningColor = Color.yellow;

    [Tooltip("위험 상태일 때의 숫자 색상입니다. 21점 이상")]
    public Color dangerColor =
        new Color(
            1f,
            0.5f,
            0f
        );

    [Tooltip("매우 위험한 상태일 때의 숫자 색상입니다. 20점 이하")]
    public Color criticalColor = Color.red;

    // 현재 안전관리 점수입니다.
    private int currentScore;

    private void Awake()
    {
        // 게임이 시작되면 기본 점수를 화면에 표시합니다.
        SetScore(startScore);
    }

    /// <summary>
    /// 안전관리 점수를 변경하고 화면에 표시합니다.
    ///
    /// 점수는 0점보다 작아지거나
    /// 100점보다 커지지 않도록 제한합니다.
    /// </summary>
    public void SetScore(int newScore)
    {
        currentScore =
            Mathf.Clamp(
                newScore,
                0,
                100
            );

        UpdateScoreText();
    }

    /// <summary>
    /// 현재 점수를 일정량 증가시킵니다.
    /// 예: AddScore(10);
    /// </summary>
    public void AddScore(int amount)
    {
        SetScore(
            currentScore + amount
        );
    }

    /// <summary>
    /// 현재 점수를 일정량 감소시킵니다.
    /// 예: ReduceScore(20);
    /// </summary>
    public void ReduceScore(int amount)
    {
        SetScore(
            currentScore - amount
        );
    }

    /// <summary>
    /// 다른 스크립트에서 현재 점수를 확인할 때 사용합니다.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// 현재 점수를 실제 UI 텍스트에 표시합니다.
    /// 점수 구간에 따라 숫자 색상도 변경합니다.
    /// </summary>
    private void UpdateScoreText()
    {
        if (safetyScoreText == null)
        {
            Debug.LogError(
                "SafetyScoreUI: SafetyScoreText가 연결되지 않았습니다."
            );

            return;
        }

        // 현재 점수를 숫자로 표시합니다.
        safetyScoreText.text =
            currentScore.ToString();

        // 점수가 높을수록 안전한 상태입니다.
        //
        // 71 ~ 100 : 안전
        // 41 ~ 70  : 경고
        // 21 ~ 40  : 위험
        // 0  ~ 20  : 매우 위험
        if (currentScore >= 71)
        {
            safetyScoreText.color =
                safeColor;
        }
        else if (currentScore >= 41)
        {
            safetyScoreText.color =
                warningColor;
        }
        else if (currentScore >= 21)
        {
            safetyScoreText.color =
                dangerColor;
        }
        else
        {
            safetyScoreText.color =
                criticalColor;
        }
    }
}