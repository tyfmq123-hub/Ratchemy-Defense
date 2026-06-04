using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentGaugeUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public Image gaugeImage;
    public TextMeshProUGUI gaugeText;

    [Header("단계별 원형 게이지 이미지")]
    public Sprite[] gaugeSprites;

    public void SetRemainingTime(
        float remainingTime,
        float totalWaitingTime
    )
    {
        // 남은 시간이 음수가 되지 않도록 제한합니다.
        remainingTime = Mathf.Max(
            0f,
            remainingTime
        );

        // 중앙 숫자를 표시합니다.
        // 5.2초가 남았다면 6으로 표시됩니다.
        gaugeText.text =
            Mathf.CeilToInt(remainingTime).ToString();

        // 스프라이트 배열이 비어 있거나
        // 전체 제한 시간이 0초 이하라면 계산하지 않습니다.
        if (
            gaugeImage == null ||
            gaugeSprites == null ||
            gaugeSprites.Length == 0 ||
            totalWaitingTime <= 0f
        )
        {
            return;
        }

        // 전체 시간 중 현재 남은 시간의 비율을 계산합니다.
        // 예: 20초 중 14초가 남았다면 0.7입니다.
        float remainingRatio =
            remainingTime / totalWaitingTime;

        // 스프라이트 배열 번호를 자동으로 계산합니다.
        // 이미지가 11장이면 사용할 번호는 0~10입니다.
        int spriteIndex =
            Mathf.CeilToInt(
                remainingRatio *
                (gaugeSprites.Length - 1)
            );

        // 배열 범위를 벗어나지 않도록 제한합니다.
        spriteIndex = Mathf.Clamp(
            spriteIndex,
            0,
            gaugeSprites.Length - 1
        );

        // 계산된 단계의 스프라이트를 표시합니다.
        gaugeImage.sprite =
            gaugeSprites[spriteIndex];
    }

    public void ShowWaveStart()
    {
        gaugeText.text = "GO!";

        if (
            gaugeImage != null &&
            gaugeSprites != null &&
            gaugeSprites.Length > 0
        )
        {
            gaugeImage.sprite =
                gaugeSprites[0];
        }
    }

    // Wave 4 적을 모두 처치하고 보스전에 진입했을 때 호출합니다.
    // 기존 숫자 카운트다운 대신 STAGE 문구를 표시합니다.
    public void ShowBossStage()
    {
        if (gaugeText != null)
        {
            gaugeText.text = "STAGE";
        }
    }

    public void ShowAllWavesComplete()
    {
        gaugeText.text = "CLEAR";
    }
}