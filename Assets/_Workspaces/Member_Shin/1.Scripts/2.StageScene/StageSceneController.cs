using UnityEngine;

public class StageSceneController : MonoBehaviour
{
    public void MoveToStoryScene()
    {
        AppManager.Instance.LoadScene("3.StoryScene");
    }
}