using UnityEngine;

public class GameSceneChange : MonoBehaviour
{
    public string SceneName;

    public void MoveToStoryScene()
    {
        AppManager.Instance.LoadScene(SceneName);
    }
}
