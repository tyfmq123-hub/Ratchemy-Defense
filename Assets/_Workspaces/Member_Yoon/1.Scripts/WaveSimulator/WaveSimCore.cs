using System.Collections.Generic;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════
//  SimUnitTemplate  —  ScriptableObject에서 변환된 불변 유닛 정의
// ═══════════════════════════════════════════════════════════════════
[System.Serializable]
public class SimUnitTemplate
{
    public string id;           // AssetDatabase GUID 기반 유일 키
    public string displayName;
    public bool   isPlayer;
    public bool   isBoss;

    // ── 기본 전투 스탯 ─────────────────────────────
    public float maxHp;
    public float damage;
    public float attackSpeed;       // 초당 공격 횟수
    public float moveSpeed;         // 초당 이동 단위
    public float attackRange;       // 공격 판정 거리

    // ── 경제 ──────────────────────────────────────
    public int   cost;
    public float productionCooldown = 3f;

    // ── 태그 / 특수 ───────────────────────────────
    public string roleTag;          // Tank, Assassin, Splash, Ranged, Swarm, BossKiller, Boss, Support, Melee
    public string elementTag;
    public float  splashRadius  = 0f;   // 0 = 단일 대상
    public float  damageReduction = 0f; // 0~1  피해 감소율

    // ── 딥 복사 (최적화 루프에서 수치 조정용) ────────
    public SimUnitTemplate Clone()
    {
        return (SimUnitTemplate)MemberwiseClone();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  SimUnit  —  한 번의 시뮬레이션 런에서 살아있는 유닛 인스턴스
// ═══════════════════════════════════════════════════════════════════
public class SimUnit
{
    private static int _globalIdCounter;

    public int            instanceId  = ++_globalIdCounter;
    public SimUnitTemplate template;
    public bool           isPlayer;

    public float hp;
    public float position;          // 1D x 좌표
    public float attackCooldown;

    public bool IsAlive => hp > 0f;

    // ── 단일 런 통계 ──────────────────────────────
    public float damageDealt;
    public float damageTaken;
    public float damageAbsorbed;
    public int   kills;
    public float spawnTime;
    public float deathTime = -1f;   // -1 = 아직 살아있음

    public float GetSurvivalTime(float currentTime)
        => deathTime >= 0f ? deathTime - spawnTime : currentTime - spawnTime;

    public static void ResetIdCounter() => _globalIdCounter = 0;
}

// ═══════════════════════════════════════════════════════════════════
//  WaveSpawnScheduleEntry  —  WaveData를 시간 순으로 전개한 개별 소환 이벤트
// ═══════════════════════════════════════════════════════════════════
public class WaveSpawnScheduleEntry
{
    public float          exactSpawnTime;
    public SimUnitTemplate template;
    public bool           isBoss;
    public int            originalEntryIndex; // WaveData.entries 인덱스 (최적화용)
}

// ═══════════════════════════════════════════════════════════════════
//  WaveSimConfig  —  단일 시뮬레이션 실행 설정
// ═══════════════════════════════════════════════════════════════════
public class WaveSimConfig
{
    // ── 게임 규칙 ──────────────────────────────────
    public float gameDuration         = 180f;
    public float tickDuration         = 0.1f;
    public float startTemperature     = 20f;
    public float temperaturePerBreach = 10f;
    public float defeatTemperature    = 130f;

    // ── 경제 ──────────────────────────────────────
    public float costIncomePerSecond  = 3f;
    public float initialCost          = 10f;

    // ── 전장 ──────────────────────────────────────
    public float battlefieldLength = 100f;
    public float playerSpawnX      = 5f;
    public float enemySpawnX       = 95f;
    public float playerBaseX       = 0f;    // 적이 여기 도달 시 온도 상승
    public float enemyBaseX        = 100f;  // 플레이어가 여기 도달 시 승리

    // ── 데이터 ────────────────────────────────────
    public List<SimUnitTemplate>        playerTemplates = new();
    public List<WaveSpawnScheduleEntry> spawnSchedule   = new();

    public int seed = 0;

    // ── 유틸 ──────────────────────────────────────
    /// <summary>
    /// WaveData 항목들을 시간순 개별 소환 이벤트로 전개한다.
    /// enemyPool이 있으면 seed 기반 RNG로 소환마다 랜덤 선택 — 실제 게임 WaveSetting 방식과 동일.
    /// </summary>
    public static List<WaveSpawnScheduleEntry> ExpandWaveData(
        List<WaveEnemyEntry> entries,
        List<SimUnitTemplate> enemyTemplates,
        int seed = 0)
    {
        var schedule = new List<WaveSpawnScheduleEntry>();
        var rng      = new System.Random(seed);

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // 풀 구성: enemyPool 우선, 없으면 enemyData 단일 항목
            var pool = new List<SimUnitTemplate>();

            if (entry.enemyPool != null && entry.enemyPool.Count > 0)
            {
                foreach (var ed in entry.enemyPool)
                {
                    if (ed == null) continue;
                    var t = enemyTemplates.Find(
                        t => t.id == ed.name || t.displayName == ed.unitName);
                    if (t != null) pool.Add(t);
                }
            }
            else if (entry.enemyData != null)
            {
                var t = enemyTemplates.Find(
                    t => t.id == entry.enemyData.name || t.displayName == entry.enemyData.unitName);
                if (t != null) pool.Add(t);
            }

            if (pool.Count == 0) continue;

            int   spawnCount = Mathf.Max(1, entry.count);
            float interval   = Mathf.Max(0.05f, entry.spawnInterval);

            for (int k = 0; k < spawnCount; k++)
            {
                // 풀에서 랜덤 선택 (풀이 1개면 사실상 고정)
                var tmpl = pool[rng.Next(pool.Count)];

                schedule.Add(new WaveSpawnScheduleEntry
                {
                    exactSpawnTime     = entry.spawnTime + k * interval,
                    template           = tmpl,
                    isBoss             = entry.isBoss,
                    originalEntryIndex = i,
                });
            }
        }

        schedule.Sort((a, b) => a.exactSpawnTime.CompareTo(b.exactSpawnTime));
        return schedule;
    }
}

// ═══════════════════════════════════════════════════════════════════
//  CounterTable  —  태그 기반 상성 시스템 (확장 가능)
// ═══════════════════════════════════════════════════════════════════
public static class CounterTable
{
    // (공격자 태그, 방어자 태그) → 데미지 배율
    private static readonly Dictionary<(string, string), float> _table
        = new Dictionary<(string, string), float>
    {
        { ("Splash",     "Swarm"),      1.5f },
        { ("Assassin",   "Ranged"),     1.5f },
        { ("BossKiller", "Boss"),       2.0f },
        { ("Tank",       "Assassin"),   0.6f },  // Tank이 Assassin 피해 40% 감소 효과
        { ("Ranged",     "Melee"),      1.2f },
        { ("Support",    "Tank"),       1.2f },
        { ("Swarm",      "Support"),    1.3f },
        { ("Melee",      "Support"),    1.3f },
    };

    // 런타임에 항목 추가 가능 (확장성)
    public static void AddCounter(string attacker, string defender, float multiplier)
        => _table[(attacker, defender)] = multiplier;

    /// <summary>공격자 roleTag → 방어자 roleTag 간 데미지 배율 반환 (없으면 1.0)</summary>
    public static float GetMultiplier(string attackerRole, string defenderRole)
    {
        if (string.IsNullOrEmpty(attackerRole) || string.IsNullOrEmpty(defenderRole))
            return 1f;
        return _table.TryGetValue((attackerRole, defenderRole), out float m) ? m : 1f;
    }
}
