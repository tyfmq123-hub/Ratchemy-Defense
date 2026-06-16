using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DefeatResultUI : MonoBehaviour
{
    [Header("이동 버튼")]
    [SerializeField] private Button buttonRetry;
    [SerializeField] private Button buttonHome;
    [SerializeField] private Button buttonExit;

    [Header("이동할 씬 이름")]
    [SerializeField] private string retrySceneName = "5.BattleScene";
    [SerializeField] private string homeSceneName = "1.StartScene";

    private ResultUICanvas resultCanvas;

    private void Awake()
    {
        resultCanvas = GetComponentInParent<ResultUICanvas>();

        if (buttonRetry == null)
            buttonRetry = FindButton("ButtonRetry");

        if (buttonHome == null)
            buttonHome = FindButton("Button Home", "ButtonHome");

        if (buttonExit == null)
            buttonExit = FindButton("Button Exit", "ButtonExit");

        if (buttonRetry != null)
            buttonRetry.onClick.AddListener(OnClickRetry);

        if (buttonHome != null)
            buttonHome.onClick.AddListener(OnClickHome);

        if (buttonExit != null)
            buttonExit.onClick.AddListener(OnClickExit);
    }

    private void OnDestroy()
    {
        if (buttonRetry != null)
            buttonRetry.onClick.RemoveListener(OnClickRetry);

        if (buttonHome != null)
            buttonHome.onClick.RemoveListener(OnClickHome);

        if (buttonExit != null)
            buttonExit.onClick.RemoveListener(OnClickExit);
    }

    public void Show()
    {
        if (resultCanvas == null)
            resultCanvas = GetComponentInParent<ResultUICanvas>();

        resultCanvas?.BringToFront();
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[DefeatResultUI] 패배 화면 표시");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnClickRetry()
    {
        Time.timeScale = 1f;

        if (AppManager.Instance != null)
        {
            AppManager.Instance.Retry();
            return;
        }

        SceneManager.LoadScene(retrySceneName);
    }

    public void OnClickHome()
    {
        LoadScene(homeSceneName);
    }

    public void OnClickExit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

    private Button FindButton(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            Transform child = transform.Find(objectName);

            if (child == null)
                continue;

            Button button = child.GetComponent<Button>();

            if (button != null)
                return button;
        }

        return null;
    }
}
