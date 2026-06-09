using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRetry : MonoBehaviour
{
    public string SceneName;
     public void MoveToStoryScene()
    {
        
        SceneManager.LoadScene(SceneName);
    }
}
