using UnityEngine;
using System.Collections;
using TMPro;

[System.Serializable]
public class WaveSetting
{
    [Header("현재 웨이브에 등장 가능한 적")]
    public GameObject[] enemyPrefabs;

    [Header("현재 웨이브에서 생성할 적 마릿수")]
    public int spawnCount = 5;

    [Header("적 한 마리 사이의 생성 간격")]
    public float spawnInterval = 2f;

    [Header("이 웨이브 제한 시간 (0이면 전역 totalCountdownTime 사용)")]
    public float waveDuration = 0f;

    [Header("enemyPrefabs 배열 순서대로 생성")]
    public bool spawnInOrder = false;
}

public class WaveManager : MonoBehaviour
{
    [Header("연결할 UI")]
    [Tooltip("화면 상단 중앙의 BattleTimerText를 연결하세요.")]
    public TextMeshProUGUI battleTimerText;

    [Tooltip("화면 중앙에 WAVE 1, WAVE 2 문구를 잠깐 표시하는 UI입니다.")]
    public WaveAnnouncementUI waveAnnouncementUI;

    public BossStageWarningUI bossStageWarningUI;


    [Header("연결할 적 생성 스크립트")]
    public EnemySpawner enemySpawner;

    [Header("웨이브 시간 설정")]
    public float totalCountdownTime = 20f;

    [Header("첫 번째 적 등장 지연 시간")]
    public float firstWaveSpawnDelay = 2f;

    [Header("웨이브별 적 설정")]
    public WaveSetting[] waveSettings;

    // 현재 진행 중인 웨이브의 배열 번호입니다.
    // 배열은 0번부터 시작하므로 게임 시작 전에는 -1로 둡니다.
    private int currentWaveIndex = -1;

    public int CurrentWaveIndex => currentWaveIndex;

    // 현재 웨이브에 남은 시간입니다.
    private float remainingCountdownTime;

    // 카운트다운이 진행 중인지 확인합니다.
    private bool isCountingDown;

    // 웨이브가 중복으로 시작되지 않도록 막습니다.
    private bool hasBattleStarted;

    // 마지막 일반 웨이브에서 살아 있는 적의 수입니다.
    private int aliveFinalWaveEnemyCount;

    // 마지막 일반 웨이브의 적 생성이 모두 끝났는지 확인합니다.
    private bool hasFinishedFinalWaveSpawning;

    // 마지막 일반 웨이브의 제한 시간이 끝났는지 확인합니다.
    private bool hasFinishedFinalWaveCountdown;

    // 보스 스테이지가 중복으로 시작되지 않도록 막습니다.
    private bool hasEnteredBossStage;

    // 씬 전환 또는 오브젝트 정리 중에는
    // EnemyWaveTracker의 뒤늦은 제거 알림을 무시합니다.
    private bool isShuttingDown;

    void Start()
    {
        // 첫 번째 웨이브는 바로 시작하지 않습니다.
        // BattleIntroCamera의 카메라 왕복 연출이 끝난 뒤 시작합니다.
    }

    private void OnDestroy()
    {
        // 씬이 닫히는 동안 적 오브젝트도 연달아 Destroy될 수 있습니다.
        // 이때 EnemyWaveTracker가 제거 알림을 보내더라도
        // 삭제 중인 WaveManager가 새 코루틴을 시작하지 않도록 막습니다.
        isShuttingDown = true;

        StopAllCoroutines();
    }

    void Update()
    {
        if (isShuttingDown)
        {
            return;
        }

        // 카운트다운 중이 아니라면 아래 코드를 실행하지 않습니다.
        if (!isCountingDown)
        {
            return;
        }

        // 프레임이 지날 때마다 남은 시간을 감소시킵니다.
        remainingCountdownTime -= Time.deltaTime;

        UpdateBattleTimerText();

        // 아직 시간이 남아 있다면 다음 프레임까지 기다립니다.
        if (remainingCountdownTime > 0f)
        {
            return;
        }

        // 시간이 끝났을 때 음수가 남지 않도록 0으로 맞춥니다.
        remainingCountdownTime = 0f;
        isCountingDown = false;

        // 현재 웨이브가 마지막 일반 웨이브라면
        // 다음 웨이브를 시작하지 않고 보스 진입 조건을 확인합니다.
        if (IsFinalWave(currentWaveIndex))
        {
            hasFinishedFinalWaveCountdown = true;

            TryEnterBossStage();

            return;
        }

        // 마지막 웨이브가 아니라면 다음 웨이브를 시작합니다.
        StartWave(
            currentWaveIndex + 1
        );
    }

