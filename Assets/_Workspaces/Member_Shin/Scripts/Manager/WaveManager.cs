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

    [Header("웨이브 대기 시간 설정")]
    public float totalCountdownTime = 8f;
    public float secondsPerGaugeStep = 1f;

    [Header("웨이브별 적 설정")]
    public WaveSetting[] waveSettings;

    [Header("테스트 설정")]
    public bool startAutomatically = true;

    // 배열에서 현재 실행할 웨이브의 위치를 저장합니다.
    // 배열 번호는 0부터 시작하지만 화면의 웨이브 숫자는 1부터 시작합니다.
    // 따라서 처음에는 -1로 두고, 웨이브 시작 시 1씩 증가시킵니다.
    private int currentWaveIndex = -1;

    // 다음 웨이브 시작까지 남은 시간을 저장합니다.
    // Update에서 Time.deltaTime만큼 감소합니다.
    // 0초가 되면 다음 웨이브를 시작합니다.
    private float remainingCountdownTime;

    // 현재 다음 웨이브를 기다리는 중인지 저장합니다.
    // 적 생성 중에는 false이고, 원형 게이지가 줄어드는 동안에는 true입니다.
    // 이렇게 해야 대기 시간과 적 생성 시간이 동시에 진행되지 않습니다.
    private bool isCountingDown;

    void Start()
    {
        // 게임이 실행되자마자 첫 번째 웨이브를 즉시 시작합니다.
        // 기존에는 StartCountdown()을 거쳐서 일정 시간을 기다린 뒤 적이 나왔습니다.
        // 지금은 별도의 대기 시간 없이 바로 1웨이브 적을 생성하도록 변경합니다.
        if (startAutomatically)
        {
            StartNextWave();
        }
    }

    void Update()
    {
        // 현재 웨이브 대기 시간이 진행 중이 아니라면
        // 아래 시간 감소 코드를 실행하지 않습니다.
        // 적 생성 중에도 원형 게이지가 임의로 감소하는 현상을 막습니다.
        if (!isCountingDown)
        {
            return;
        }

        // 실제 경과 시간만큼 남은 시간을 감소시킵니다.
        // Time.deltaTime을 사용하면 컴퓨터의 프레임 수와 관계없이
        // 실제 초 단위 기준으로 동일하게 동작합니다.
        remainingCountdownTime -=
            Time.deltaTime;

        // 원형 게이지 UI에 현재 남은 시간을 전달합니다.
        // UI는 전달받은 시간과 한 칸 감소 간격을 사용하여
        // 중앙 숫자와 단계별 이미지를 자동으로 변경합니다.
        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.SetRemainingTime(
                remainingCountdownTime,
                secondsPerGaugeStep
            );
        }

        // 남은 시간이 0초 이하가 되면
        // 카운트다운을 멈추고 다음 웨이브를 시작합니다.
        // 다음 프레임에서 여러 번 실행되지 않도록 먼저 false로 바꿉니다.
        if (remainingCountdownTime <= 0f)
        {
            isCountingDown = false;

            StartNextWave();
        }
    }

    public void StartBattleWaves()
    {
        // 외부의 START 버튼이나 전투 시작 연출에서 호출할 수 있는 메서드입니다.
        // 첫 웨이브 시작 전 카운트다운을 시작합니다.
        // 현재는 Start()에서 자동으로 호출됩니다.
        StartCountdown();
    }

    private void StartCountdown()
    {
        // 다음에 실행할 웨이브가 배열 범위를 넘어섰다면
        // 등록된 모든 웨이브의 적 생성이 끝난 상태입니다.
        // 더 이상 카운트다운을 시작하지 않고 완료 처리를 실행합니다.
        if (
            currentWaveIndex + 1 >=
            waveSettings.Length
        )
        {
            CompleteAllWaves();

            return;
        }

        // 지금부터 준비할 다음 웨이브 번호를 계산합니다.
        // currentWaveIndex는 배열 번호이므로 0부터 시작합니다.
        // 아직 첫 웨이브가 시작되지 않았을 때 값은 -1이므로 화면에는 WAVE.1이 표시됩니다.
        int nextWaveNumber =
            currentWaveIndex + 2;

        // 원형 게이지 카운트다운이 시작되는 순간
        // 곧 등장할 웨이브 번호를 화면에 먼저 표시합니다.
        // 첫 카운트다운에서는 WAVE.1, 다음 카운트다운에서는 WAVE.2가 표시됩니다.
        if (waveUI != null)
        {
            waveUI.SetWave(
                nextWaveNumber
            );
        }

        // Inspector에 설정한 전체 대기 시간으로 초기화합니다.
        // 예를 들어 Total Countdown Time이 8이라면
        // 중앙 숫자는 8부터 시작해 0까지 감소합니다.
        remainingCountdownTime =
            totalCountdownTime;

        isCountingDown = true;

        // 카운트다운이 시작된 첫 프레임부터
        // 원형 이미지와 중앙 숫자가 즉시 표시되도록 한 번 호출합니다.
        // 이 호출이 없으면 첫 프레임 동안 이전 이미지가 잠깐 남을 수 있습니다.
        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.SetRemainingTime(
                remainingCountdownTime,
                secondsPerGaugeStep
            );
        }
    }

    private void StartNextWave()
    {
        // 새로운 웨이브를 시작하므로 배열 번호를 1 증가시킵니다.
        // 처음 값은 -1이므로 첫 실행 시 0번 배열을 사용합니다.
        // 화면에는 배열 번호보다 1 큰 값을 표시합니다.
        currentWaveIndex++;

        int currentWaveNumber =
            currentWaveIndex + 1;

        // 화면의 WaveText를 WAVE.1, WAVE.2 형태로 변경합니다.
        // UI 표시만 담당하는 WaveUI에 현재 웨이브 숫자를 전달합니다.
        if (waveUI != null)
        {
            waveUI.SetWave(
                currentWaveNumber
            );
        }

        // 원형 카운트다운 UI를 GO! 상태로 변경합니다.
        // 대기 시간이 끝나고 적이 나오기 시작한다는 것을 보여줍니다.
        if (environmentGaugeUI != null)
        {
            environmentGaugeUI.ShowWaveStart();
        }

        // 현재 웨이브에 설정된 적 목록, 마릿수, 간격을
        // EnemySpawner에 전달합니다.
        // 모든 적 생성이 끝나면 OnWaveSpawnFinished()가 자동 호출됩니다.
        if (enemySpawner != null)
        {
            enemySpawner.StartWave(
                waveSettings[currentWaveIndex],
                OnWaveSpawnFinished
            );
        }
        else
        {
            Debug.LogWarning(
                "EnemySpawner가 연결되지 않았습니다."
            );
        }
    }

    private void OnWaveSpawnFinished()
    {
        // 현재 웨이브에서 설정한 적을 모두 생성한 뒤 호출됩니다.
        // 기존에는 다음 웨이브 전에 카운트다운을 다시 시작했습니다.
        // 지금은 대기 시간 없이 바로 다음 웨이브를 시작합니다.

        // 현재 웨이브가 마지막 웨이브인지 먼저 확인합니다.
        // 예를 들어 웨이브 배열이 3개라면 사용할 수 있는 인덱스는 0, 1, 2입니다.
        // 현재 인덱스가 마지막 번호라면 더 이상 StartNextWave()를 호출하지 않습니다.
        if (
            currentWaveIndex + 1 >=
            waveSettings.Length
        )
        {
            CompleteAllWaves();

            return;
        }

        // 아직 다음 웨이브가 남아 있다면 즉시 다음 웨이브를 시작합니다.
        // 별도의 카운트다운을 거치지 않으므로
        // 이전 웨이브의 마지막 적이 나온 직후 다음 웨이브 적이 이어서 나옵니다.
        StartNextWave();
    }

    private void CompleteAllWaves()
    {
        // 모든 웨이브의 적 생성이 끝났습니다.
        // 현재는 UI 문구만 변경합니다.
        // 추후 BattleManager를 추가하면 실제 승리 조건과 연결할 수 있습니다.
        isCountingDown = false;

        if (waveUI != null)
        {
            waveUI.ShowComplete();
        }

        if (environmentGaugeUI != null)
        {
            environmentGaugeUI
                .ShowAllWavesComplete();
        }

        Debug.Log(
            "모든 웨이브의 적 생성이 완료되었습니다."
        );
    }
}