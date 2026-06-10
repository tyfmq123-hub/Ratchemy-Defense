using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 순수 계산 기반 웨이브 시뮬레이션 엔진
/// - GameObject / Physics / Animator / Renderer 사용 없음
/// - Tick = 0.1초, 최대 180초 진행
/// - 완전 결정론적 (seed 기반 RNG)
/// </summary>
public static class WaveSimEngine
{
    // ───────────────────────────────────────────────────────────
    //  Public API
    // ───────────────────────────────────────────────────────────

    /// <summary>단일 시뮬레이션 실행. 스레드 세이프 (전역 상태 없음)</summary>
    public static SimRunResult Run(WaveSimConfig config, int seed)
    {
        var state = new RunState(config, new System.Random(seed));
        int maxTicks = Mathf.CeilToInt(config.gameDuration / config.tickDuration);

        for (int tick = 0; tick < maxTicks; tick++)
        {
            state.currentTime = (tick + 1) * config.tickDuration;
            ProcessTick(state);
            if (state.isOver) break;
        }

        // 시간 초과 = 온도 130 미달 방어 성공 → 플레이어 승리
        if (!state.isOver)
        {
            state.isOver    = true;
            state.playerWon = true;
        }

        return BuildResult(state);
    }

    // ───────────────────────────────────────────────────────────
    //  내부 런 상태 (Run() 호출마다 새로 생성)
    // ───────────────────────────────────────────────────────────

    private sealed class RunState
    {
        public float currentTime;
        public float temperature;
        public float playerCost;
        public bool  isOver;
        public bool  playerWon;

        public List<SimUnit>                 playerUnits  = new();
        public List<SimUnit>                 enemyUnits   = new();
        public List<WaveSpawnScheduleEntry>  pendingSpawns;

        public float[] unitCooldowns;   // 플레이어 유닛별 생산 쿨타임

        // 결과 수집
        public int          totalBreaches;
        public SegmentData[] segments = new SegmentData[6];
        public Dictionary<string, UnitRunStats> unitStats = new();

        public System.Random  rng;
        public WaveSimConfig  cfg;

        private int _nextId;
        public int NextId => ++_nextId;

        public RunState(WaveSimConfig c, System.Random r)
        {
            cfg         = c;
            rng         = r;
            temperature = c.startTemperature;
            playerCost  = c.initialCost;

            pendingSpawns = new List<WaveSpawnScheduleEntry>(c.spawnSchedule);
            pendingSpawns.Sort((a, b) => a.exactSpawnTime.CompareTo(b.exactSpawnTime));

            unitCooldowns = new float[c.playerTemplates.Count];

            for (int i = 0; i < 6; i++)
                segments[i] = new SegmentData { segmentIndex = i };

            foreach (var t in c.playerTemplates)
                if (!unitStats.ContainsKey(t.id))
                    unitStats[t.id] = new UnitRunStats
                        { templateId = t.id, name = t.displayName, isPlayer = true };
        }
    }

    // ───────────────────────────────────────────────────────────
    //  틱 처리  (순서: 코스트 → 스폰 → AI 생산 → 이동/전투 → 사망 → 기지 → 승패)
    // ───────────────────────────────────────────────────────────

    private static void ProcessTick(RunState s)
    {
        float dt  = s.cfg.tickDuration;
        int   seg = Mathf.Clamp((int)(s.currentTime / 30f), 0, 5);
        float tempBefore = s.temperature;

        // 1. 코스트 증가
        s.playerCost += s.cfg.costIncomePerSecond * dt;

        // 2. 생산 쿨타임 감소
        for (int i = 0; i < s.unitCooldowns.Length; i++)
            s.unitCooldowns[i] = Mathf.Max(0f, s.unitCooldowns[i] - dt);

        // 3. 적 스폰 (웨이브 스케줄)
        SpawnScheduledEnemies(s, seg);

        // 4. 플레이어 AI 생산 판단
        PlayerAIDecide(s, seg);

        // 5~6. 이동 + 공격 (플레이어 측)
        ProcessSideUnits(s.playerUnits, s.enemyUnits, dt, isPlayer: true);

        // 5~6. 이동 + 공격 (적 측)
        ProcessSideUnits(s.enemyUnits, s.playerUnits, dt, isPlayer: false);

        // 7. 사망 처리 및 통계 수집
        CollectDeadUnits(s, seg);

        // 8. 기지 도달 처리 (온도 / 플레이어 승리)
        ProcessBaseReach(s, seg);

        // 9. 온도 패배 판정
        if (!s.isOver && s.temperature >= s.cfg.defeatTemperature)
        {
            s.isOver    = true;
            s.playerWon = false;
        }

        // 구간 온도 누적
        s.segments[seg].temperatureGain += s.temperature - tempBefore;
    }