    public void StartBattleWaves()
    {
        // 이미 전투가 시작된 상태라면 중복 실행하지 않습니다.
        if (hasBattleStarted)
        {
            return;
        }

        // Inspector에 웨이브 설정이 없다면 실행하지 않습니다.
        if (
            waveSettings == null ||
            waveSettings.Length == 0
        )
        {
            return;
        }

        hasBattleStarted = true;

        // 배열의 첫 번째 웨이브인 0번부터 시작합니다.
        StartWave(0);
    }

    /// <summary>
    /// 현재 웨이브의 제한 시간만 다시 시작합니다.
    /// BalanceBattleSimulator 등 외부에서 웨이브 시스템 복구 후 타이머를 재설정할 때 사용합니다.
    /// 웨이브 번호, 적 스폰, 보스 진입 조건은 변경하지 않습니다.
    /// </summary>
    public void RestartWaveTimer()
    {
        // 전투가 시작되지 않았거나 유효한 웨이브가 없으면 실행하지 않습니다.
        if (!hasBattleStarted || currentWaveIndex < 0)
        {
            return;
        }

        // 이미 보스 스테이지에 진입한 뒤에는 타이머를 되돌리지 않습니다.
        if (hasEnteredBossStage)
        {
            return;
        }

        remainingCountdownTime = totalCountdownTime;
        isCountingDown = true;

        UpdateBattleTimerText();
    }

    private void StartWave(int waveIndex)
    {
        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            return;
        }

        // 존재하지 않는 배열 번호가 들어오면 실행하지 않습니다.
        if (
            waveIndex < 0 ||
            waveIndex >= waveSettings.Length
        )
        {
            return;
        }

        currentWaveIndex = waveIndex;

        // 새로운 웨이브가 시작될 때만
        // 화면 중앙에 WAVE 1, WAVE 2 같은 문구를 잠깐 표시합니다.
        if (waveAnnouncementUI != null)
        {
            waveAnnouncementUI.ShowWaveAnnouncement(
                currentWaveIndex + 1
            );
        }

        // 새로운 웨이브가 시작될 때 제한 시간을 초기화합니다.
        float waveDuration =
            waveSettings[waveIndex].waveDuration;

        remainingCountdownTime =
            waveDuration > 0f
                ? waveDuration
                : totalCountdownTime;

        isCountingDown = true;

        UpdateBattleTimerText();

        // 첫 번째 웨이브에만 약간의 등장 지연 시간을 적용합니다.
        float spawnDelay =
            currentWaveIndex == 0
                ? firstWaveSpawnDelay
                : 0f;

        // 콜백이 실행될 때 현재 웨이브 번호가 바뀌어도
        // 적이 생성된 당시의 웨이브 번호를 유지합니다.
        int spawnedWaveIndex =
            currentWaveIndex;

