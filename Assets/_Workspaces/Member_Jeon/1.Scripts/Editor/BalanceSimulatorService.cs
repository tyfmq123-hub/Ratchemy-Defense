#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class BalanceSimulatorSettings
{
    public string unitCardDataPath = "Assets/_Workspaces/Member_Jeon/2.Prefabs/Data/UnitCardData.asset";
    public string enemyDataFolder = "Assets/_Workspaces/Member_Yoon/6.SO";
    public string reportFolder = "Assets/_Workspaces/Member_Jeon/Notes";
    public string reportFileName = "BalanceReport.txt";

    public int costBudget = 25;
    public int enemyCount = 5;
    public float startDistance = 10f;
    public int iterationCount = 1500;
    public int randomSeed;

    public bool includeEnemyTier1 = true;
    public bool includeEnemyTier2 = true;
    public bool includeEnemyTier3 = true;
    public float maxEnemyAttackRange;

    public bool enableBattleEconomy = true;
    public float costIncomePerSecond = 1f;
    public float deathRefundRatio = 0.5f;
    public bool autoReinforceDuringBattle = true;

    public BalanceCombatSimulator.EconomySettings GetEconomySettings()
    {
        if (!enableBattleEconomy)
            return BalanceCombatSimulator.EconomySettings.Disabled;

        return new BalanceCombatSimulator.EconomySettings
        {
            enabled = true,
            costIncomePerSecond = Mathf.Max(0f, costIncomePerSecond),
            deathRefundRatio = Mathf.Clamp01(deathRefundRatio),
            autoReinforce = autoReinforceDuringBattle
        };
    }

    public void ApplyPreset(BalanceSimulatorPreset preset)
    {
        switch (preset)
        {
            case BalanceSimulatorPreset.Realistic:
                costBudget = 25;
                enemyCount = 5;
                startDistance = 10f;
                iterationCount = 1500;
                includeEnemyTier1 = true;
                includeEnemyTier2 = true;
                includeEnemyTier3 = true;
                maxEnemyAttackRange = 0f;
                break;

            case BalanceSimulatorPreset.Standard:
                costBudget = 20;
                enemyCount = 8;
                startDistance = 10f;
                iterationCount = 1500;
                includeEnemyTier1 = true;
                includeEnemyTier2 = true;
                includeEnemyTier3 = true;
                maxEnemyAttackRange = 0f;
                break;

            case BalanceSimulatorPreset.EarlyWave:
                costBudget = 20;
                enemyCount = 4;
                startDistance = 10f;
                iterationCount = 1500;
                includeEnemyTier1 = true;
                includeEnemyTier2 = false;
                includeEnemyTier3 = false;
                maxEnemyAttackRange = 0f;
                break;

            case BalanceSimulatorPreset.StressTest:
                costBudget = 20;
                enemyCount = 8;
                startDistance = 15f;
                iterationCount = 2000;
                includeEnemyTier1 = true;
                includeEnemyTier2 = true;
                includeEnemyTier3 = true;
                maxEnemyAttackRange = 0f;
                break;

            case BalanceSimulatorPreset.MeleeOnlyEnemies:
                costBudget = 25;
                enemyCount = 5;
                startDistance = 10f;
                iterationCount = 1500;
                includeEnemyTier1 = true;
                includeEnemyTier2 = true;
                includeEnemyTier3 = true;
                maxEnemyAttackRange = 1.5f;
                break;
        }
    }

    public string GetFilterDescription()
    {
        List<string> parts = new List<string>();
        if (!includeEnemyTier1) parts.Add("T1 제외");
        if (!includeEnemyTier2) parts.Add("T2 제외");
        if (!includeEnemyTier3) parts.Add("T3 제외");
        if (maxEnemyAttackRange > 0f)
            parts.Add($"사거리 {maxEnemyAttackRange:F1} 이하 적만");

        return parts.Count > 0 ? string.Join(", ", parts) : "전체 적 풀";
    }
}

public enum BalanceSimulatorPreset
{
    Realistic,
    Standard,
    EarlyWave,
    StressTest,
    MeleeOnlyEnemies
}

public struct BalanceSimulatorRunResult
{
    public bool success;
    public string errorMessage;
    public string report;
    public string reportFilePath;
    public BalanceCombatSimulator.BatchStats stats;
    public List<BalanceCombatSimulator.Fighter> playerRoster;
    public List<BalanceCombatSimulator.Fighter> enemyRoster;
}

