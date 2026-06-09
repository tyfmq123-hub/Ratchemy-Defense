using TMPro;
using UnityEngine;

public class VictoryResultUI : MonoBehaviour
{
    [Header("기지 온도")]
    [SerializeField] private BaseHealth baseHealth;

    [Header("승리 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI maGradeText;
    [SerializeField] private TextMeshProUGUI saPercentText;

    private ResultUICanvas resultCanvas;

    private void Awake()
    {
        resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (baseHealth == null)
            baseHealth = FindFirstObjectByType<BaseHealth>();
    }

    public void Show()
    {
        if (resultCanvas == null)
            resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (baseHealth == null)
            baseHealth = FindFirstObjectByType<BaseHealth>();

        resultCanvas?.BringToFront();
        UpdateScoreTexts();
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[VictoryResultUI] 승리 화면 표시");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateScoreTexts()
    {
        float temperature = baseHealth != null ? baseHealth.currentTemperature : 0f;
        var result = EvaluateBaseScore(temperature);

        if (maGradeText != null)
            maGradeText.text = result.grade;

        if (saPercentText != null)
            saPercentText.text = $"{result.percent}%";
    }

    private static (int percent, string grade) EvaluateBaseScore(float temperature)
    {
        if (temperature < 20f)
            return (100, "Perfect");

        if (temperature < 40f)
            return (80, "Normal");

        if (temperature < 60f)
            return (60, "중간");

        if (temperature < 80f)
            return (40, "나쁨");

        if (temperature < 100f)
            return (20, "위험");

        return (0, "실패");
    }
}
