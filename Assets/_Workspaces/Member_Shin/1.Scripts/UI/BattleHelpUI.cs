using UnityEngine;

/// <summary>
/// 전투 화면의 도움말 UI를 열고 닫습니다.
///
/// 화면 오른쪽 위의 ? 버튼을 누르면
/// 전체 화면 도움말 오버레이가 열립니다.
///
/// X 버튼을 누르면 다시 닫힙니다.
/// </summary>
public class BattleHelpUI : MonoBehaviour
{
    [Header("도움말 전체 화면")]
    [SerializeField] private GameObject helpOverlay;

    /// <summary>
    /// 게임이 시작될 때 도움말은 닫힌 상태로 둡니다.
    /// </summary>
    private void Awake()
    {
        if (helpOverlay != null)
        {
            helpOverlay.SetActive(false);
        }
    }

    /// <summary>
    /// ? 버튼에서 호출합니다.
    /// 도움말 전체 화면을 표시합니다.
    /// </summary>
    public void OpenHelp()
    {
        if (helpOverlay == null)
        {
            Debug.LogError(
                "[BattleHelpUI] HelpOverlay가 연결되지 않았습니다."
            );

            return;
        }

        helpOverlay.SetActive(true);

        // 도움말을 보는 동안 전투를 잠시 멈춥니다.
        Time.timeScale = 0f;
    }

    /// <summary>
    /// X 버튼에서 호출합니다.
    /// 도움말 전체 화면을 닫습니다.
    /// </summary>
    public void CloseHelp()
    {
        if (helpOverlay == null)
        {
            return;
        }

        helpOverlay.SetActive(false);

        // 도움말을 닫으면 전투를 다시 진행합니다.
        Time.timeScale = 1f;
    }
}