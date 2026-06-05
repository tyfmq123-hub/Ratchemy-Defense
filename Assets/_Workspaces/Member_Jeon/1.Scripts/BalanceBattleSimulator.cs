using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class BalanceBattleSimulator : MonoBehaviour
{
    public enum SimulationMode
    {
        Mixed,
        OneVsOne,
        OneVsOneMatrix
    }

    [Header("Simulation")]
    [SerializeField] private SimulationMode simulationMode = SimulationMode.Mixed;
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private int rounds = 10;
    [SerializeField] private float roundDuration = 25f;
    [SerializeField] private float roundInterval = 1f;

    [Header("Spawn")]
    [SerializeField] private PlayerUnitBase[] playerPrefabs;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private int playersPerRound = 4;
    [SerializeField] private int enemiesPerRound = 8;

    [Header("1vs1")]
    [SerializeField] private PlayerUnitBase oneVsOnePlayerPrefab;
    [SerializeField] private GameObject oneVsOneEnemyPrefab;

    [Header("Option")]
    [SerializeField] private bool randomPickPrefab = true;
    [SerializeField] private bool clearOldUnitsBeforeRound = true;
    [SerializeField] private bool pauseWaveSystemDuringSimulation = true;
    [SerializeField] private bool exportReportToFile = true;
    [SerializeField] private string reportFileName = "BalanceReport.txt";

    private readonly List<BalanceUnitTracker> activeRoundTrackers = new List<BalanceUnitTracker>();
    private readonly List<GameObject> simulatorSpawnedUnits = new List<GameObject>();
    private readonly Dictionary<string, SideStats> playerStatsByPrefab = new Dictionary<string, SideStats>();
    private readonly Dictionary<string, SideStats> enemyStatsByPrefab = new Dictionary<string, SideStats>();
    private readonly Dictionary<string, MatchupStats> matchupStats = new Dictionary<string, MatchupStats>();

    private WaveManager pausedWaveManager;
    private EnemySpawner pausedEnemySpawner;
    private bool waveManagerWasEnabled;
    private bool enemySpawnerWasEnabled;

    private int totalPlayerWins;
    private int totalEnemyWins;
    private int totalDraws;
    private bool isRunning;
    private StringBuilder reportBuilder;

    private void Start()
    {
        if (runOnStart)
            StartSimulation();
    }

    [ContextMenu("Start Simulation")]
    public void StartSimulation()
    {
        if (isRunning)
        {
            Debug.LogWarning("[BalanceBattleSimulator] 이미 시뮬레이션 실행 중입니다.");
            return;
        }

        if (!ValidateConfig())
            return;

        StartCoroutine(RunSimulationFlow());
    }

    [ContextMenu("Stop Simulation")]
    public void StopSimulation()
    {
        if (!isRunning)
            return;

        StopAllCoroutines();
        ClearSimulatorUnitsOnly();
        ResumeWaveSystem();
        isRunning = false;
        Debug.Log("[BalanceBattleSimulator] 수동 중지됨.");
    }

    private IEnumerator RunSimulationFlow()
    {
        isRunning = true;
        ResetAllStats();
        PauseWaveSystem();

        reportBuilder = new StringBuilder();
        reportBuilder.AppendLine("=== Balance Battle Report ===");
        reportBuilder.AppendLine($"Mode: {simulationMode}");
        reportBuilder.AppendLine($"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        reportBuilder.AppendLine();

        try
        {
            if (simulationMode == SimulationMode.OneVsOneMatrix)
            {
                for (int playerIndex = 0; playerIndex < playerPrefabs.Length; playerIndex++)
                {
                    for (int enemyIndex = 0; enemyIndex < enemyPrefabs.Length; enemyIndex++)
                    {
                        oneVsOnePlayerPrefab = playerPrefabs[playerIndex];
                        oneVsOneEnemyPrefab = enemyPrefabs[enemyIndex];

                        string matchupLabel = $"{oneVsOnePlayerPrefab.name} vs {oneVsOneEnemyPrefab.name}";
                        reportBuilder.AppendLine($"--- Matchup: {matchupLabel} ---");

                        yield return StartCoroutine(SimulateRounds(rounds, matchupLabel));
                        reportBuilder.AppendLine();
                    }
                }
            }
            else
            {
                yield return StartCoroutine(SimulateRounds(rounds, simulationMode == SimulationMode.OneVsOne
                    ? $"{GetOneVsOnePlayer().name} vs {GetOneVsOneEnemy().name}"
                    : "Mixed"));
            }

            AppendSummaryToReport();
            string reportText = reportBuilder.ToString();
            Debug.Log(reportText);

            if (exportReportToFile)
                SaveReport(reportText);
        }
        finally
        {
            ClearSimulatorUnitsOnly();
            ResumeWaveSystem();
            isRunning = false;
        }
    }

    private IEnumerator SimulateRounds(int roundCount, string label)
    {
        int localPlayerWins = 0;
        int localEnemyWins = 0;
        int localDraws = 0;

        for (int round = 1; round <= roundCount; round++)
        {
            if (clearOldUnitsBeforeRound)
                ClearSimulatorUnitsOnly();

            activeRoundTrackers.Clear();
            SpawnPlayersForRound();
            SpawnEnemiesForRound();

            float elapsed = 0f;
            while (elapsed < roundDuration)
            {
                if (!HasAliveSimPlayers())
                    break;
                if (!HasAliveSimEnemies())
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            float roundEndTime = Time.time;
            RoundResult result = EvaluateRoundResult();
            RecordRoundStats(result, roundEndTime, label);

            if (result == RoundResult.PlayerWin) { totalPlayerWins++; localPlayerWins++; }
            else if (result == RoundResult.EnemyWin) { totalEnemyWins++; localEnemyWins++; }
            else { totalDraws++; localDraws++; }

            string roundLog = $"[{label}] Round {round}/{roundCount}: {result} | Alive P:{CountAliveSimPlayers()} E:{CountAliveSimEnemies()}";
            Debug.Log($"[BalanceBattleSimulator] {roundLog}");
            reportBuilder.AppendLine(roundLog);

            if (round < roundCount)
                yield return new WaitForSeconds(roundInterval);
        }

        string blockSummary = $"[{label}] PlayerWin:{localPlayerWins}, EnemyWin:{localEnemyWins}, Draw:{localDraws}";
        Debug.Log($"[BalanceBattleSimulator] {blockSummary}");
        reportBuilder.AppendLine(blockSummary);
        reportBuilder.AppendLine();
    }

    private void PauseWaveSystem()
    {
        if (!pauseWaveSystemDuringSimulation)
            return;

        pausedWaveManager = FindAnyObjectByType<WaveManager>();
        if (pausedWaveManager != null)
        {
            waveManagerWasEnabled = pausedWaveManager.enabled;
            pausedWaveManager.enabled = false;
        }

        pausedEnemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (pausedEnemySpawner != null)
        {
            enemySpawnerWasEnabled = pausedEnemySpawner.enabled;
            pausedEnemySpawner.StopAllCoroutines();
            pausedEnemySpawner.enabled = false;
        }

        Debug.Log("[BalanceBattleSimulator] WaveManager / EnemySpawner 일시 정지");
    }

    private void ResumeWaveSystem()
    {
        if (!pauseWaveSystemDuringSimulation)
            return;

        if (pausedEnemySpawner != null)
            pausedEnemySpawner.enabled = enemySpawnerWasEnabled;

        if (pausedWaveManager != null)
            pausedWaveManager.enabled = waveManagerWasEnabled;

        pausedWaveManager = null;
        pausedEnemySpawner = null;

        Debug.Log("[BalanceBattleSimulator] WaveManager / EnemySpawner 재개");
    }

    private void RecordRoundStats(RoundResult result, float roundEndTime, string matchupLabel)
    {
        foreach (BalanceUnitTracker tracker in activeRoundTrackers)
        {
            if (tracker == null)
                continue;

            Dictionary<string, SideStats> bucket = tracker.IsPlayerSide
                ? playerStatsByPrefab
                : enemyStatsByPrefab;

            if (!bucket.TryGetValue(tracker.PrefabName, out SideStats stats))
            {
                stats = new SideStats();
                bucket[tracker.PrefabName] = stats;
            }

            stats.spawnCount++;
            stats.totalSurvivalSeconds += tracker.GetSurvivalTime(roundEndTime);

            if (tracker.IsAliveAt(roundEndTime))
                stats.survivedCount++;

            if (result == RoundResult.PlayerWin && tracker.IsPlayerSide && tracker.IsAliveAt(roundEndTime))
                stats.sideWinWhileAliveCount++;
            if (result == RoundResult.EnemyWin && !tracker.IsPlayerSide && tracker.IsAliveAt(roundEndTime))
                stats.sideWinWhileAliveCount++;
        }

        if (simulationMode == SimulationMode.OneVsOne || simulationMode == SimulationMode.OneVsOneMatrix)
        {
            if (!matchupStats.TryGetValue(matchupLabel, out MatchupStats stats))
            {
                stats = new MatchupStats();
                matchupStats[matchupLabel] = stats;
            }

            if (result == RoundResult.PlayerWin) stats.playerWins++;
            else if (result == RoundResult.EnemyWin) stats.enemyWins++;
            else stats.draws++;

            foreach (BalanceUnitTracker tracker in activeRoundTrackers)
            {
                if (tracker == null)
                    continue;

                if (tracker.IsPlayerSide)
                    stats.totalPlayerSurvival += tracker.GetSurvivalTime(roundEndTime);
                else
                    stats.totalEnemySurvival += tracker.GetSurvivalTime(roundEndTime);
            }

            stats.roundCount++;
        }
    }

    private void AppendSummaryToReport()
    {
        reportBuilder.AppendLine("=== Overall ===");
        reportBuilder.AppendLine($"PlayerWin: {totalPlayerWins}");
        reportBuilder.AppendLine($"EnemyWin: {totalEnemyWins}");
        reportBuilder.AppendLine($"Draw: {totalDraws}");
        reportBuilder.AppendLine();

        AppendSideStats(reportBuilder, "Player Units", playerStatsByPrefab);
        AppendSideStats(reportBuilder, "Enemy Units", enemyStatsByPrefab);

        if (matchupStats.Count > 0)
        {
            reportBuilder.AppendLine("=== 1vs1 Matchups ===");
            foreach (KeyValuePair<string, MatchupStats> pair in matchupStats)
            {
                MatchupStats stats = pair.Value;
                float avgPlayerSurvival = stats.roundCount > 0 ? stats.totalPlayerSurvival / stats.roundCount : 0f;
                float avgEnemySurvival = stats.roundCount > 0 ? stats.totalEnemySurvival / stats.roundCount : 0f;
                reportBuilder.AppendLine(
                    $"{pair.Key} | PWin:{stats.playerWins} EWin:{stats.enemyWins} Draw:{stats.draws} | AvgSurvival P:{avgPlayerSurvival:F1}s E:{avgEnemySurvival:F1}s");
            }
        }
    }

    private static void AppendSideStats(StringBuilder builder, string title, Dictionary<string, SideStats> statsMap)
    {
        builder.AppendLine($"=== {title} ===");
        foreach (KeyValuePair<string, SideStats> pair in statsMap)
        {
            SideStats stats = pair.Value;
            float avgSurvival = stats.spawnCount > 0 ? stats.totalSurvivalSeconds / stats.spawnCount : 0f;
            float surviveRate = stats.spawnCount > 0 ? (float)stats.survivedCount / stats.spawnCount * 100f : 0f;
            builder.AppendLine(
                $"{pair.Key} | Spawns:{stats.spawnCount} Survived:{stats.survivedCount} ({surviveRate:F0}%) | AvgSurvival:{avgSurvival:F1}s | SideWinAlive:{stats.sideWinWhileAliveCount}");
        }
        builder.AppendLine();
    }

    private void SaveReport(string reportText)
    {
        string notesDir = Path.Combine(Application.dataPath, "_Workspaces/Member_Jeon/Notes");
        if (!Directory.Exists(notesDir))
            Directory.CreateDirectory(notesDir);

        string filePath = Path.Combine(notesDir, reportFileName);
        File.WriteAllText(filePath, reportText, Encoding.UTF8);
        Debug.Log($"[BalanceBattleSimulator] 리포트 저장: {filePath}");
    }

    private void SpawnPlayersForRound()
    {
        int count = simulationMode == SimulationMode.Mixed ? playersPerRound : 1;

        for (int i = 0; i < count; i++)
        {
            PlayerUnitBase prefab = PickPlayerPrefab(i);
            Transform spawn = playerSpawnPoints[i % playerSpawnPoints.Length];
            PlayerUnitBase instance = Instantiate(prefab, spawn.position, spawn.rotation);
            AttachTracker(instance.gameObject, prefab.name, true);
        }
    }

    private void SpawnEnemiesForRound()
    {
        int count = simulationMode == SimulationMode.Mixed ? enemiesPerRound : 1;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickEnemyPrefab(i);
            Transform spawn = enemySpawnPoints[i % enemySpawnPoints.Length];
            GameObject instance = Instantiate(prefab, spawn.position, spawn.rotation);
            AttachTracker(instance, prefab.name, false);
        }
    }

    private void AttachTracker(GameObject instance, string prefabName, bool isPlayerSide)
    {
        BalanceUnitTracker tracker = instance.GetComponent<BalanceUnitTracker>();
        if (tracker == null)
            tracker = instance.AddComponent<BalanceUnitTracker>();

        tracker.Initialize(prefabName, isPlayerSide);
        activeRoundTrackers.Add(tracker);
        simulatorSpawnedUnits.Add(instance);
    }

    private PlayerUnitBase PickPlayerPrefab(int index)
    {
        if (simulationMode == SimulationMode.OneVsOne || simulationMode == SimulationMode.OneVsOneMatrix)
            return GetOneVsOnePlayer();

        if (!randomPickPrefab)
            return playerPrefabs[index % playerPrefabs.Length];

        return playerPrefabs[Random.Range(0, playerPrefabs.Length)];
    }

    private GameObject PickEnemyPrefab(int index)
    {
        if (simulationMode == SimulationMode.OneVsOne || simulationMode == SimulationMode.OneVsOneMatrix)
            return GetOneVsOneEnemy();

        if (!randomPickPrefab)
            return enemyPrefabs[index % enemyPrefabs.Length];

        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    private PlayerUnitBase GetOneVsOnePlayer()
    {
        if (oneVsOnePlayerPrefab != null)
            return oneVsOnePlayerPrefab;

        return playerPrefabs[0];
    }

    private GameObject GetOneVsOneEnemy()
    {
        if (oneVsOneEnemyPrefab != null)
            return oneVsOneEnemyPrefab;

        return enemyPrefabs[0];
    }

    private RoundResult EvaluateRoundResult()
    {
        bool playerAlive = HasAliveSimPlayers();
        bool enemyAlive = HasAliveSimEnemies();

        if (playerAlive && !enemyAlive) return RoundResult.PlayerWin;
        if (!playerAlive && enemyAlive) return RoundResult.EnemyWin;
        return RoundResult.Draw;
    }

    private int CountAliveSimPlayers()
    {
        int count = 0;
        for (int i = 0; i < activeRoundTrackers.Count; i++)
        {
            BalanceUnitTracker tracker = activeRoundTrackers[i];
            if (tracker != null && tracker.IsPlayerSide && !tracker.IsUnitDead)
                count++;
        }

        return count;
    }

    private int CountAliveSimEnemies()
    {
        int count = 0;
        for (int i = 0; i < activeRoundTrackers.Count; i++)
        {
            BalanceUnitTracker tracker = activeRoundTrackers[i];
            if (tracker != null && !tracker.IsPlayerSide && !tracker.IsUnitDead)
                count++;
        }

        return count;
    }

    private bool HasAliveSimPlayers() => CountAliveSimPlayers() > 0;
    private bool HasAliveSimEnemies() => CountAliveSimEnemies() > 0;

    private void ClearSimulatorUnitsOnly()
    {
        for (int i = simulatorSpawnedUnits.Count - 1; i >= 0; i--)
        {
            GameObject unit = simulatorSpawnedUnits[i];
            if (unit != null)
                Destroy(unit);
        }

        simulatorSpawnedUnits.Clear();
        activeRoundTrackers.Clear();
    }

    private void ResetAllStats()
    {
        totalPlayerWins = 0;
        totalEnemyWins = 0;
        totalDraws = 0;
        playerStatsByPrefab.Clear();
        enemyStatsByPrefab.Clear();
        matchupStats.Clear();
        activeRoundTrackers.Clear();
        simulatorSpawnedUnits.Clear();
    }

    private bool ValidateConfig()
    {
        if (rounds <= 0 || roundDuration <= 0f)
        {
            Debug.LogWarning("[BalanceBattleSimulator] rounds / roundDuration 값을 확인해주세요.");
            return false;
        }

        if (playerPrefabs == null || playerPrefabs.Length == 0 || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[BalanceBattleSimulator] playerPrefabs / enemyPrefabs가 비어 있습니다.");
            return false;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0 || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[BalanceBattleSimulator] playerSpawnPoints / enemySpawnPoints를 설정해주세요.");
            return false;
        }

        if (simulationMode == SimulationMode.OneVsOne)
        {
            if (oneVsOnePlayerPrefab == null && playerPrefabs.Length == 0)
            {
                Debug.LogWarning("[BalanceBattleSimulator] OneVsOne 모드: oneVsOnePlayerPrefab을 설정해주세요.");
                return false;
            }

            if (oneVsOneEnemyPrefab == null && enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[BalanceBattleSimulator] OneVsOne 모드: oneVsOneEnemyPrefab을 설정해주세요.");
                return false;
            }
        }

        return true;
    }

    private class SideStats
    {
        public int spawnCount;
        public int survivedCount;
        public float totalSurvivalSeconds;
        public int sideWinWhileAliveCount;
    }

    private class MatchupStats
    {
        public int roundCount;
        public int playerWins;
        public int enemyWins;
        public int draws;
        public float totalPlayerSurvival;
        public float totalEnemySurvival;
    }

    private enum RoundResult
    {
        PlayerWin,
        EnemyWin,
        Draw
    }
}
