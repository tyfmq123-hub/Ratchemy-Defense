#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AssetDatabase에서 프로젝트 데이터를 읽어 시뮬레이터 템플릿으로 변환한다.
/// </summary>
public static class WaveSimDataLoader
{
    // ────────────────────────────────────────────────────────────
    //  플레이어 유닛 로드  (UnitCardData 기반)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 지정 폴더 내 UnitCardData 에셋을 모두 로드하여 SimUnitTemplate 목록으로 반환한다.
    /// UnitCardData.cards[].unitPrefab → PlayerUnitBase 직렬화 값으로 스탯 추출.
    /// </summary>
    public static List<SimUnitTemplate> LoadPlayerTemplates(string searchFolder)
    {
        var templates = new List<SimUnitTemplate>();

        string[] guids = AssetDatabase.FindAssets("t:UnitCardData", new[] { searchFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cardData = AssetDatabase.LoadAssetAtPath<UnitCardData>(path);
            if (cardData == null || cardData.cards == null) continue;

            foreach (var entry in cardData.cards)
            {
                if (entry == null || entry.unitPrefab == null) continue;

                var tmpl = ExtractPlayerTemplate(entry);
                if (tmpl != null) templates.Add(tmpl);
            }
        }

        // UnitCardData가 없으면 PlayerUnitDisplayData로 폴백
        if (templates.Count == 0)
        {
            string[] displayGuids = AssetDatabase.FindAssets("t:PlayerUnitDisplayData", new[] { searchFolder });
            foreach (var guid in displayGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var display = AssetDatabase.LoadAssetAtPath<PlayerUnitDisplayData>(path);
                if (display == null || display.unitPrefab == null) continue;

                var tmpl = ExtractFromDisplayData(display);
                if (tmpl != null) templates.Add(tmpl);
            }
        }

        return templates;
    }

    private static SimUnitTemplate ExtractPlayerTemplate(UnitCardEntry entry)
    {
        if (entry.unitPrefab == null) return null;

        // SerializedObject로 private 직렬화 필드 안전하게 읽기
        var so        = new SerializedObject(entry.unitPrefab);
        float maxHp   = so.FindProperty("maxHp")      ?.floatValue  ?? 100f;
        int   atk     = so.FindProperty("attackPower") ?.intValue    ?? 10;
        float atkSpd  = so.FindProperty("attackSpeed") ?.floatValue  ?? 1f;
        float movSpd  = so.FindProperty("moveSpeed")   ?.floatValue  ?? 2f;
        float range   = so.FindProperty("attackRange") ?.floatValue  ?? 1.5f;

        // 특수 컴포넌트 처리 (CoolantRat 멀티샷 등)
        float bonusSkillDps = CalculateBonusSkillDps(entry.unitPrefab.gameObject);

        string assetPath = AssetDatabase.GetAssetPath(entry.unitPrefab);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        return new SimUnitTemplate
        {
            id                  = assetGuid,
            displayName         = string.IsNullOrEmpty(entry.cardName)
                                    ? entry.unitPrefab.name
                                    : entry.cardName,
            isPlayer            = true,
            isBoss              = false,
            maxHp               = maxHp,
            damage              = atk + bonusSkillDps * 0.3f, // 스킬 DPS를 일부 반영
            attackSpeed         = atkSpd,
            moveSpeed           = movSpd,
            attackRange         = range,
            cost                = entry.cost,
            productionCooldown  = Mathf.Max(1f, entry.cost * 0.5f),
            roleTag             = InferPlayerRoleTag(entry.unitPrefab.gameObject),
            elementTag          = "",
            splashRadius        = InferSplashRadius(entry.unitPrefab.gameObject),
            damageReduction     = InferDamageReduction(entry.unitPrefab.gameObject),
        };
    }

    private static SimUnitTemplate ExtractFromDisplayData(PlayerUnitDisplayData display)
    {
        string assetPath = AssetDatabase.GetAssetPath(display);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        return new SimUnitTemplate
        {
            id                  = assetGuid,
            displayName         = display.unitName,
            isPlayer            = true,
            maxHp               = display.MaxHp,
            damage              = display.Damage,
            attackSpeed         = display.AttackSpeed,
            moveSpeed           = display.MoveSpeed,
            attackRange         = display.AttackRange,
            cost                = display.number,
            productionCooldown  = Mathf.Max(1f, display.number * 0.5f),
            roleTag             = display.roleType,
            elementTag          = display.elementType,
        };
    }

    // ────────────────────────────────────────────────────────────
    //  적 유닛 로드  (EnemyUnitData 기반)
    // ────────────────────────────────────────────────────────────

    /// <summary>지정 폴더 내 EnemyUnitData (하위 클래스 포함) 에셋을 로드한다</summary>
    public static List<SimUnitTemplate> LoadEnemyTemplates(string searchFolder)
    {
        var templates = new List<SimUnitTemplate>();

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchFolder });
        foreach (var guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            var    asset = AssetDatabase.LoadAssetAtPath<EnemyUnitData>(path);
            if (asset == null) continue;

            var tmpl = ExtractEnemyTemplate(asset, guid);
            if (tmpl != null) templates.Add(tmpl);
        }

