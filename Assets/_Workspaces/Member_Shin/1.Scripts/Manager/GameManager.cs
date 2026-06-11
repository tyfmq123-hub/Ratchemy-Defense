using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("결과 UI")]
    [SerializeField] private ComicCutsceneUI comicCutsceneUI;
    [SerializeField] private VictoryResultUI victoryResultUI;
    [SerializeField] private DefeatResultUI defeatResultUI;

    // 승리 또는 패배 처리가 중복 실행되지 않도록 막습니다.
    private bool isGameEnd = false;

    private void Awake()
    {
        // Inspector 연결을 빠뜨렸을 때를 대비해
        // 비활성화된 결과 UI까지 포함하여 자동으로 찾습니다.
        if (comicCutsceneUI == null)
        {
            comicCutsceneUI =
                FindFirstObjectByType<ComicCutsceneUI>(
                    FindObjectsInactive.Include
                );
        }

        if (victoryResultUI == null)
        {
            victoryResultUI =
                FindFirstObjectByType<VictoryResultUI>(
                    FindObjectsInactive.Include
                );
        }

        if (defeatResultUI == null)
        {
            defeatResultUI =
                FindFirstObjectByType<DefeatResultUI>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void OnEnable()
    {
        // BossBase에서 보스 사망 이벤트가 발생하면
        // Victory()를 자동으로 실행합니다.
        BossBase.OnBossDead += Victory;
    }

    private void OnDisable()
    {
        // 씬 종료 또는 오브젝트 비활성화 시 구독을 해제합니다.
        // 구독을 해제하지 않으면 중복 호출 문제가 생길 수 있습니다.
        BossBase.OnBossDead -= Victory;
    }

    /// <summary>
    /// 보스가 죽었을 때 실행됩니다.
    /// 승리 만화 컷을 보여준 뒤 승리 결과 UI를 엽니다.
    /// </summary>
    public void Victory()
    {
        if (isGameEnd)
            return;

        if (comicCutsceneUI == null)
        {
            Debug.LogError(
                "[GameManager] ComicCutsceneUI가 연결되지 않았습니다."
            );

            return;
        }

        isGameEnd = true;
        comicCutsceneUI.ShowVictory(ShowVictoryResult);
    }

    /// <summary>
    /// 배터리 온도가 열폭주 기준에 도달했을 때 실행됩니다.
    /// 패배 만화 컷을 보여준 뒤 패배 결과 UI를 엽니다.
    /// </summary>
    public void Defeat()
    {
        if (isGameEnd)
            return;

        if (comicCutsceneUI == null)
        {
            Debug.LogError(
                "[GameManager] ComicCutsceneUI가 연결되지 않았습니다."
            );

            return;
        }

        isGameEnd = true;
        comicCutsceneUI.ShowDefeat(ShowDefeatResult);
    }

    private void ShowVictoryResult()
    {
        if (victoryResultUI == null)
        {
            Debug.LogError(
                "[GameManager] VictoryResultUI가 연결되지 않았습니다."
            );

            Time.timeScale = 1f;
            return;
        }

        victoryResultUI.Show();
    }

    private void ShowDefeatResult()
    {
        if (defeatResultUI == null)
        {
            Debug.LogError(
                "[GameManager] DefeatResultUI가 연결되지 않았습니다."
            );

            Time.timeScale = 1f;
            return;
        }

        defeatResultUI.Show();
    }
}