        if (enemySpawner != null)
        {
            enemySpawner.StartWave(
                waveSettings[
                    spawnedWaveIndex
                ],
                spawnDelay,
                () => OnEnemySpawned(
                    spawnedWaveIndex
                ),
                () => OnEnemyRemoved(
                    spawnedWaveIndex
                ),
                () => OnWaveSpawnFinished(
                    spawnedWaveIndex
                )
            );
        }
    }

    private void OnEnemySpawned(int spawnedWaveIndex)
    {
        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            return;
        }

        // 마지막 일반 웨이브의 적만 별도로 개수를 셉니다.
        if (!IsFinalWave(spawnedWaveIndex))
        {
            return;
        }

        aliveFinalWaveEnemyCount++;
    }

    public void OnEnemyRemoved(int removedWaveIndex)
    {
        // 씬 전환 중에는 적 오브젝트가 한꺼번에 Destroy됩니다.
        // 삭제된 WaveManager에서 StartCoroutine()을 호출하면
        // MissingReferenceException이 발생하므로 즉시 종료합니다.
        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            return;
        }

        // 마지막 일반 웨이브가 아니라면
        // 보스 진입 조건과 관계가 없으므로 무시합니다.
        if (!IsFinalWave(removedWaveIndex))
        {
            return;
        }

        aliveFinalWaveEnemyCount =
            Mathf.Max(
                0,
                aliveFinalWaveEnemyCount - 1
            );

        StartCoroutine(
            TryEnterBossStageNextFrame()
        );
    }

    private void OnWaveSpawnFinished(
        int finishedWaveIndex
    )
    {
        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            return;
        }

        // 마지막 일반 웨이브의 생성 완료만 확인합니다.
        if (!IsFinalWave(finishedWaveIndex))
        {
            return;
        }

        hasFinishedFinalWaveSpawning = true;

        TryEnterBossStage();
    }

    private void TryEnterBossStage()
    {
        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            return;
        }

        // 이미 보스 스테이지에 진입했다면 중복 실행하지 않습니다.
        if (hasEnteredBossStage)
        {
            return;
        }

        // 마지막 웨이브의 시간이 끝나야 합니다.
        if (!hasFinishedFinalWaveCountdown)
        {
            return;
        }

        // 마지막 웨이브의 적 생성도 모두 끝나야 합니다.
        if (!hasFinishedFinalWaveSpawning)
        {
            return;
        }

        // 화면에 살아 있는 일반 웨이브 적이 한 마리라도 남아 있다면 기다립니다.
        // 단순 누적 카운터 대신 실제 씬에 남아 있는 적을 직접 확인합니다.
        int remainingWaveEnemyCount =
            CountRemainingWaveEnemies();

        if (remainingWaveEnemyCount > 0)
        {
            return;
        }

        hasEnteredBossStage = true;

        // 새로운 웨이브가 시작될 때만
        // 화면 중앙에 WAVE 1, WAVE 2 같은 문구를 잠깐 표시합니다.
        if (waveAnnouncementUI != null)
        {
            waveAnnouncementUI.ShowWaveAnnouncement(
                currentWaveIndex + 1
            );
        }

        UpdateBattleTimerText();
        // Boss Stage 문구와 같은 시점에 Warning 이미지 깜빡임 연출을 재생합니다.
        // Inspector 연결이 없어도 기존 게임 진행은 계속됩니다.
        bossStageWarningUI?.PlayWarning();
    }

    // 적이 Destroy된 직후에는 씬에서 완전히 제거되기 전일 수 있습니다.
    // 한 프레임 기다린 뒤 실제로 남아 있는 일반 적 수를 다시 확인합니다.
    private IEnumerator TryEnterBossStageNextFrame()
    {
        yield return null;

        if (isShuttingDown || this == null || !isActiveAndEnabled)
        {
            yield break;
        }

        TryEnterBossStage();
    }


    // EnemySpawner가 생성한 일반 적에게는 EnemyWaveTracker가 붙습니다.
    // 보스는 처음부터 씬에 배치되어 있으므로 이 개수에 포함되지 않습니다.
    private int CountRemainingWaveEnemies()
    {
        EnemyWaveTracker[] remainingEnemies =
            FindObjectsByType<EnemyWaveTracker>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        return remainingEnemies.Length;
    }

    private bool IsFinalWave(int waveIndex)
    {
        return
            waveSettings != null &&
            waveSettings.Length > 0 &&
            waveIndex ==
                waveSettings.Length - 1;
    }

    /// <summary>
    /// 화면 상단 중앙에 남은 시간을 01:25 형식으로 표시합니다.
    /// </summary>
    private void UpdateBattleTimerText()
    {
        if (battleTimerText == null)
        {
            return;
        }

        // 화면에 음수가 표시되지 않게 막습니다.
        float safeRemainingTime =
            Mathf.Max(
                0f,
                remainingCountdownTime
            );

        // 5.2초가 남으면 06초로 표시합니다.
        // 타이머가 너무 일찍 00:00으로 보이는 것을 막기 위한 처리입니다.
        int totalSeconds =
            Mathf.CeilToInt(
                safeRemainingTime
            );

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        battleTimerText.text =
        $"WAVE TIME\n{minutes:00}:{seconds:00}";
    }
}