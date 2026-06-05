using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Play 없이 숫자만으로 팀 교전을 계산합니다.
/// </summary>
public static class BalanceCombatSimulator
{
    public const int DefaultCostBudget = 20;
    public const int DefaultEnemyCount = 8;
    public const int DefaultIterationCount = 1500;
    public const float DefaultStartDistance = 10f;

    public struct Fighter
    {
        public string name;
        public float hp;
        public int hitDamage;
        public float attackSpeed;
        public float moveSpeed;
        public float attackRange;
        public float damageReduction;
        public float bonusSkillDps;
        public int cost;

        public float BaseDps => hitDamage * Mathf.Max(attackSpeed, 0.01f) + bonusSkillDps;
        public float EffectiveHp => hp / Mathf.Max(1f - Mathf.Clamp01(damageReduction), 0.05f);
    }

    public struct TeamFightResult
    {
        public bool playerWin;
        public bool draw;
        public float duration;
        public int playerUnitsAlive;
        public int enemyUnitsAlive;
        public float playerHpRemaining;
        public float enemyHpRemaining;
        public int playerCostSpent;
        public int reinforcementsSpawned;
        public float deathRefundsEarned;
        public float passiveIncomeEarned;
    }

    public struct EconomySettings
    {
        public bool enabled;
        public float costIncomePerSecond;
        public float deathRefundRatio;
        public bool autoReinforce;

        public static EconomySettings Disabled => new EconomySettings { enabled = false };

        public static EconomySettings Default => new EconomySettings
        {
            enabled = true,
            costIncomePerSecond = 1f,
            deathRefundRatio = 0.5f,
            autoReinforce = true
        };
    }

    public struct BatchStats
    {
        public int totalRuns;
        public int playerWins;
        public int enemyWins;
        public int draws;
        public float avgDuration;
        public float avgPlayerUnitCount;
        public float avgEnemyUnitCount;
        public float avgPlayerCostSpent;
        public Dictionary<string, int> playerSpawnCounts;
        public Dictionary<string, int> enemySpawnCounts;
        public float avgReinforcements;
        public float avgDeathRefunds;
        public float avgPassiveIncome;
    }

    private struct BattleUnit
    {
        public Fighter template;
        public float x;
        public float y;
        public float hp;
        public float attackCooldown;
        public bool isPlayer;
        public int spawnCost;
        public bool deathRefundGranted;
        public bool IsAlive => hp > 0f;
    }