    // ───────────────────────────────────────────────────────────
    //  적 스폰
    // ───────────────────────────────────────────────────────────

    private static void SpawnScheduledEnemies(RunState s, int seg)
    {
        // pendingSpawns은 시간 오름차순 정렬되어 있음
        int i = 0;
        while (i < s.pendingSpawns.Count && s.pendingSpawns[i].exactSpawnTime <= s.currentTime)
        {
            var entry = s.pendingSpawns[i];
            s.pendingSpawns.RemoveAt(i);   // 앞에서 제거 (정렬 유지)

            var unit = new SimUnit
            {
                instanceId = s.NextId,
                template   = entry.template,
                isPlayer   = false,
                hp         = entry.template.maxHp,
                position   = s.cfg.enemySpawnX,
                spawnTime  = entry.exactSpawnTime,
            };
            s.enemyUnits.Add(unit);

            // 적 통계 초기화
            if (!s.unitStats.ContainsKey(entry.template.id))
                s.unitStats[entry.template.id] = new UnitRunStats
                    { templateId = entry.template.id, name = entry.template.displayName, isPlayer = false };

            s.segments[seg].enemySpawns++;
        }
    }

    // ───────────────────────────────────────────────────────────
    //  플레이어 AI  —  실제 플레이어 수준의 의사결정 + 약간의 랜덤
    // ───────────────────────────────────────────────────────────

    private static void PlayerAIDecide(RunState s, int seg)
    {
        if (s.cfg.playerTemplates.Count == 0) return;

        // ── 전선 상황 파악 ──────────────────────────
        float closestEnemyX = s.cfg.battlefieldLength;
        int   activeEnemies = 0;
        bool  bossPresent   = false;

        foreach (var e in s.enemyUnits)
        {
            if (!e.IsAlive) continue;
            closestEnemyX = Mathf.Min(closestEnemyX, e.position);
            activeEnemies++;
            if (e.template.isBoss) bossPresent = true;
        }

        bool frontPushed = closestEnemyX < 35f;    // 전선이 밀려오는 상황
        bool manyEnemies = activeEnemies > 6;       // 물량 압박

        // ── 소환 후보 중 우선순위 계산 ──────────────
        int   bestSlot  = -1;
        float bestScore = float.MinValue;

        for (int i = 0; i < s.cfg.playerTemplates.Count; i++)
        {
            var t = s.cfg.playerTemplates[i];
            if (s.playerCost < t.cost) continue;
            if (s.unitCooldowns[i] > 0f) continue;

            float score = 1f;
            string role = t.roleTag ?? "";

            // 상황별 우선순위 규칙
            if (frontPushed && role == "Tank")      score += 3.5f;
            if (manyEnemies && role == "Splash")    score += 2.5f;
            if (bossPresent && role == "BossKiller") score += 5.0f;
            if (frontPushed && role == "Assassin")  score += 1.5f;
            if (role == "Ranged")                   score += 0.5f; // 원거리는 항상 소환 긍정

            // 코스트 효율 보너스 (싼 유닛은 기본 활용)
            score += Mathf.Clamp(10f / Mathf.Max(t.cost, 1), 0f, 1f);

            // 약간의 랜덤성 허용
            score += (float)(s.rng.NextDouble() * 0.6 - 0.3);

            if (score > bestScore)
            {
                bestScore = score;
                bestSlot  = i;
            }
        }

        if (bestSlot >= 0)
            SpawnPlayerUnit(s, s.cfg.playerTemplates[bestSlot], bestSlot, seg);
    }

