using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 새로운 웨이브가 시작될 때
/// 화면 가운데에 "WAVE 1", "WAVE 2" 같은 안내 문구를
/// 잠깐 보여주고 자동으로 숨기는 역할만 담당합니다.
///
/// 실제 적 소환이나 웨이브 진행은 건드리지 않습니다.
/// </summary>
public class WaveAnnouncementUI : MonoBehaviour
{
    [Header("연결할 중앙 웨이브 텍스트")]
    [Tooltip("Canvas 아래에 만든 WaveAnnouncementText를 연결하세요.")]
    public TextMeshProUGUI waveAnnouncementText;

    [Header("표시 시간 설정")]
    [Tooltip("텍스트가 화면에 완전히 보이는 시간입니다.")]
    public float displayDuration = 1.2f;

    [Tooltip("텍스트가 서서히 사라지는 시간입니다.")]
    public float fadeDuration = 0.4f;

    // 이미 실행 중인 연출을 기억합니다.
    // 웨이브 표시가 겹치면 기존 연출을 멈추고 새 연출을 실행합니다.
    private Coroutine announcementCoroutine;

    private void Awake()
    {
        // 게임이 시작될 때는 웨이브 안내 문구가 보이지 않게 합니다.
        HideImmediately();
    }

    /// <summary>
    /// 외부 스크립트에서 호출할 함수입니다.
    /// 예: ShowWaveAnnouncement(1);
    /// 화면 중앙에 "WAVE 1"이 표시됩니다.
    /// </summary>
    public void ShowWaveAnnouncement(int waveNumber)
    {
        // TextMeshPro 연결이 빠졌다면 오류를 출력하고 종료합니다.
        if (waveAnnouncementText == null)
        {
            Debug.LogError("WaveAnnouncementUI: WaveAnnouncementText가 연결되지 않았습니다.");
            return;
        }

        // 기존 연출이 아직 실행 중이면 멈춥니다.
        // 웨이브가 빠르게 변경되어도 문구가 겹치지 않게 하기 위한 처리입니다.
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
        }

        // 새로운 웨이브 표시 연출을 시작합니다.
        announcementCoroutine = StartCoroutine(ShowAnnouncementRoutine(waveNumber));
    }

    /// <summary>
    /// 텍스트를 표시하고 일정 시간 뒤 서서히 사라지게 합니다.
    /// </summary>
    private IEnumerator ShowAnnouncementRoutine(int waveNumber)
    {
        // 표시할 글자를 설정합니다.
        waveAnnouncementText.text = $"WAVE {waveNumber}";

        // 완전히 보이는 상태로 만듭니다.
        SetTextAlpha(1f);
        waveAnnouncementText.gameObject.SetActive(true);

        // 잠시 유지합니다.
        yield return new WaitForSeconds(displayDuration);

        // 서서히 투명하게 만듭니다.
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 1에서 0으로 점점 줄어드는 값을 계산합니다.
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

            SetTextAlpha(alpha);

            yield return null;
        }

        // 연출이 끝나면 완전히 숨깁니다.
        HideImmediately();

        announcementCoroutine = null;
    }

    /// <summary>
    /// TextMeshPro 글자의 투명도를 변경합니다.
    /// </summary>
    private void SetTextAlpha(float alpha)
    {
        Color textColor = waveAnnouncementText.color;
        textColor.a = alpha;
        waveAnnouncementText.color = textColor;
    }

    /// <summary>
    /// 문구를 즉시 숨깁니다.
    /// </summary>
    private void HideImmediately()
    {
        if (waveAnnouncementText == null)
        {
            return;
        }

        SetTextAlpha(0f);
        waveAnnouncementText.gameObject.SetActive(false);
    }
}