    public static List<Fighter> RollRandomPlayerTeam(IReadOnlyList<Fighter> roster, int budget, System.Random rng)
    {
        List<Fighter> team = new List<Fighter>();
        if (roster == null || roster.Count == 0 || budget <= 0)
            return team;

        int minCost = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
            minCost = Mathf.Min(minCost, Mathf.Max(1, roster[i].cost));

        int remaining = budget;
        while (remaining >= minCost)
        {
            List<Fighter> affordable = new List<Fighter>();
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i].cost > 0 && roster[i].cost <= remaining)
                    affordable.Add(roster[i]);
            }

            if (affordable.Count == 0)
                break;

            Fighter pick = affordable[rng.Next(affordable.Count)];
            team.Add(pick);
            remaining -= pick.cost;
        }

        return team;
    }

    public static List<Fighter> RollRandomEnemyTeam(IReadOnlyList<Fighter> roster, int count, System.Random rng)
    {
        List<Fighter> team = new List<Fighter>();
        if (roster == null || roster.Count == 0 || count <= 0)
            return team;

        for (int i = 0; i < count; i++)
            team.Add(roster[rng.Next(roster.Count)]);

        return team;
    }

    public static int GetTeamCost(IReadOnlyList<Fighter> team)
    {
        int total = 0;
        for (int i = 0; i < team.Count; i++)
            total += team[i].cost;
        return total;
    }

    public static TeamFightResult SimulateTeamBattle(
        IReadOnlyList<Fighter> playerTeam,
        IReadOnlyList<Fighter> enemyTeam,
        float startDistance = DefaultStartDistance,
        float maxDuration = 120f,
        EconomySettings economy = default,
        IReadOnlyList<Fighter> playerRoster = null,
        System.Random rng = null)
    {
        if (!economy.enabled)
            economy = EconomySettings.Disabled;

        List<BattleUnit> units = new List<BattleUnit>(playerTeam.Count + enemyTeam.Count);
        float costPool = 0f;
        float incomeAccumulator = 0f;
        int reinforcementsSpawned = 0;
        float deathRefundsEarned = 0f;
        float passiveIncomeEarned = 0f;
        int totalCostSpent = GetTeamCost(playerTeam);

        for (int i = 0; i < playerTeam.Count; i++)
        {
            float y = GetLanePosition(i, playerTeam.Count);
            units.Add(CreateBattleUnit(playerTeam[i], 0f, y, true));
        }

        for (int i = 0; i < enemyTeam.Count; i++)
        {
            float y = GetLanePosition(i, enemyTeam.Count);
            units.Add(CreateBattleUnit(enemyTeam[i], startDistance, y, false));
        }

        const float dt = 0.05f;
        float time = 0f;

        while (time < maxDuration && !IsBattleFinished(units, economy, costPool, playerRoster))
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].IsAlive)
                    continue;

                int targetIndex = FindNearestEnemyIndex(units, i);
                if (targetIndex < 0)
                    continue;

                BattleUnit unit = units[i];
                BattleUnit target = units[targetIndex];
                float dx = target.x - unit.x;
                float dy = target.y - unit.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                bool inRange = distance <= unit.template.attackRange;

                if (!inRange)
                {
                    float move = unit.template.moveSpeed * dt;
                    unit.x += (dx / Mathf.Max(distance, 0.01f)) * move;
                    unit.y += (dy / Mathf.Max(distance, 0.01f)) * move;
                }

                unit.attackCooldown -= dt;

                if (inRange && unit.attackCooldown <= 0f)
                {
                    BattleUnit damagedTarget = units[targetIndex];
                    float damage = unit.template.hitDamage;
                    if (!unit.isPlayer)
                        damage *= 1f - Mathf.Clamp01(damagedTarget.template.damageReduction);

                    damagedTarget.hp -= damage;
                    units[targetIndex] = damagedTarget;
                    unit.attackCooldown = 1f / Mathf.Max(unit.template.attackSpeed, 0.01f);
                }

                if (inRange && unit.template.bonusSkillDps > 0f)
                {
                    BattleUnit damagedTarget = units[targetIndex];
                    float bonus = unit.template.bonusSkillDps * dt;
                    if (!unit.isPlayer)
                        bonus *= 1f - Mathf.Clamp01(damagedTarget.template.damageReduction);

                    damagedTarget.hp -= bonus;
                    units[targetIndex] = damagedTarget;
                }

                units[i] = unit;
            }

            if (economy.enabled)
            {
                float refund = ProcessDeathRefunds(units, economy.deathRefundRatio);
                costPool += refund;
                deathRefundsEarned += refund;
                passiveIncomeEarned += ApplyPassiveIncome(ref costPool, ref incomeAccumulator, economy.costIncomePerSecond, dt);

                if (economy.autoReinforce && playerRoster != null && playerRoster.Count > 0 && rng != null)
                {
                    int spawned = TrySpawnReinforcements(units, playerRoster, ref costPool, rng, ref totalCostSpent);
                    reinforcementsSpawned += spawned;
                }
            }

            time += dt;
        }

        int playerAlive = CountAlive(units, true);
        int enemyAlive = CountAlive(units, false);
        float playerHpLeft = SumAliveHp(units, true);
        float enemyHpLeft = SumAliveHp(units, false);
        bool enemyDefeated = enemyAlive <= 0;
        bool playerDefeated = IsPlayerSideDefeated(units, economy, costPool, playerRoster);

        return new TeamFightResult
        {
            playerWin = playerAlive > 0 && enemyDefeated,
            draw = !enemyDefeated && !playerDefeated,
            duration = time,
            playerUnitsAlive = playerAlive,
            enemyUnitsAlive = enemyAlive,
            playerHpRemaining = playerHpLeft,
            enemyHpRemaining = enemyHpLeft,
            playerCostSpent = totalCostSpent,
            reinforcementsSpawned = reinforcementsSpawned,
            deathRefundsEarned = deathRefundsEarned,
            passiveIncomeEarned = passiveIncomeEarned
        };
    }

    public static BatchStats RunBatchSimulation(
        IReadOnlyList<Fighter> playerRoster,
        IReadOnlyList<Fighter> enemyRoster,
        int iterationCount,
        int costBudget,
        int enemyCount,
        float startDistance,
        int randomSeed = 0,
        EconomySettings economy = default)
    {
        if (!economy.enabled)
            economy = EconomySettings.Disabled;

        System.Random rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();

        BatchStats stats = new BatchStats
        {
            totalRuns = iterationCount,
            playerSpawnCounts = new Dictionary<string, int>(),
            enemySpawnCounts = new Dictionary<string, int>()
        };

        float totalDuration = 0f;
        float totalPlayerUnits = 0f;
        float totalEnemyUnits = 0f;
        float totalCostSpent = 0f;
        float totalReinforcements = 0f;
        float totalRefunds = 0f;
        float totalPassiveIncome = 0f;

        for (int run = 0; run < iterationCount; run++)
        {
            List<Fighter> playerTeam = RollRandomPlayerTeam(playerRoster, costBudget, rng);
            List<Fighter> enemyTeam = RollRandomEnemyTeam(enemyRoster, enemyCount, rng);

            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
                continue;

            for (int i = 0; i < playerTeam.Count; i++)
                AddCount(stats.playerSpawnCounts, playerTeam[i].name);

            for (int i = 0; i < enemyTeam.Count; i++)
                AddCount(stats.enemySpawnCounts, enemyTeam[i].name);

            TeamFightResult result = SimulateTeamBattle(
                playerTeam,
                enemyTeam,
                startDistance,
                120f,
                economy,
                playerRoster,
                rng);
            totalDuration += result.duration;
            totalPlayerUnits += playerTeam.Count + result.reinforcementsSpawned;
            totalEnemyUnits += enemyTeam.Count;
            totalCostSpent += result.playerCostSpent;
            totalReinforcements += result.reinforcementsSpawned;
            totalRefunds += result.deathRefundsEarned;
            totalPassiveIncome += result.passiveIncomeEarned;

            if (result.draw)
                stats.draws++;
            else if (result.playerWin)
                stats.playerWins++;
            else
                stats.enemyWins++;
        }

        int completed = stats.playerWins + stats.enemyWins + stats.draws;
        if (completed > 0)
        {
            stats.avgDuration = totalDuration / completed;
            stats.avgPlayerUnitCount = totalPlayerUnits / completed;
            stats.avgEnemyUnitCount = totalEnemyUnits / completed;
            stats.avgPlayerCostSpent = totalCostSpent / completed;
            stats.avgReinforcements = totalReinforcements / completed;
            stats.avgDeathRefunds = totalRefunds / completed;
            stats.avgPassiveIncome = totalPassiveIncome / completed;
        }

        return stats;
    }

    public static string BuildReport(
        IReadOnlyList<Fighter> playerRoster,
        IReadOnlyList<Fighter> enemyRoster,
        BatchStats stats,
        int costBudget,
        int enemyCount,
        float startDistance,
        int iterationCount,
        string enemyFilterDescription = null,
        EconomySettings economy = default)
    {
        if (!economy.enabled)
            economy = EconomySettings.Disabled;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Balance Log Simulator (팀전 / 계산 전용) ===");
        sb.AppendLine($"조건: 플레이어 {costBudget}코스트 랜덤 소환 | 적 {enemyCount}마리 랜덤 소환");
        sb.AppendLine($"시작 거리: {startDistance:F1} | 반복: {iterationCount}판");
        if (economy.enabled)
        {
            sb.AppendLine(
                $"경제: 초당 +{economy.costIncomePerSecond:F0} 코스트 | " +
                $"사망 환급 {economy.deathRefundRatio:P0} | " +
                $"전투 중 자동 재소환 {(economy.autoReinforce ? "ON" : "OFF")}");
        }
        if (!string.IsNullOrEmpty(enemyFilterDescription))
            sb.AppendLine($"적 필터: {enemyFilterDescription}");
        sb.AppendLine("※ Play/Instantiate 없이 프리팹·SO 스탯으로 추정한 결과입니다.");
        sb.AppendLine();

        sb.AppendLine("=== Player Roster ===");
        for (int i = 0; i < playerRoster.Count; i++)
            AppendFighterLine(sb, playerRoster[i]);

        sb.AppendLine();
        sb.AppendLine("=== Enemy Pool ===");
        for (int i = 0; i < enemyRoster.Count; i++)
            AppendFighterLine(sb, enemyRoster[i]);

        sb.AppendLine();
        sb.AppendLine("=== Batch Result ===");
        int completed = stats.playerWins + stats.enemyWins + stats.draws;
        float playerWinRate = completed > 0 ? stats.playerWins * 100f / completed : 0f;
        float enemyWinRate = completed > 0 ? stats.enemyWins * 100f / completed : 0f;
        float drawRate = completed > 0 ? stats.draws * 100f / completed : 0f;

        sb.AppendLine($"완료 판수: {completed}");
        sb.AppendLine($"플레이어 승: {stats.playerWins} ({playerWinRate:F1}%)");
        sb.AppendLine($"적 승: {stats.enemyWins} ({enemyWinRate:F1}%)");
        sb.AppendLine($"무승부(시간초과): {stats.draws} ({drawRate:F1}%)");
        sb.AppendLine($"평균 교전 시간: {stats.avgDuration:F1}s");
        sb.AppendLine($"평균 플레이어 유닛 수: {stats.avgPlayerUnitCount:F2}마리");
        sb.AppendLine($"평균 사용 코스트: {stats.avgPlayerCostSpent:F1} / {costBudget}");
        sb.AppendLine($"평균 적 수: {stats.avgEnemyUnitCount:F0}마리");
        if (economy.enabled)
        {
            sb.AppendLine($"평균 재소환: {stats.avgReinforcements:F2}마리");
            sb.AppendLine($"평균 사망 환급: {stats.avgDeathRefunds:F1} 코스트");
            sb.AppendLine($"평균 패시브 수입: {stats.avgPassiveIncome:F1} 코스트");
        }

        sb.AppendLine();
        sb.AppendLine("=== Player Spawn Frequency ===");
        AppendFrequencyLines(sb, stats.playerSpawnCounts, completed);

        sb.AppendLine();
        sb.AppendLine("=== Enemy Spawn Frequency ===");
        AppendFrequencyLines(sb, stats.enemySpawnCounts, completed * enemyCount);

        sb.AppendLine();
        sb.AppendLine("=== Sample Random Teams (마지막 시드 기준 3판) ===");
        AppendSampleTeams(sb, playerRoster, enemyRoster, costBudget, enemyCount, iterationCount + 12345);

        return sb.ToString();
    }

    private static void AppendSampleTeams(
        StringBuilder sb,
        IReadOnlyList<Fighter> playerRoster,
        IReadOnlyList<Fighter> enemyRoster,
        int costBudget,
        int enemyCount,
        int seed)
    {
        System.Random rng = new System.Random(seed);
        for (int sample = 0; sample < 3; sample++)
        {
            List<Fighter> playerTeam = RollRandomPlayerTeam(playerRoster, costBudget, rng);
            List<Fighter> enemyTeam = RollRandomEnemyTeam(enemyRoster, enemyCount, rng);
            TeamFightResult result = SimulateTeamBattle(playerTeam, enemyTeam);

            sb.AppendLine(
                $"Sample {sample + 1} | Cost {GetTeamCost(playerTeam)}/{costBudget} | " +
                $"{FormatTeam(playerTeam)} vs {FormatTeam(enemyTeam)} | " +
                (result.draw ? "Draw" : (result.playerWin ? "PlayerWin" : "EnemyWin")) +
                $" | {result.duration:F1}s");
        }
    }

    private static string FormatTeam(IReadOnlyList<Fighter> team)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < team.Count; i++)
            AddCount(counts, team[i].name);

        StringBuilder sb = new StringBuilder();
        bool first = true;
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (!first)
                sb.Append(", ");
            first = false;
            sb.Append(pair.Value > 1 ? $"{pair.Key}x{pair.Value}" : pair.Key);
        }

        return sb.Length > 0 ? sb.ToString() : "(empty)";
    }

    private static void AppendFrequencyLines(StringBuilder sb, Dictionary<string, int> counts, int totalSlots)
    {
        if (counts == null || counts.Count == 0 || totalSlots <= 0)
        {
            sb.AppendLine("(none)");
            return;
        }

        List<KeyValuePair<string, int>> sorted = new List<KeyValuePair<string, int>>(counts);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        for (int i = 0; i < sorted.Count; i++)
        {
            float rate = sorted[i].Value * 100f / totalSlots;
            sb.AppendLine($"{sorted[i].Key} | {sorted[i].Value}회 ({rate:F1}%)");
        }
    }

    private static BattleUnit CreateBattleUnit(Fighter template, float x, float y, bool isPlayer)
    {
        return new BattleUnit
        {
            template = template,
            x = x,
            y = y,
            hp = template.hp,
            attackCooldown = 0f,
            isPlayer = isPlayer,
            spawnCost = Mathf.Max(0, template.cost),
            deathRefundGranted = false
        };
    }

    private static bool IsBattleFinished(
        List<BattleUnit> units,
        EconomySettings economy,
        float costPool,
        IReadOnlyList<Fighter> playerRoster)
    {
        if (!HasAliveSide(units, false))
            return true;

        return IsPlayerSideDefeated(units, economy, costPool, playerRoster);
    }

    private static bool IsPlayerSideDefeated(
        List<BattleUnit> units,
        EconomySettings economy,
        float costPool,
        IReadOnlyList<Fighter> playerRoster)
    {
        if (HasAliveSide(units, true))
            return false;

        if (!economy.enabled || !economy.autoReinforce || playerRoster == null || playerRoster.Count == 0)
            return true;

        return costPool + 0.001f < GetMinPlayerCost(playerRoster);
    }

    private static int GetMinPlayerCost(IReadOnlyList<Fighter> roster)
    {
        int minCost = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
            minCost = Mathf.Min(minCost, Mathf.Max(1, roster[i].cost));

        return minCost == int.MaxValue ? int.MaxValue : minCost;
    }

    private static float ProcessDeathRefunds(List<BattleUnit> units, float deathRefundRatio)
    {
        float refunded = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit.IsAlive || !unit.isPlayer || unit.deathRefundGranted)
                continue;

            float amount = unit.spawnCost * Mathf.Clamp01(deathRefundRatio);
            unit.deathRefundGranted = true;
            units[i] = unit;
            refunded += amount;
        }

        return refunded;
    }

    private static float ApplyPassiveIncome(ref float costPool, ref float incomeAccumulator, float incomePerSecond, float dt)
    {
        incomeAccumulator += incomePerSecond * dt;
        int wholeIncome = Mathf.FloorToInt(incomeAccumulator);
        if (wholeIncome <= 0)
            return 0f;

        incomeAccumulator -= wholeIncome;
        costPool += wholeIncome;
        return wholeIncome;
    }

    private static int TrySpawnReinforcements(
        List<BattleUnit> units,
        IReadOnlyList<Fighter> playerRoster,
        ref float costPool,
        System.Random rng,
        ref int totalCostSpent)
    {
        int spawned = 0;

        while (true)
        {
            List<Fighter> affordable = new List<Fighter>();
            for (int i = 0; i < playerRoster.Count; i++)
            {
                if (playerRoster[i].cost > 0 && playerRoster[i].cost <= costPool)
                    affordable.Add(playerRoster[i]);
            }

            if (affordable.Count == 0)
                break;

            Fighter pick = affordable[rng.Next(affordable.Count)];
            int playerCount = CountAlive(units, true) + CountDeadPlayerSlots(units);
            float y = GetLanePosition(playerCount, playerCount + 1);
            units.Add(CreateBattleUnit(pick, 0f, y, true));
            costPool -= pick.cost;
            totalCostSpent += pick.cost;
            spawned++;
        }

        return spawned;
    }

    private static int CountDeadPlayerSlots(List<BattleUnit> units)
    {
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].isPlayer && !units[i].IsAlive)
                count++;
        }

        return count;
    }

    private static float GetLanePosition(int index, int count)
    {
        if (count <= 1)
            return 0f;

        float spacing = 1.5f;
        float center = (count - 1) * 0.5f;
        return (index - center) * spacing;
    }

    private static bool HasAliveSide(List<BattleUnit> units, bool playerSide)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].isPlayer == playerSide && units[i].IsAlive)
                return true;
        }

        return false;
    }

    private static int CountAlive(List<BattleUnit> units, bool playerSide)
    {
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].isPlayer == playerSide && units[i].IsAlive)
                count++;
        }

        return count;
    }

    private static float SumAliveHp(List<BattleUnit> units, bool playerSide)
    {
        float total = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].isPlayer == playerSide && units[i].IsAlive)
                total += units[i].hp;
        }

        return total;
    }

    private static int FindNearestEnemyIndex(List<BattleUnit> units, int selfIndex)
    {
        BattleUnit self = units[selfIndex];
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            if (i == selfIndex || !units[i].IsAlive || units[i].isPlayer == self.isPlayer)
                continue;

            float dx = units[i].x - self.x;
            float dy = units[i].y - self.y;
            float distance = dx * dx + dy * dy;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static void AddCount(Dictionary<string, int> counts, string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (counts.ContainsKey(key))
            counts[key]++;
        else
            counts[key] = 1;
    }

    private static void AppendFighterLine(StringBuilder sb, Fighter fighter)
    {
        sb.AppendLine(
            $"{fighter.name} | HP:{fighter.hp:F0} DMG:{fighter.hitDamage} AS:{fighter.attackSpeed:F2} " +
            $"Move:{fighter.moveSpeed:F1} Range:{fighter.attackRange:F1} | BaseDPS:{fighter.BaseDps:F1} " +
            $"BonusSkillDPS:{fighter.bonusSkillDps:F1} DR:{fighter.damageReduction:P0} Cost:{fighter.cost}");
    }
}
