using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("결과 UI")]
    [SerializeField] private ComicCutsceneUI comicCutsceneUI;
    [SerializeField] private VictoryResultUI victoryResultUI;
    [SerializeField] private DefeatResultUI defeatResultUI;

    private bool isGameEnd = false;

    private void Awake()
    {
        if (comicCutsceneUI == null)
            comicCutsceneUI = FindFirstObjectByType<ComicCutsceneUI>(FindObjectsInactive.Include);

        if (victoryResultUI == null)
            victoryResultUI = FindFirstObjectByType<VictoryResultUI>(FindObjectsInactive.Include);

        if (defeatResultUI == null)
            defeatResultUI = FindFirstObjectByType<DefeatResultUI>(FindObjectsInactive.Include);
    }

    public void Victory()
    {
        if (isGameEnd) return;
        if (comicCutsceneUI == null)
        {
            Debug.LogError("[GameManager] ComicCutsceneUI가 연결되지 않았습니다.");
            return;
        }

        comicCutsceneUI.ShowVictory(ShowVictoryResult);
        isGameEnd = true;
    }

    public void Defeat()
    {
        if (isGameEnd) return;
        if (comicCutsceneUI == null)
        {
            Debug.LogError("[GameManager] ComicCutsceneUI가 연결되지 않았습니다.");
            return;
        }

        comicCutsceneUI.ShowDefeat(ShowDefeatResult);
        isGameEnd = true;
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

    // 테스트용 (V: 승리, D: 패배)
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.vKey.wasPressedThisFrame)
            Victory();

        if (Keyboard.current.dKey.wasPressedThisFrame)
            Defeat();
    }
}
