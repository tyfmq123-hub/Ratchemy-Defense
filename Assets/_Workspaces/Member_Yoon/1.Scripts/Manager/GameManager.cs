using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        BossBase.OnBossDead += OnGameClear;
    }

    private void OnDisable()
    {
        BossBase.OnBossDead -= OnGameClear;
    }

    private bool isPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void OnGameClear()
    {
        Debug.Log("[GameManager] 게임 클리어!");
    }
}
