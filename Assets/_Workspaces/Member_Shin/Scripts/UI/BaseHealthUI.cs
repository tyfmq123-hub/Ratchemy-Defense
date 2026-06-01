using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseHealthUI : MonoBehaviour
{
    [Header("연결할 UI")]
    public Image baseHealthFill;
    public TextMeshProUGUI baseHealthText;
    
    [Header("온도 설정")]
    public float maxTemperature = 100f;
    
    [Range(0f, 100f)]
    public float previewTemperature = 0f;
    
    [Header("온도별 색상")]
    public Color normalColor = Color.cyan;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    void Start()
    {
        SetTemperature(previewTemperature);
    }

    void Update()
    {
        SetTemperature(previewTemperature);
    }

    public void SetTemperature(float currentTemperature)
    {
        currentTemperature = Mathf.Clamp(
            currentTemperature,
            0f,
            maxTemperature
        );

        float temperatureRatio =
            currentTemperature / maxTemperature;

        baseHealthFill.fillAmount = temperatureRatio;
        Color currentColor;

        if (temperatureRatio < 0.5f)
        {
            currentColor = Color.Lerp(
                normalColor,
                warningColor,
                temperatureRatio * 2f
            );
        }
        else
        {
           currentColor = Color.Lerp(
                warningColor,
                dangerColor,
                (temperatureRatio - 0.5f) * 2f
           );
        }

        baseHealthFill.color = currentColor;
        baseHealthText.color = currentColor;
        baseHealthText.text =
        Mathf.RoundToInt(currentTemperature) + "°C";
    }
}


