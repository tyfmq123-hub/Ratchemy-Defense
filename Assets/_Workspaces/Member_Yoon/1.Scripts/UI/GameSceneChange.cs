using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneChange : MonoBehaviour
{
    public string SceneName;
     public void MoveToStoryScene()
    {
        
        SceneManager.LoadScene(SceneName);
    }
}