    private static void SpawnPlayerUnit(RunState s, SimUnitTemplate t, int slotIdx, int seg)
    {
        var unit = new SimUnit
        {
            instanceId = s.NextId,
            template   = t,
            isPlayer   = true,
            hp         = t.maxHp,
            position   = s.cfg.playerSpawnX,
            spawnTime  = s.currentTime,
        };
        s.playerUnits.Add(unit);

        s.playerCost -= t.cost;
        if (slotIdx < s.unitCooldowns.Length)
            s.unitCooldowns[slotIdx] = t.productionCooldown;

        s.segments[seg].playerSpawns++;

        if (s.unitStats.TryGetValue(t.id, out var st))
            st.spawnCount++;
    }

    // ───────────────────────────────────────────────────────────
    //  유닛 행동 (이동 → 적 탐색 → 공격)
    // ───────────────────────────────────────────────────────────

    private static void ProcessSideUnits(
        List<SimUnit> myUnits, List<SimUnit> enemies,
        float dt, bool isPlayer)
    {
        foreach (var unit in myUnits)
        {
            if (!unit.IsAlive) continue;

            unit.attackCooldown = Mathf.Max(0f, unit.attackCooldown - dt);

            SimUnit target = FindBestTarget(unit, enemies);

            if (target != null)
            {
                // 사거리 내 적 존재 → 쿨타임 소진 시 공격
                if (unit.attackCooldown <= 0f)
                    ExecuteAttack(unit, target, enemies);
            }
            else
            {
                // 적 없음 → 전진
                float dir = isPlayer ? 1f : -1f;
                unit.position += unit.template.moveSpeed * dt * dir;
            }
        }
    }

