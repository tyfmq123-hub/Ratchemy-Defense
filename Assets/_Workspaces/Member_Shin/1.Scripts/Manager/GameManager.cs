using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("결과 UI")]
    [SerializeField] private ComicCutsceneUI comicCutsceneUI;
    [SerializeField] private VictoryResultUI victoryResultUI;
    [SerializeField] private DefeatResultUI defeatResultUI;

    // 승리 또는 패배 화면이 중복 실행되는 것을 막습니다.
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
        // BossBase가 사망 이벤트를 발생시키면
        // GameManager의 Victory()가 자동으로 실행됩니다.
        BossBase.OnBossDead += Victory;
    }

    private void OnDisable()
    {
        // GameManager가 비활성화되거나 씬이 종료될 때
        // 기존 연결을 제거하여 중복 호출을 방지합니다.
        BossBase.OnBossDead -= Victory;
    }

    /// <summary>
    /// 보스가 사망했을 때 실행됩니다.
    /// 승리 만화 컷을 보여준 뒤 승리 결과 화면으로 넘어갑니다.
    /// </summary>
    public void Victory()
    {
        if (isGameEnd)
            return;

        if (comicCutsceneUI == null)
        {
            Debug.LogError("[GameManager] ComicCutsceneUI가 연결되지 않았습니다.");
            return;
        }

        isGameEnd = true;
        comicCutsceneUI.ShowVictory(ShowVictoryResult);
    }

    /// <summary>
    /// 배터리 온도가 최대치에 도달했을 때 호출합니다.
    /// 패배 만화 컷을 보여준 뒤 패배 결과 화면으로 넘어갑니다.
    /// </summary>
    public void Defeat()
    {
        if (isGameEnd)
            return;

        if (comicCutsceneUI == null)
        {
            Debug.LogError("[GameManager] ComicCutsceneUI가 연결되지 않았습니다.");
            return;
        }

        isGameEnd = true;
        comicCutsceneUI.ShowDefeat(ShowDefeatResult);
    }

    private void ShowVictoryResult()
    {
        if (victoryResultUI == null)
        {
            Debug.LogError("[GameManager] VictoryResultUI가 연결되지 않았습니다.");
            Time.timeScale = 1f;
            return;
        }

        victoryResultUI.Show();
    }

    private void ShowDefeatResult()
    {
        if (defeatResultUI == null)
        {
            Debug.LogError("[GameManager] DefeatResultUI가 연결되지 않았습니다.");
            Time.timeScale = 1f;
            return;
        }

        defeatResultUI.Show();
    }
}