        templates.Sort((a, b) => string.Compare(a.displayName, b.displayName));
        return templates;
    }

    private static SimUnitTemplate ExtractEnemyTemplate(EnemyUnitData data, string guid)
    {
        float hp     = data.maxHp > 0 ? data.maxHp : data.hp;
        if (hp <= 0) hp = 100f;

        // 보스 여부 판정 (BossData 서브클래스이거나 이름에 "Boss" 포함)
        bool isBoss = data is BossData || data.unitName.ToLower().Contains("boss");

        return new SimUnitTemplate
        {
            id           = guid,
            displayName  = string.IsNullOrEmpty(data.unitName) ? data.name : data.unitName,
            isPlayer     = false,
            isBoss       = isBoss,
            maxHp        = hp,
            damage       = data.damage,
            attackSpeed  = Mathf.Max(0.1f, data.attackSpeed),
            moveSpeed    = Mathf.Max(0.1f, data.moveSpeed),
            attackRange  = Mathf.Max(0.5f, data.attackRange),
            cost         = 0,
            roleTag      = isBoss ? "Boss" : InferEnemyRoleTag(data),
            elementTag   = data.elementType,
            splashRadius = InferEnemySplash(data),
        };
    }

    // ────────────────────────────────────────────────────────────
    //  WaveData 로드
    // ────────────────────────────────────────────────────────────

    public static List<WaveData> LoadWaveData(string searchFolder)
    {
        var waves = new List<WaveData>();
        string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { searchFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var wave    = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (wave != null) waves.Add(wave);
        }
        waves.Sort((a, b) => string.Compare(a.waveName, b.waveName));
        return waves;
    }

    // ────────────────────────────────────────────────────────────
    //  태그 / 특수 능력 추론 (컴포넌트 타입 기반)
    // ────────────────────────────────────────────────────────────

    private static string InferPlayerRoleTag(GameObject prefabRoot)
    {
        // 컴포넌트 타입 이름으로 역할 추론
        var comps = prefabRoot.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            string typeName = c.GetType().Name.ToLower();
            if (typeName.Contains("tank"))      return "Tank";
            if (typeName.Contains("coolant"))   return "Ranged";
            if (typeName.Contains("insulator")) return "Support";
            if (typeName.Contains("ultimate"))  return "BossKiller";
            if (typeName.Contains("safety"))    return "Support";
        }
        return "Melee";
    }

    private static float InferSplashRadius(GameObject prefabRoot)
    {
        var comps = prefabRoot.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            string typeName = c.GetType().Name.ToLower();
            if (typeName.Contains("splash") || typeName.Contains("explosion"))
                return 2f;
        }
        return 0f;
    }

    private static float InferDamageReduction(GameObject prefabRoot)
    {
        var comps = prefabRoot.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c.GetType().Name.ToLower().Contains("tank"))
                return 0.25f; // 탱크는 25% 피해 감소 가정
        }
        return 0f;
    }

    private static float CalculateBonusSkillDps(GameObject prefabRoot)
    {
        // CoolantRat 멀티샷 보너스 계산 (기존 BalanceSimulatorService 로직 참조)
        var coolant = prefabRoot.GetComponent<CoolantRat>();
        if (coolant != null)
        {
            var so           = new SerializedObject(coolant);
            int projectiles  = so.FindProperty("attackProjectileCount")?.intValue ?? 1;
            return (projectiles - 1) * 0.5f; // 추가 투사체당 보너스
        }
        return 0f;
    }

    private static string InferEnemyRoleTag(EnemyUnitData data)
    {
        string role = (data.roleType ?? "").ToLower();
        if (role.Contains("swarm") || role.Contains("ranged") ||
            role.Contains("tank")  || role.Contains("assassin") ||
            role.Contains("boss")) return data.roleType;

        // 이름 기반 추론
        string name = data.unitName?.ToLower() ?? "";
        if (name.Contains("slime") || name.Contains("small")) return "Swarm";
        if (name.Contains("thunder") || name.Contains("lightning")) return "Ranged";
        if (name.Contains("heavy") || name.Contains("armor"))       return "Tank";

        return string.IsNullOrEmpty(data.roleType) ? "Melee" : data.roleType;
    }

    private static float InferEnemySplash(EnemyUnitData data)
    {
        string role = (data.roleType ?? "").ToLower();
        if (role.Contains("splash") || role.Contains("explosion"))
            return 2f;
        string name = (data.unitName ?? "").ToLower();
        if (name.Contains("flame") || name.Contains("fire"))
            return 1.5f;
        return 0f;
    }

    // ────────────────────────────────────────────────────────────
    //  씬의 WaveManager → WaveData 에셋 변환
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 열린 씬의 WaveManager를 찾아 전체 WaveSetting[]을 하나의 WaveData 에셋으로 합쳐 저장한다.
    /// 각 웨이브는 waveIndex * totalCountdownTime 만큼 타임오프셋이 적용된다.
    /// 배틀 씬이 열려 있어야 한다.
    /// </summary>
    public static (WaveData created, string error) ImportFromSceneWaveManager(string outputFolder)
    {
        var waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (waveManager == null)
            return (null, "씬에서 WaveManager를 찾을 수 없습니다.\n배틀 씬을 열어두고 다시 시도하세요.");

        if (waveManager.waveSettings == null || waveManager.waveSettings.Length == 0)
            return (null, "WaveManager.waveSettings가 비어 있습니다.");

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string parent     = System.IO.Path.GetDirectoryName(outputFolder).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(outputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        float waveWindow    = waveManager.totalCountdownTime;   // 웨이브당 시간
        float firstDelay    = waveManager.firstWaveSpawnDelay;  // 첫 웨이브 지연

        var merged          = ScriptableObject.CreateInstance<WaveData>();
        merged.waveName     = "AllWaves";
        merged.description  = $"WaveManager 전체 {waveManager.waveSettings.Length}웨이브 합산 — 자동 생성";

        for (int w = 0; w < waveManager.waveSettings.Length; w++)
        {
            var setting = waveManager.waveSettings[w];
            if (setting.enemyPrefabs == null || setting.enemyPrefabs.Length == 0) continue;

            // 웨이브 시작 기준 시각 (첫 웨이브만 firstDelay 추가)
            float waveStartTime = w * waveWindow + (w == 0 ? firstDelay : 0f);

            int   spawnCount = Mathf.Max(1, setting.spawnCount);
            float interval   = Mathf.Max(0.05f, setting.spawnInterval);

            // 풀 구성 — 프리팹 순서 그대로 수집 (중복 유지 → 확률 가중치 보존)
            // 예) [T1, T1, T2] → T1이 2/3 확률로 나오도록 실제 게임과 동일하게
            var pool = new List<EnemyUnitData>();
            foreach (var prefab in setting.enemyPrefabs)
            {
                if (prefab == null) continue;
                var enemyUnit = prefab.GetComponent<EnemyUnit>();
                if (enemyUnit == null) continue;
                var unitSo    = new SerializedObject(enemyUnit);
                var dataProp  = unitSo.FindProperty("enemyUnitData");
                var enemyData = dataProp?.objectReferenceValue as EnemyUnitData;
                if (enemyData != null)
                    pool.Add(enemyData);   // 중복 허용 — 등장 확률 그대로 반영
            }

            if (pool.Count == 0) continue;

            // 웨이브 전체를 하나의 풀 항목으로 — 소환마다 랜덤 선택
            merged.entries.Add(new WaveEnemyEntry
            {
                spawnTime        = waveStartTime,
                spawnInterval    = interval,
                enemyData        = pool[0],
                enemyPool        = pool,
                count            = spawnCount,
                isBoss           = false,
                waveSettingIndex = w,            // WaveManager.waveSettings[w] 역추적용
            });
        }

        if (merged.entries.Count == 0)
        {
            UnityEngine.Object.DestroyImmediate(merged);
            return (null, "변환된 항목이 없습니다. 적 프리팹에 EnemyUnit 컴포넌트가 있는지 확인하세요.");
        }

        string assetPath = $"{outputFolder}/WaveData_AllWaves.asset";
        var    existing  = AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);
        if (existing != null)
            EditorUtility.CopySerialized(merged, existing);
        else
            AssetDatabase.CreateAsset(merged, assetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return (AssetDatabase.LoadAssetAtPath<WaveData>(assetPath), null);
    }
}
#endif
