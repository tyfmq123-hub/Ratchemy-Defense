using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryResultUI : MonoBehaviour
{
    [Header("기지 온도")]
    [SerializeField] private BaseHealth baseHealth;

    [Header("승리 결과 텍스트")]
    [SerializeField] private TextMeshProUGUI maGradeText;
    [SerializeField] private TextMeshProUGUI saPercentText;

    [Header("이동 버튼")]
    [SerializeField] private Button buttonNext;
    [SerializeField] private Button buttonHome;

    [Header("이동할 씬 이름")]
    [SerializeField] private string nextSceneName = "2.StageScene";
    [SerializeField] private string homeSceneName = "1.StartScene";

    private ResultUICanvas resultCanvas;

    private void Awake()
    {
        resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (baseHealth == null)
            baseHealth = FindFirstObjectByType<BaseHealth>();

        if (buttonNext == null)
            buttonNext = transform.Find("ButtonNext")?.GetComponent<Button>();

        if (buttonHome == null)
            buttonHome = transform.Find("ButtonHome")?.GetComponent<Button>();

        if (buttonNext != null)
            buttonNext.onClick.AddListener(OnClickNext);

        if (buttonHome != null)
            buttonHome.onClick.AddListener(OnClickHome);
    }

    private void OnDestroy()
    {
        if (buttonNext != null)
            buttonNext.onClick.RemoveListener(OnClickNext);

        if (buttonHome != null)
            buttonHome.onClick.RemoveListener(OnClickHome);
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

    public void OnClickNext()
    {
        LoadScene(nextSceneName);
    }

    public void OnClickHome()
    {
        LoadScene(homeSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        Time.timeScale = 1f;

        if (AppManager.Instance != null)
            AppManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
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