public static class BalanceSimulatorService
{
    public static BalanceSimulatorRunResult Run(BalanceSimulatorSettings settings)
    {
        BalanceSimulatorRunResult result = new BalanceSimulatorRunResult();

        List<BalanceCombatSimulator.Fighter> players = LoadPlayerFighters(settings.unitCardDataPath);
        List<BalanceCombatSimulator.Fighter> allEnemies = LoadEnemyFighters(settings.enemyDataFolder);
        List<BalanceCombatSimulator.Fighter> enemies = FilterEnemies(allEnemies, settings);

        result.playerRoster = players;
        result.enemyRoster = enemies;

        if (players.Count == 0)
        {
            result.errorMessage = "플레이어 유닛 데이터를 불러오지 못했습니다. UnitCardData 경로를 확인하세요.";
            return result;
        }

        if (enemies.Count == 0)
        {
            result.errorMessage = "적 데이터를 불러오지 못했습니다. 적 폴더 경로 또는 필터 조건을 확인하세요.";
            return result;
        }

        int iterations = Mathf.Max(1, settings.iterationCount);
        int costBudget = Mathf.Max(1, settings.costBudget);
        int enemyCount = Mathf.Max(1, settings.enemyCount);
        float startDistance = Mathf.Max(0.1f, settings.startDistance);

        BalanceCombatSimulator.BatchStats stats = BalanceCombatSimulator.RunBatchSimulation(
            players,
            enemies,
            iterations,
            costBudget,
            enemyCount,
            startDistance,
            settings.randomSeed,
            settings.GetEconomySettings());

        string report = BalanceCombatSimulator.BuildReport(
            players,
            enemies,
            stats,
            costBudget,
            enemyCount,
            startDistance,
            iterations,
            settings.GetFilterDescription(),
            settings.GetEconomySettings());

        result.stats = stats;
        result.report = report;
        result.reportFilePath = SaveReport(settings, report);
        result.success = true;
        return result;
    }

    public static List<BalanceCombatSimulator.Fighter> FilterEnemies(
        IReadOnlyList<BalanceCombatSimulator.Fighter> source,
        BalanceSimulatorSettings settings)
    {
        List<BalanceCombatSimulator.Fighter> filtered = new List<BalanceCombatSimulator.Fighter>();
        for (int i = 0; i < source.Count; i++)
        {
            BalanceCombatSimulator.Fighter enemy = source[i];
            if (!IsTierIncluded(enemy.name, settings))
                continue;

            if (settings.maxEnemyAttackRange > 0f && enemy.attackRange > settings.maxEnemyAttackRange)
                continue;

            filtered.Add(enemy);
        }

        return filtered;
    }

    private static bool IsTierIncluded(string enemyName, BalanceSimulatorSettings settings)
    {
        if (enemyName.Contains("_T1"))
            return settings.includeEnemyTier1;
        if (enemyName.Contains("_T2"))
            return settings.includeEnemyTier2;
        if (enemyName.Contains("_T3"))
            return settings.includeEnemyTier3;

        return true;
    }

    public static List<BalanceCombatSimulator.Fighter> LoadPlayerFighters(string unitCardDataPath)
    {
        List<BalanceCombatSimulator.Fighter> fighters = new List<BalanceCombatSimulator.Fighter>();
        UnitCardData cardData = AssetDatabase.LoadAssetAtPath<UnitCardData>(unitCardDataPath);
        if (cardData == null || cardData.cards == null)
            return fighters;

        for (int i = 0; i < cardData.cards.Length; i++)
        {
            UnitCardEntry entry = cardData.cards[i];
            if (entry == null || entry.unitPrefab == null)
                continue;

            fighters.Add(BuildPlayerFighter(entry.unitPrefab.gameObject, entry.cardName, entry.cost));
        }

        return fighters;
    }

    public static List<BalanceCombatSimulator.Fighter> LoadEnemyFighters(string enemyDataFolder)
    {
        List<BalanceCombatSimulator.Fighter> fighters = new List<BalanceCombatSimulator.Fighter>();
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { enemyDataFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null || asset.name == "EnemyUnitData" || asset.name == "BossData")
                continue;

            fighters.Add(BuildEnemyFighter(asset));
        }

