#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class BalanceDataFile
{
    public string exportedAt;
    public string unityVersion;
    public FighterExportData[] players;
    public FighterExportData[] enemies;
}

[Serializable]
public class FighterExportData
{
    public string id;
    public string name;
    public float hp;
    public int hitDamage;
    public float attackSpeed;
    public float moveSpeed;
    public float attackRange;
    public float damageReduction;
    public float bonusSkillDps;
    public int cost;
}

public static class BalanceSimulatorDataExport
{
    private const string ExportPath =
        "Assets/_Workspaces/Member_Jeon/BalanceSimWeb/data/balance-data.json";

    private const string EmbeddedJsPath =
        "Assets/_Workspaces/Member_Jeon/BalanceSimWeb/embedded-data.js";

    [MenuItem("Member_Jeon/Balance/Export Web Tool Data (JSON)")]
    public static void ExportWebToolData()
    {
        BalanceSimulatorSettings settings = new BalanceSimulatorSettings();
        var players = BalanceSimulatorService.LoadPlayerFighters(settings.unitCardDataPath);
        var enemies = BalanceSimulatorService.LoadEnemyFighters(settings.enemyDataFolder);

        if (players.Count == 0 || enemies.Count == 0)
        {
            Debug.LogError("[BalanceSimulator] Export 실패 — 플레이어 또는 적 데이터가 없습니다.");
            return;
        }

        BalanceDataFile data = new BalanceDataFile
        {
            exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            unityVersion = Application.unityVersion,
            players = ToExportArray(players, true),
            enemies = ToExportArray(enemies, false)
        };

        string json = JsonUtility.ToJson(data, true);
        string directory = Path.GetDirectoryName(ExportPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(ExportPath, json);
        File.WriteAllText(EmbeddedJsPath, "window.EMBEDDED_BALANCE_DATA = " + json + ";\n");
        AssetDatabase.Refresh();

        Debug.Log($"[BalanceSimulator] 웹 툴 데이터 export 완료:\n- {ExportPath}\n- {EmbeddedJsPath}");
        EditorUtility.RevealInFinder(ExportPath);
    }

    private static FighterExportData[] ToExportArray(
        System.Collections.Generic.List<BalanceCombatSimulator.Fighter> fighters,
        bool isPlayer)
    {
        FighterExportData[] array = new FighterExportData[fighters.Count];
        for (int i = 0; i < fighters.Count; i++)
        {
            BalanceCombatSimulator.Fighter fighter = fighters[i];
            array[i] = new FighterExportData
            {
                id = MakeId(fighter.name, i),
                name = fighter.name,
                hp = fighter.hp,
                hitDamage = fighter.hitDamage,
                attackSpeed = fighter.attackSpeed,
                moveSpeed = fighter.moveSpeed,
                attackRange = fighter.attackRange,
                damageReduction = fighter.damageReduction,
                bonusSkillDps = fighter.bonusSkillDps,
                cost = isPlayer ? fighter.cost : 0
            };
        }

        return array;
    }

    private static string MakeId(string name, int index)
    {
        if (string.IsNullOrEmpty(name))
            return "unit_" + index;

        return name.Replace(" ", "_");
    }
}
#endif
