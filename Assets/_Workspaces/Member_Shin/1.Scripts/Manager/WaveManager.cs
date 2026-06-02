using UnityEngine;

[System.Serializable]
public class WaveSetting
{
    [Header("현재 웨이브에 등장 가능한 적")]
    public GameObject[] enemyPrefabs;

    [Header("현재 웨이브에서 생성할 적 마릿수")]
    public int spawnCount = 5;

    [Header("적 한 마리 사이의 생성 간격")]
    public float spawnInterval = 2f;
}

public class WaveManager : MonoBehaviour
{
    [Header("연결할 UI")]
    public EnvironmentGaugeUI environmentGaugeUI;
    public WaveUI waveUI;

    [Header("연결할 적 생성 스크립트")]
    public EnemySpawner enemySpawner;

    [Header("웨이브 시간 설정")]
    public float totalCountdownTime = 20f;
    public float secondsPerGaugeStep = 1f;

    [Header("첫 번째 적 등장 지연 시간")]
    public float firstWaveSpawnDelay = 2f;

    [Header("웨이브별 적 설정")]
    public WaveSetting[] waveSettings;

    [Header("테스트 설정")]
    public bool startAutomatically = true;

    private int currentWaveIndex = -1;

    private float remainingCountdownTime;

    private bool isCountingDown;

    private bool hasBattleStarted;

    private int aliveFinalWaveEnemyCount;

    private bool hasFinishedFinalWaveSpawning;

    private bool hasFinishedFinalWaveCountdown;

    private bool hasEnteredBossStage;

    void Start()
    {
        if (startAutomatically)
        {
            StartBattleWaves();
        }
    }

    void Update()
    {
        if (!isCountingDown)
        {
            return;
        }
        remainingCountdownTime -= Time.deltaTime;

        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.SetRemainingTime(
                remainingCountdownTime,
                secondsPerGaugeStep
            );
        }
        if (remainingCountdownTime > 0f)
        {
            return;
        }
        remainingCountdownTime = 0f;
        isCountingDown = false;
        if (IsFinalWave(currentWaveIndex))
        {
            hasFinishedFinalWaveCountdown = true;

            TryEnterBossStage();

            return;
        }
        StartWave(
            currentWaveIndex + 1
        );
    }

    public void StartBattleWaves()
    {
        if (hasBattleStarted)
        {
            return;
        }

        if (
            waveSettings == null ||
            waveSettings.Length == 0
        )
        {
            Debug.LogWarning(
                "Wave Settings가 비어 있습니다."
            );

            return;
        }

        hasBattleStarted = true;

        StartWave(0);
    }

    private void StartWave(int waveIndex)
    {
        if (
            waveIndex < 0 ||
            waveIndex >= waveSettings.Length
        )
        {
            return;
        }

        currentWaveIndex = waveIndex;
        if (waveUI != null)
        {
            waveUI.SetWave(
                currentWaveIndex + 1
            );
        }
        remainingCountdownTime =
            totalCountdownTime;

        isCountingDown = true;

        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.SetRemainingTime(
                remainingCountdownTime,
                secondsPerGaugeStep
            );
        }

        float spawnDelay =
            currentWaveIndex == 0
                ? firstWaveSpawnDelay
                : 0f;

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
        else
        {
            Debug.LogWarning(
                "EnemySpawner가 연결되지 않았습니다."
            );
        }
    }

    private void OnEnemySpawned(int spawnedWaveIndex)
    {
        if (!IsFinalWave(spawnedWaveIndex))
        {
            return;
        }

        aliveFinalWaveEnemyCount++;
    }

    public void OnEnemyRemoved(int removedWaveIndex)
    {
        if (!IsFinalWave(removedWaveIndex))
        {
            return;
        }

        aliveFinalWaveEnemyCount =
            Mathf.Max(
                0,
                aliveFinalWaveEnemyCount - 1
            );
        TryEnterBossStage();
    }

    private void OnWaveSpawnFinished(
        int finishedWaveIndex
    )
    {
        if (!IsFinalWave(finishedWaveIndex))
        {
            return;
        }

        hasFinishedFinalWaveSpawning = true;

        TryEnterBossStage();
    }

    private void TryEnterBossStage()
    {
        if (hasEnteredBossStage)
        {
            return;
        }

        if (!hasFinishedFinalWaveCountdown)
        {
            return;
        }

        if (!hasFinishedFinalWaveSpawning)
        {
            return;
        }

        if (aliveFinalWaveEnemyCount > 0)
        {
            return;
        }

        hasEnteredBossStage = true;

        if (waveUI != null)
        {
            waveUI.ShowBossStage();
        }
        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.SetRemainingTime(
                0f,
                secondsPerGaugeStep
            );
        }

        Debug.Log(
            "마지막 일반 웨이브 종료: BOSS STAGE 진입"
        );
    }

    private bool IsFinalWave(int waveIndex)
    {
        return
            waveSettings != null &&
            waveSettings.Length > 0 &&
            waveIndex ==
                waveSettings.Length - 1;
    }
}