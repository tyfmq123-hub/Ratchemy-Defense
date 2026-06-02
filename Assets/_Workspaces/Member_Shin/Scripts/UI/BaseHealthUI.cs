using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseHealthUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public Image baseHealthFill;
    public TextMeshProUGUI baseHealthText;

    [Header("온도 설정")]
    public float maxTemperature = 130f;

    [Range(0f, 130f)]
    public float previewTemperature = 25f;

    [Header("온도별 색상")]
    public Color normalColor = Color.cyan;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    void Start()
    {
        // 게임 시작 시 인스펙터에 설정한 미리보기 온도를 표시합니다.
        SetTemperature(previewTemperature);
    }

    public void SetTemperature(float currentTemperature)
    {
        // 온도가 0°C보다 작거나 최대 온도를 넘지 않도록 제한합니다.
        currentTemperature = Mathf.Clamp(
            currentTemperature,
            0f,
            maxTemperature
        );

        // 현재 온도를 게이지 비율로 바꿉니다.
        // 예: 65°C / 130°C = 0.5
        float temperatureRatio =
            currentTemperature / maxTemperature;

        // 게이지 길이를 변경합니다.
        baseHealthFill.fillAmount = temperatureRatio;

        Color currentColor;

        // 게이지가 절반보다 낮으면 청록색에서 노란색으로 변합니다.
        if (temperatureRatio < 0.5f)
        {
            currentColor = Color.Lerp(
                normalColor,
                warningColor,
                temperatureRatio * 2f
            );
        }
        // 게이지가 절반 이상이면 노란색에서 빨간색으로 변합니다.
        else
        {
            currentColor = Color.Lerp(
                warningColor,
                dangerColor,
                (temperatureRatio - 0.5f) * 2f
            );
        }

        // 게이지와 숫자 색상을 함께 변경합니다.
        baseHealthFill.color = currentColor;
        baseHealthText.color = currentColor;

        // 현재 온도를 화면에 표시합니다.
        baseHealthText.text =
            Mathf.RoundToInt(currentTemperature) + "°C";
    }
}