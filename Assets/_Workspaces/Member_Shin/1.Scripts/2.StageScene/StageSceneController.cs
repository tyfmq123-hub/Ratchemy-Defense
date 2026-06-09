using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// StageScene에서 버튼을 눌렀을 때
/// 다음 씬으로 이동하는 기능을 관리합니다.
/// </summary>
public class StageSceneController : MonoBehaviour
{
    /// <summary>
    /// 열린 스테이지 버튼을 누르면 StoryScene으로 이동합니다.
    /// </summary>
    public void MoveToStoryScene()
    {
        SceneManager.LoadScene("3.StoryScene");
    }
}