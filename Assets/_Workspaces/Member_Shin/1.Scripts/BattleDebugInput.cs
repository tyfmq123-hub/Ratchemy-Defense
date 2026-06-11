using UnityEngine;

/// <summary>
/// 배틀 씬 테스트용 단축키.
/// V: 승리 화면, D: 패배 화면, W: 보스 워닝 사인
/// </summary>
public class BattleDebugInput : MonoBehaviour
{
    [Header("연결할 실제 게임 스크립트")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossStageWarningUI bossStageWarningUI;

    [Header("단축키 사용 여부")]
    [SerializeField] private bool enableDebugKeys = true;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (bossStageWarningUI == null)
        {
            bossStageWarningUI =
                FindFirstObjectByType<BossStageWarningUI>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void Update()
    {
        if (!enableDebugKeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ShowVictory();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            ShowDefeat();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            ShowWarning();
        }
    }

    private void ShowVictory()
    {
        if (gameManager == null)
        {
            Debug.LogWarning(
                "[BattleDebugInput] GameManager가 연결되지 않았습니다."
            );
            return;
        }

        gameManager.Victory();
    }

    private void ShowDefeat()
    {
        if (gameManager == null)
        {
            Debug.LogWarning(
                "[BattleDebugInput] GameManager가 연결되지 않았습니다."
            );
            return;
        }

        gameManager.Defeat();
    }

    private void ShowWarning()
    {
        if (bossStageWarningUI == null)
        {
            Debug.LogWarning(
                "[BattleDebugInput] BossStageWarningUI가 연결되지 않았습니다."
            );
            return;
        }

        if (!bossStageWarningUI.gameObject.activeSelf)
        {
            bossStageWarningUI.gameObject.SetActive(true);
        }

        bossStageWarningUI.PlayWarning();
    }
}
