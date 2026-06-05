using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseHealthUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public Image baseHealthFill;
    public TextMeshProUGUI baseHealthText;

    [Header("배터리 이미지 UI")]
    [SerializeField] private Image basePortraitImage;

    [Header("온도 단계별 배터리 이미지")]
    [SerializeField] private Sprite normalBatterySprite;   // 정상 상태 이미지
    [SerializeField] private Sprite crackedBatterySprite;  // 금이 간 이미지
    [SerializeField] private Sprite brokenBatterySprite;   // 부서진 이미지

    [Header("온도 설정")]
    public float maxTemperature = 130f;

    [Range(0f, 130f)]
    public float previewTemperature = 0f;

    [Header("온도별 색상")]
    public Color normalColor = Color.cyan;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    void Start()
    {
        // 게임 시작 시 초기 온도를 UI에 반영합니다.
        SetTemperature(previewTemperature);
    }

    public void SetTemperature(float currentTemperature)
    {
        // 온도가 0도보다 낮아지거나 최대 온도를 넘어가지 않도록 제한합니다.
        currentTemperature = Mathf.Clamp(
            currentTemperature,
            0f,
            maxTemperature
        );

        // 현재 온도가 최대 온도의 몇 퍼센트인지 계산합니다.
        // 예: 65도 / 130도 = 0.5
        float temperatureRatio =
            currentTemperature / maxTemperature;

        // 온도 게이지의 채워진 정도를 변경합니다.
        baseHealthFill.fillAmount = temperatureRatio;

        Color currentColor;

        // 0% ~ 50%:
        // 파란색에서 노란색으로 서서히 변합니다.
        if (temperatureRatio < 0.5f)
        {
            currentColor = Color.Lerp(
                normalColor,
                warningColor,
                temperatureRatio * 2f
            );
        }
        // 50% ~ 100%:
        // 노란색에서 빨간색으로 서서히 변합니다.
        else
        {
            currentColor = Color.Lerp(
                warningColor,
                dangerColor,
                (temperatureRatio - 0.5f) * 2f
            );
        }

        // 계산한 색상을 게이지에 적용합니다.
        baseHealthFill.color = currentColor;

        // 현재 온도를 텍스트로 표시합니다.
        baseHealthText.text =
            Mathf.RoundToInt(currentTemperature) + "°C";

        // 온도에 맞는 배터리 이미지를 표시합니다.
        UpdateBatteryPortrait(temperatureRatio);
    }

    private void UpdateBatteryPortrait(float temperatureRatio)
    {
        // 최대 온도의 50% 미만:
        // 아직 안전한 상태이므로 멀쩡한 이미지를 표시합니다.
        if (temperatureRatio < 0.5f)
        {
            basePortraitImage.sprite = normalBatterySprite;
        }
        // 최대 온도의 50% 이상, 80% 미만:
        // 경고 상태이므로 금이 간 이미지를 표시합니다.
        else if (temperatureRatio < 0.8f)
        {
            basePortraitImage.sprite = crackedBatterySprite;
        }
        // 최대 온도의 80% 이상:
        // 위험 상태이므로 부서진 이미지를 표시합니다.
        else
        {
            basePortraitImage.sprite = brokenBatterySprite;
        }
    }
}