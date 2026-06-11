using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private bool runInBackground = true;

    private string currentSubScene;
    private Coroutine transitionCoroutine;

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
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionScene(sceneName));
    }

    public void Retry()
    {
        if (!string.IsNullOrEmpty(currentSubScene))
            LoadScene(currentSubScene);
    }

    private IEnumerator TransitionScene(string sceneName)
    {
        Time.timeScale = 1f;

        string previousSubScene = currentSubScene;
        bool hasPreviousScene = !string.IsNullOrEmpty(previousSubScene);

        if (hasPreviousScene)
            SetSceneCamerasEnabled(previousSubScene, false);

        currentSubScene = sceneName;

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
            yield return null;

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        if (hasPreviousScene)
            yield return SceneManager.UnloadSceneAsync(previousSubScene);

        transitionCoroutine = null;
    }

    private static void SetSceneCamerasEnabled(
        string sceneName,
        bool enabled
    )
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            foreach (Camera camera in rootObject.GetComponentsInChildren<Camera>(true))
                camera.enabled = enabled;
        }
    }
}
