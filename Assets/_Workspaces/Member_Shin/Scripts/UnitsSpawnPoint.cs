using UnityEngine;

public class UnitsSpawnPoint : MonoBehaviour
{
    [Header("Player")]
    public GameObject[] playerPrefabs;
    public Transform[] playerSpawnPoints;

    [Header("Enemy")] 
    public GameObject[] enemyPrefabs;
    public Transform[] enemySpawnPoints;

    [Header("Enemy Spawn Settings")]
    public float enemySpawnInterval = 2f;
    
    void Start()
    {
        SpawnAllPlayers();

        InvokeRepeating(
            nameof(SpawnRandomEnemy),
            1f,
            enemySpawnInterval
        );
    }

    void SpawnAllPlayers()
    {
        if (playerPrefabs == null || playerPrefabs.Length ==0)
        {
            Debug.LogWarning("Player Prefabs가 비어 있습니다.");
            return;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length ==0)
        {
            Debug.LogWarning("Player Spawn Points가 비어 있습니다.");
            return;
        }

        int spawnCount = Mathf.Min(playerPrefabs.Length, playerSpawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            Instantiate(playerPrefabs[i], playerSpawnPoints[i].position, playerSpawnPoints[i].rotation);
        }
    }

    void SpawnRandomEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Enemy Prefabs가 비어 있습니다.");
            return;
        }

        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("Enemy Spawn Points가 비어 있습니다.");
            return;
        }

        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        int spawnIndex = Random.Range(0, enemySpawnPoints.Length);
        GameObject enemyPrefab = enemyPrefabs[enemyIndex];
        Transform spawnPoint = enemySpawnPoints[spawnIndex];

        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

    }
}