        fighters.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return fighters;
    }

    private static BalanceCombatSimulator.Fighter BuildPlayerFighter(GameObject prefabRoot, string fallbackName, int cost)
    {
        PlayerUnitBase baseUnit = prefabRoot.GetComponent<PlayerUnitBase>();
        SerializedObject so = new SerializedObject(baseUnit);

        BalanceCombatSimulator.Fighter fighter = new BalanceCombatSimulator.Fighter
        {
            name = string.IsNullOrEmpty(fallbackName) ? prefabRoot.name : fallbackName,
            hp = so.FindProperty("maxHp").floatValue,
            hitDamage = so.FindProperty("attackPower").intValue,
            attackSpeed = so.FindProperty("attackSpeed").floatValue,
            moveSpeed = so.FindProperty("moveSpeed").floatValue,
            attackRange = so.FindProperty("attackRange").floatValue,
            cost = cost
        };

        CoolantRat coolant = prefabRoot.GetComponent<CoolantRat>();
        if (coolant != null)
        {
            SerializedObject coolantSo = new SerializedObject(coolant);
            int projectileCount = coolantSo.FindProperty("attackProjectileCount").intValue;
            fighter.hitDamage = fighter.hitDamage * Mathf.Max(projectileCount, 1);

            float skillDuration = coolantSo.FindProperty("skillDuration").floatValue;
            float skillFireRate = coolantSo.FindProperty("skillFireRate").floatValue;
            int skillDamage = coolantSo.FindProperty("skillProjectileDamage").intValue;
            float skillCooldown = coolantSo.FindProperty("skillCooldown").floatValue;
            float skillBurstDps = skillDamage * skillFireRate;
            float skillUptime = skillDuration / Mathf.Max(skillCooldown, 0.01f);
            fighter.bonusSkillDps = skillBurstDps * skillUptime;
        }

        TankRat tank = prefabRoot.GetComponent<TankRat>();
        if (tank != null)
        {
            SerializedObject tankSo = new SerializedObject(tank);
            int skillDamage = tankSo.FindProperty("skillDamage").intValue;
            float skillCooldown = tankSo.FindProperty("skillCooldown").floatValue;
            fighter.bonusSkillDps += skillDamage / Mathf.Max(skillCooldown, 0.01f);
        }

        UltimateRat ultimate = prefabRoot.GetComponent<UltimateRat>();
        if (ultimate != null)
        {
            SerializedObject ultimateSo = new SerializedObject(ultimate);
            int skillHits = ultimateSo.FindProperty("skillHitCount").intValue;
            float skillCooldown = ultimateSo.FindProperty("skillCooldown").floatValue;
            fighter.bonusSkillDps += (fighter.hitDamage * skillHits) / Mathf.Max(skillCooldown, 0.01f);
        }

        SafetyManagerRat safety = prefabRoot.GetComponent<SafetyManagerRat>();
        if (safety != null)
        {
            SerializedObject safetySo = new SerializedObject(safety);
            float runReduction = safetySo.FindProperty("runDamageReduction").floatValue;
            fighter.damageReduction = runReduction * 0.35f;
        }

        return fighter;
    }

    private static BalanceCombatSimulator.Fighter BuildEnemyFighter(ScriptableObject asset)
    {
        SerializedObject so = new SerializedObject(asset);
        float maxHp = ReadNumeric(so.FindProperty("maxHp"));
        if (maxHp <= 0f)
            maxHp = ReadNumeric(so.FindProperty("hp"));

        BalanceCombatSimulator.Fighter fighter = new BalanceCombatSimulator.Fighter
        {
            name = asset.name,
            hp = maxHp,
            hitDamage = Mathf.RoundToInt(ReadNumeric(so.FindProperty("damage"))),
            attackSpeed = ReadNumeric(so.FindProperty("attackSpeed")),
            moveSpeed = ReadNumeric(so.FindProperty("moveSpeed")),
            attackRange = ReadNumeric(so.FindProperty("attackRange")),
            cost = 0
        };

        SerializedProperty auraDamage = so.FindProperty("auraDamage");
        SerializedProperty auraInterval = so.FindProperty("auraInterval");
        float auraIntervalValue = ReadNumeric(auraInterval);
        if (auraDamage != null && auraIntervalValue > 0f)
            fighter.bonusSkillDps += ReadNumeric(auraDamage) / auraIntervalValue;

        SerializedProperty explosionDamage = so.FindProperty("explosionDamage");
        SerializedProperty explosionDelay = so.FindProperty("explosionDelay");
        if (explosionDamage != null && explosionDelay != null)
        {
            float cycle = Mathf.Max(ReadNumeric(explosionDelay) + 5f, 1f);
            fighter.bonusSkillDps += ReadNumeric(explosionDamage) / cycle;
        }

        return fighter;
    }

    private static float ReadNumeric(SerializedProperty property)
    {
        if (property == null)
            return 0f;

        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue;
            case SerializedPropertyType.Float:
                return property.floatValue;
            default:
                return 0f;
        }
    }

    private static string SaveReport(BalanceSimulatorSettings settings, string report)
    {
        if (!Directory.Exists(settings.reportFolder))
            Directory.CreateDirectory(settings.reportFolder);

        string path = Path.Combine(settings.reportFolder, settings.reportFileName);
        File.WriteAllText(path, report);
        AssetDatabase.Refresh();
        return path;
    }
}
#endif
