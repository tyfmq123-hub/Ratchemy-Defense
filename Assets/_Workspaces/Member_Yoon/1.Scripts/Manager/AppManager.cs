using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private bool runInBackground = true;

    private string currentSubScene;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Application.runInBackground = runInBackground;
    }

    private void Start()
    {
        LoadScene("1.StartScene");
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionScene(sceneName));
    }

    public void Retry()
    {
        if (!string.IsNullOrEmpty(currentSubScene))
            LoadScene(currentSubScene);
    }

    private IEnumerator TransitionScene(string sceneName)
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(currentSubScene))
            yield return SceneManager.UnloadSceneAsync(currentSubScene);

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        currentSubScene = sceneName;
    }
}
