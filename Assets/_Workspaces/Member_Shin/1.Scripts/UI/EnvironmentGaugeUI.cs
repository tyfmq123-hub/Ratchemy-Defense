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
        float secondsPerStep
    )
    {
        // 남은 시간이 음수가 되지 않도록 제한합니다.
        // 예를 들어 실제 계산 과정에서 -0.02초처럼 아주 작은 음수가 생겨도
        // 화면에는 항상 0 이상의 값만 표시되도록 합니다.
        remainingTime = Mathf.Max(
            0f,
            remainingTime
        );

        // 중앙에 표시할 남은 시간을 정수로 바꿉니다.
        // CeilToInt를 사용하면 5.2초가 남았을 때 6으로 표시됩니다.
        // 실제 시간이 완전히 끝나기 전에 숫자가 먼저 0으로 보이는 현상을 막아줍니다.
        gaugeText.text =
            Mathf.CeilToInt(remainingTime).ToString();

        // 현재 남아 있어야 하는 원형 칸 개수를 계산합니다.
        // 예를 들어 한 칸 감소 시간이 2초이고 남은 시간이 9초라면
        // 9 ÷ 2 = 4.5이므로 올림 처리하여 5칸을 보여줍니다.
        int remainingStepCount =
            Mathf.CeilToInt(
                remainingTime / secondsPerStep
            );

        // 배열 범위를 벗어나지 않도록 제한합니다.
        // 이미지가 총 9장이라면 사용할 수 있는 번호는 0~8입니다.
        // 시간이 길게 설정되어도 존재하지 않는 이미지 번호를 찾지 않게 합니다.
        remainingStepCount = Mathf.Clamp(
            remainingStepCount,
            0,
            gaugeSprites.Length - 1
        );

        // 계산한 단계에 해당하는 원형 게이지 이미지를 적용합니다.
        // Inspector 배열의 0번에는 빈 이미지,
        // 마지막 번호에는 모든 칸이 남아 있는 이미지를 넣어야 합니다.
        if (
            gaugeImage != null &&
            gaugeSprites != null &&
            gaugeSprites.Length > 0
        )
        {
            gaugeImage.sprite =
                gaugeSprites[remainingStepCount];
        }
    }

    public void ShowWaveStart()
    {
        // 카운트다운이 끝나고 적 웨이브가 시작될 때 호출합니다.
        // 중앙 숫자 대신 GO! 문구를 잠시 보여줍니다.
        // 원하지 않으면 "GO!" 대신 빈 문자열을 넣어도 됩니다.
        gaugeText.text = "GO!";

        // 웨이브가 시작되면 원형 칸을 모두 비운 상태로 표시합니다.
        // 배열의 0번에는 칸이 모두 사라진 이미지를 넣습니다.
        // 다음 웨이브 대기 시간이 시작되면 다시 시간이 표시됩니다.
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

    public void ShowAllWavesComplete()
    {
        // 등록된 웨이브가 모두 끝났을 때 표시합니다.
        // 현재는 적 생성 완료 상태를 알려주는 용도로 사용합니다.
        // 추후 승리 조건을 따로 만들면 BattleManager와 연결할 수 있습니다.
        gaugeText.text = "CLEAR";
    }
}