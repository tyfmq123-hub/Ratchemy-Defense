using UnityEngine;

// 다른 씬으로 이동할 때 필요한 Unity 기능입니다.
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    // START 버튼을 클릭하면 실행되는 메서드입니다.
    public void MoveToStageScene()
    {
        // "StageScene"이라는 이름의 씬으로 이동합니다.
        //
        // 주의:
        // 실제로 이동하려면 StageScene.unity 파일이 존재해야 하고,
        // 나중에 Build Profiles에도 해당 씬을 등록해야 합니다.
        SceneManager.LoadScene("2.StageScene");
    }

    // EXIT 버튼을 클릭하면 실행되는 메서드입니다.
    // EXIT 버튼을 클릭하면 실행됩니다.
    public void ExitGame()
    {
        // Unity 에디터에서 테스트할 때는
        // 상단의 Play 버튼을 다시 누른 것처럼 실행 모드를 종료합니다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // 실제로 빌드한 게임에서는 프로그램을 종료합니다.
#else
    Application.Quit();
#endif
    }
}