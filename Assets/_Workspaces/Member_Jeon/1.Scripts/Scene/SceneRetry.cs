using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRetry : MonoBehaviour
{
    public string SceneName;

    public void MoveToStoryScene()
    {
        if (string.IsNullOrEmpty(SceneName))
        {
            Debug.LogWarning("[SceneRetry] SceneName이 비어 있습니다.");
            return;
        }

        if (AppManager.Instance != null)
            AppManager.Instance.LoadScene(SceneName);
        else
            SceneManager.LoadScene(SceneName);
    }
}
