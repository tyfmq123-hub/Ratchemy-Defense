using UnityEngine;

public class SceneExit : MonoBehaviour
{
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
