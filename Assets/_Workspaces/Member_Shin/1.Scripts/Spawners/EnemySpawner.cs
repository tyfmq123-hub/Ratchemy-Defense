using System;              
using System.Collections; 
using UnityEngine;        

public class EnemySpawner : MonoBehaviour
{
    [Header("적 생성 위치")]
    public Transform[] enemySpawnPoints;

    public void StartWave(
        WaveSetting waveSetting,
        float firstSpawnDelay,
        Action onEnemySpawned,
        Action onEnemyRemoved,
        Action onWaveSpawnFinished
    )
    {
        StartCoroutine(
            SpawnWaveCoroutine(
                waveSetting,
                firstSpawnDelay,
                onEnemySpawned,
                onEnemyRemoved,
                onWaveSpawnFinished
            )
        );
    }

    private IEnumerator SpawnWaveCoroutine(
        WaveSetting waveSetting,
        float firstSpawnDelay,
        Action onEnemySpawned,
        Action onEnemyRemoved,
        Action onWaveSpawnFinished
    )
    {
        if (firstSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                firstSpawnDelay
            );
        }
        if (
            waveSetting.enemyPrefabs == null ||
            waveSetting.enemyPrefabs.Length == 0
        )
        {
            onWaveSpawnFinished?.Invoke();

            yield break;
        }
        if (
            enemySpawnPoints == null ||
            enemySpawnPoints.Length == 0
        )
        {
            onWaveSpawnFinished?.Invoke();

            yield break;
        }
        for (
            int i = 0;
            i < waveSetting.spawnCount;
            i++
        )
        {
            GameObject spawnedEnemy =
                waveSetting.spawnInOrder
                    ? SpawnEnemyInOrder(
                        waveSetting.enemyPrefabs,
                        i
                    )
                    : SpawnRandomEnemy(
                        waveSetting.enemyPrefabs
                    );
            EnemyWaveTracker tracker =
                spawnedEnemy.GetComponent<
                    EnemyWaveTracker
                >();

            if (tracker == null)
            {
                tracker =
                    spawnedEnemy.AddComponent<
                        EnemyWaveTracker
                    >();
            }
            tracker.Initialize(
                onEnemyRemoved
            );
            onEnemySpawned?.Invoke();
            if (
                i < waveSetting.spawnCount - 1
            )
            {
                yield return new WaitForSeconds(
                    waveSetting.spawnInterval
                );
            }
        }
        onWaveSpawnFinished?.Invoke();
    }

    private GameObject SpawnEnemyInOrder(
        GameObject[] availableEnemyPrefabs,
        int spawnIndex
    )
    {
        int enemyPrefabIndex =
            Mathf.Clamp(
                spawnIndex,
                0,
                availableEnemyPrefabs.Length - 1
            );

        return SpawnEnemyPrefab(
            availableEnemyPrefabs[enemyPrefabIndex]
        );
    }

    private GameObject SpawnRandomEnemy(
        GameObject[] availableEnemyPrefabs
    )
    {
        int enemyPrefabIndex =
            UnityEngine.Random.Range(
                0,
                availableEnemyPrefabs.Length
            );

        return SpawnEnemyPrefab(
            availableEnemyPrefabs[enemyPrefabIndex]
        );
    }

    private GameObject SpawnEnemyPrefab(
        GameObject selectedEnemyPrefab
    )
    {
        int spawnPointIndex =
            UnityEngine.Random.Range(
                0,
                enemySpawnPoints.Length
            );

        Transform selectedSpawnPoint =
            enemySpawnPoints[
                spawnPointIndex
            ];

        return Instantiate(
            selectedEnemyPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );
    }
}