    /// <summary>사거리 내에서 가장 앞쪽(아군 기지에 가까운)에 있는 적을 우선 타겟</summary>
    private static SimUnit FindBestTarget(SimUnit attacker, List<SimUnit> enemies)
    {
        SimUnit best     = null;
        float   bestPos  = attacker.isPlayer ? float.MinValue : float.MaxValue;

        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            float dist = Mathf.Abs(e.position - attacker.position);
            if (dist > attacker.template.attackRange) continue;

            // 플레이어 유닛 → 적 기지에 가장 가까운 적 우선 (position 큰 쪽)
            // 적 유닛       → 플레이어 기지에 가장 가까운 적 우선 (position 작은 쪽)
            if (attacker.isPlayer)
            {
                if (best == null || e.position > bestPos) { best = e; bestPos = e.position; }
            }
            else
            {
                if (best == null || e.position < bestPos) { best = e; bestPos = e.position; }
            }
        }
        return best;
    }

    private static void ExecuteAttack(SimUnit attacker, SimUnit primaryTarget, List<SimUnit> allEnemies)
    {
        float cooldown = 1f / Mathf.Max(0.01f, attacker.template.attackSpeed);
        attacker.attackCooldown = cooldown;

        float baseDmg    = attacker.template.damage;
        float counterMult = CounterTable.GetMultiplier(attacker.template.roleTag, primaryTarget.template.roleTag);
        float dmg        = baseDmg * counterMult;

        if (attacker.template.splashRadius > 0f)
        {
            // 광역 공격
            foreach (var e in allEnemies)
            {
                if (!e.IsAlive) continue;
                if (Mathf.Abs(e.position - primaryTarget.position) <= attacker.template.splashRadius)
                    ApplyDamage(attacker, e, dmg);
            }
        }
        else
        {
            ApplyDamage(attacker, primaryTarget, dmg);
        }
    }

    private static void ApplyDamage(SimUnit attacker, SimUnit target, float dmg)
    {
        float absorbed = dmg * target.template.damageReduction;
        float actual   = dmg - absorbed;

        target.hp             -= actual;
        target.damageTaken    += actual;
        target.damageAbsorbed += absorbed;
        attacker.damageDealt  += actual;

        // 방금 죽었으면 deathTime을 임시 마킹 (-2 = 이번 틱에 죽음)
        if (target.hp <= 0f && target.deathTime < 0f)
        {
            target.deathTime = -2f;
            attacker.kills++;
        }
    }

    // ───────────────────────────────────────────────────────────
    //  사망 처리
    // ───────────────────────────────────────────────────────────

    private static void CollectDeadUnits(RunState s, int seg)
    {
        RemoveDead(s.playerUnits, s, seg, isPlayer: true);
        RemoveDead(s.enemyUnits,  s, seg, isPlayer: false);
    }

    private static void RemoveDead(List<SimUnit> units, RunState s, int seg, bool isPlayer)
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            var u = units[i];
            if (u.IsAlive) continue;

            // deathTime 확정
            u.deathTime = u.deathTime == -2f ? s.currentTime : s.currentTime;

            // 통계 기록
            if (s.unitStats.TryGetValue(u.template.id, out var st))
            {
                st.totalDamageDealt    += u.damageDealt;
                st.totalDamageAbsorbed += u.damageAbsorbed;
                st.totalKills          += u.kills;
                st.totalSurvivalTime   += u.GetSurvivalTime(s.currentTime);
                st.deathCount++;
                if (!isPlayer) st.spawnCount++;
            }

            if (isPlayer) s.segments[seg].playerDeaths++;
            else          s.segments[seg].enemyDeaths++;

            units.RemoveAt(i);
        }
    }

    // ───────────────────────────────────────────────────────────
    //  기지 도달 처리
    // ───────────────────────────────────────────────────────────

    private static void ProcessBaseReach(RunState s, int seg)
    {
        // 적 유닛 → 플레이어 기지 도달
        for (int i = s.enemyUnits.Count - 1; i >= 0; i--)
        {
            var e = s.enemyUnits[i];
            if (!e.IsAlive || e.position > s.cfg.playerBaseX) continue;

            s.temperature += s.cfg.temperaturePerBreach;
            s.totalBreaches++;
            s.segments[seg].breachCount++;
            s.segments[seg].temperatureGain += s.cfg.temperaturePerBreach;

            // 기지 도달한 적은 제거
            s.enemyUnits.RemoveAt(i);

            if (s.temperature >= s.cfg.defeatTemperature)
            {
                s.isOver    = true;
                s.playerWon = false;
                return;
            }
        }

        // 플레이어 유닛 → 적 기지 도달
        foreach (var p in s.playerUnits)
        {
            if (p.IsAlive && p.position >= s.cfg.enemyBaseX)
            {
                s.isOver    = true;
                s.playerWon = true;
                return;
            }
        }
    }

    // ───────────────────────────────────────────────────────────
    //  결과 빌드
    // ───────────────────────────────────────────────────────────

    private static SimRunResult BuildResult(RunState s)
    {
        // 생존 유닛도 통계에 반영
        foreach (var u in s.playerUnits)
        {
            if (!s.unitStats.TryGetValue(u.template.id, out var st)) continue;
            st.totalDamageDealt    += u.damageDealt;
            st.totalDamageAbsorbed += u.damageAbsorbed;
            st.totalKills          += u.kills;
            st.totalSurvivalTime   += u.GetSurvivalTime(s.currentTime);
        }
        foreach (var u in s.enemyUnits)
        {
            if (!s.unitStats.TryGetValue(u.template.id, out var est))
            {
                est = new UnitRunStats
                    { templateId = u.template.id, name = u.template.displayName, isPlayer = false };
                s.unitStats[u.template.id] = est;
            }
            est.totalDamageDealt  += u.damageDealt;
            est.totalSurvivalTime += u.GetSurvivalTime(s.currentTime);
            est.spawnCount++;
        }

        return new SimRunResult
        {
            playerWon        = s.playerWon,
            endTime          = s.currentTime,
            finalTemperature = s.temperature,
            totalBreaches    = s.totalBreaches,
            perSegmentData   = s.segments,
            unitStats        = s.unitStats,
        };
    }
}
