using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 스테이지 진입 직전에 Warning 이미지를 깜빡이는 연출만 담당합니다.
/// 웨이브 진행, 적 스폰, 보스 로직은 건드리지 않습니다.
/// </summary>
public class BossStageWarningUI : MonoBehaviour
{
    [Header("연결할 Warning 이미지 오브젝트")]
    [Tooltip("Inspector에서 Warning UI Image가 붙은 자식 GameObject를 연결하세요.")]
    public GameObject warningImageObject;

    [Header("깜빡임 설정")]
    [Tooltip("이미지를 켜고 끄는 횟수입니다. 기본값은 4번입니다.")]
    public int blinkCount = 4;

    [Tooltip("켜짐/꺼짐 사이의 대기 시간(초)입니다. 기본값은 0.25초입니다.")]
    public float blinkInterval = 0.25f;

    // 현재 실행 중인 깜빡임 코루틴을 저장합니다.
    // 연출 중 다시 PlayWarning()이 호출되면 이 코루틴을 먼저 정리합니다.
    private Coroutine warningCoroutine;

    void Start()
    {
        // Play 시작 시 Warning 이미지는 보이지 않은 상태로 둡니다.
        SetWarningImageVisible(false);
    }

    /// <summary>
    /// 외부(WaveManager 등)에서 보스 진입 시점에 호출합니다.
    /// Warning 이미지를 일정 간격으로 깜빡인 뒤 다시 숨깁니다.
    /// </summary>
    public void PlayWarning()
    {
        // Warning 이미지가 연결되지 않았다면 아무 것도 하지 않습니다.
        if (warningImageObject == null)
        {
            return;
        }

        // 이미 연출 중이라면 기존 코루틴을 멈추고 처음부터 다시 시작합니다.
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        warningCoroutine = StartCoroutine(BlinkWarningRoutine());
    }

    /// <summary>
    /// Warning 이미지를 blinkCount만큼 깜빡인 뒤 반드시 숨깁니다.
    /// </summary>
    private IEnumerator BlinkWarningRoutine()
    {
        // blinkCount번 반복합니다.
        // 예: blinkCount = 4이면 켜짐 → 꺼짐을 4번 수행합니다.
        for (int i = 0; i < blinkCount; i++)
        {
            // 1) Warning 이미지를 켭니다.
            SetWarningImageVisible(true);

            // Time.timeScale의 영향을 받지 않도록 실시간 대기를 사용합니다.
            yield return new WaitForSecondsRealtime(blinkInterval);

            // 2) Warning 이미지를 끕니다.
            SetWarningImageVisible(false);

            yield return new WaitForSecondsRealtime(blinkInterval);
        }

        // 연출이 끝나면 Warning 이미지를 반드시 숨깁니다.
        SetWarningImageVisible(false);

        // 코루틴 참조를 비워 두어 다음 호출을 준비합니다.
        warningCoroutine = null;
    }

    /// <summary>
    /// 연결된 Warning 이미지 자식 오브젝트만 켜거나 끕니다.
    /// 이 스크립트가 붙은 오브젝트 자체는 비활성화하지 않습니다.
    /// </summary>
    private void SetWarningImageVisible(bool isVisible)
    {
        if (warningImageObject == null)
        {
            return;
        }

        warningImageObject.SetActive(isVisible);
    }
}
