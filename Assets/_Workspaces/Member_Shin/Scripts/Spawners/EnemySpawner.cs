using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("적 생성 위치")]
    public Transform[] enemySpawnPoints;

    // 현재 실행 중인 적 생성 코루틴을 저장합니다.
    // 코루틴은 일정 시간마다 반복 작업을 처리할 수 있습니다.
    // 웨이브가 중복 실행되는 것을 막기 위해 현재 상태를 기억합니다.
    private Coroutine spawnCoroutine;

    public void StartWave(
        WaveSetting waveSetting,
        Action onWaveSpawnFinished
    )
    {
        // 이전 웨이브 생성 작업이 아직 남아 있다면 중단합니다.
        // 정상 흐름에서는 겹치지 않지만,
        // 테스트 중 버튼을 여러 번 누르거나 외부에서 중복 호출될 가능성을 막습니다.
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // 전달받은 현재 웨이브 설정을 기준으로
        // 적을 일정 간격마다 생성하는 코루틴을 시작합니다.
        // 모든 적 생성이 끝나면 WaveManager가 전달한 메서드를 다시 호출합니다.
        spawnCoroutine = StartCoroutine(
            SpawnWaveCoroutine(
                waveSetting,
                onWaveSpawnFinished
            )
        );
    }

    private IEnumerator SpawnWaveCoroutine(
        WaveSetting waveSetting,
        Action onWaveSpawnFinished
    )
    {
        // 현재 웨이브에 적 프리팹이 하나도 등록되지 않았다면
        // 적을 생성할 수 없으므로 경고 메시지를 출력합니다.
        // 이후 다음 웨이브로 넘어갈 수 있도록 완료 신호는 전달합니다.
        if (
            waveSetting.enemyPrefabs == null ||
            waveSetting.enemyPrefabs.Length == 0
        )
        {
            Debug.LogWarning(
                "현재 웨이브에 등록된 Enemy Prefab이 없습니다."
            );

            onWaveSpawnFinished?.Invoke();

            yield break;
        }

        // 적 생성 위치가 등록되지 않았다면
        // Instantiate를 실행할 좌표를 알 수 없습니다.
        // Inspector에서 Enemy Spawn Points를 연결했는지 확인해야 합니다.
        if (
            enemySpawnPoints == null ||
            enemySpawnPoints.Length == 0
        )
        {
            Debug.LogWarning(
                "Enemy Spawn Points가 비어 있습니다."
            );

            onWaveSpawnFinished?.Invoke();

            yield break;
        }

        // 현재 웨이브에 설정된 마릿수만큼 반복합니다.
        // 매 반복마다 현재 웨이브에서 허용된 적 중 하나를 랜덤으로 선택합니다.
        // 따라서 같은 웨이브 안에서도 서로 다른 적이 섞여서 등장할 수 있습니다.
        for (
            int i = 0;
            i < waveSetting.spawnCount;
            i++
        )
        {
            SpawnRandomEnemy(
                waveSetting.enemyPrefabs
            );

            // 마지막 적을 생성한 뒤에는 추가 대기 시간이 필요하지 않습니다.
            // 마지막 적이 아니라면 Inspector에 설정한 간격만큼 기다립니다.
            // 예: Spawn Interval이 2라면 적이 2초 간격으로 한 마리씩 등장합니다.
            if (
                i < waveSetting.spawnCount - 1
            )
            {
                yield return new WaitForSeconds(
                    waveSetting.spawnInterval
                );
            }
        }

        // 현재 웨이브에 등록된 적 생성 작업이 모두 끝났습니다.
        // spawnCoroutine 값을 비워서 더 이상 실행 중인 작업이 없음을 표시합니다.
        // 이후 WaveManager에 완료 사실을 전달합니다.
        spawnCoroutine = null;

        onWaveSpawnFinished?.Invoke();
    }

    private void SpawnRandomEnemy(
        GameObject[] availableEnemyPrefabs
    )
    {
        // 현재 웨이브에서 허용된 적 프리팹 중 하나를 무작위로 고릅니다.
        // 모든 적 프리팹 중에서 고르는 것이 아니라,
        // WaveManager Inspector에서 현재 웨이브에 넣은 적만 후보가 됩니다.
        int enemyPrefabIndex =
            UnityEngine.Random.Range(
                0,
                availableEnemyPrefabs.Length
            );

        // 적 생성 위치도 배열에서 무작위로 선택합니다.
        // 단일 라인 게임이라면 Spawn Point를 하나만 연결하면 됩니다.
        // 여러 라인을 사용하면 등록된 위치 중 하나가 랜덤으로 선택됩니다.
        int spawnPointIndex =
            UnityEngine.Random.Range(
                0,
                enemySpawnPoints.Length
            );

        GameObject selectedEnemyPrefab =
            availableEnemyPrefabs[
                enemyPrefabIndex
            ];

        Transform selectedSpawnPoint =
            enemySpawnPoints[
                spawnPointIndex
            ];

        // 실제 적 유닛을 생성합니다.
        // 선택한 적 프리팹을 선택한 Spawn Point 위치와 회전값으로 배치합니다.
        // 적 이동 스크립트가 프리팹에 붙어 있다면 생성 직후 자동으로 이동을 시작합니다.
        Instantiate(
            selectedEnemyPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );
    }
}