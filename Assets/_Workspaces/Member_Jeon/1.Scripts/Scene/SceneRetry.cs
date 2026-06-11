using UnityEngine;

public class SceneRetry : MonoBehaviour
{
    public string SceneName;

    public void MoveToStoryScene()
    {
        AppManager.Instance.LoadScene(SceneName);
    }
}
