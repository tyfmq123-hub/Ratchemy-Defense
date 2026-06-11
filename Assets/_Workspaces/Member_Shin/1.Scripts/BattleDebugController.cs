using UnityEngine;

public class BattleDebugController : MonoBehaviour
{
    [Header("연결할 실제 게임 스크립트")]
    [SerializeField] private GameManager gameManager;

    [SerializeField] private BossStageWarningUI BossStageWarningUI;

    // 보스 경고 장면만 따로 확인할 때 사용합니다.
    public void TestBossWarning()
    {
        if (BossStageWarningUI == null)
        {
            Debug.LogWarning("BossStageWarningUI가 연결되지 않았습니다.");
            return;
        }

        // 실수로 경고 UI 부모가 꺼져 있어도
        // 테스트 버튼을 누르면 먼저 활성화합니다.
        if (!BossStageWarningUI.gameObject.activeSelf)
        {
            BossStageWarningUI.gameObject.SetActive(true);
        }

        BossStageWarningUI.PlayWarning();
    }

    // 승리 화면만 빠르게 확인할 때 사용합니다.
    public void TestVictory()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager가 연결되지 않았습니다.");
            return;
        }

        gameManager.Victory();
    }

    // 패배 화면만 빠르게 확인할 때 사용합니다.
    public void TestDefeat()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager가 연결되지 않았습니다.");
            return;
        }

        gameManager.Defeat();
    }
}