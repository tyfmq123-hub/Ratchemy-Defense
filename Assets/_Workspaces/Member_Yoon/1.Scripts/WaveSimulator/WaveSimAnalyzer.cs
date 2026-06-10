using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시뮬레이션 배치 결과를 집계·분석하고 난이도 문제 및 추천을 생성한다.
/// </summary>
public static class WaveSimAnalyzer
{
    // ────────────────────────────────────────────────────────────
    //  목표 범위 상수
    // ────────────────────────────────────────────────────────────
    public const float TARGET_WIN_RATE_LOW  = 0.60f;
    public const float TARGET_WIN_RATE_HIGH = 0.70f;
    public const float TARGET_END_TIME_LOW  = 170f;
    public const float TARGET_END_TIME_HIGH = 180f;
    public const float TARGET_TEMP_LOW      = 90f;
    public const float TARGET_TEMP_HIGH     = 120f;

    // ────────────────────────────────────────────────────────────
    //  Aggregate  —  N회 런 → BatchSimResult
    // ────────────────────────────────────────────────────────────

    public static BatchSimResult Aggregate(List<SimRunResult> runs)
    {
        var batch = new BatchSimResult
        {
            totalRuns    = runs.Count,
            playerWins   = 0,
            segmentStats = new BatchSegmentStats[6],
        };

        if (runs.Count == 0) return batch;

        for (int i = 0; i < 6; i++)
            batch.segmentStats[i] = new BatchSegmentStats { segmentIndex = i };

        // 누적 변수 (double 정밀도 사용)
        double sumEndTime  = 0, sumTemp  = 0, sumBreaches = 0;
        double[] segTemp    = new double[6];
        double[] segBreach  = new double[6];
        double[] segPSpawn  = new double[6];
        double[] segESpawn  = new double[6];
        double[] segPDeath  = new double[6];
        double[] segEDeath  = new double[6];
        int[]    segActive  = new int[6]; // 이 구간에 게임이 진행 중이었던 런 수

        // unitId → 누적 값
        var uAccum = new Dictionary<string, UnitAccum>();

        foreach (var r in runs)
        {
            if (r.playerWon) batch.playerWins++;
            sumEndTime  += r.endTime;
            sumTemp     += r.finalTemperature;
            sumBreaches += r.totalBreaches;

            for (int i = 0; i < 6 && r.perSegmentData != null; i++)
            {
                float segStart = i * 30f;
                if (r.endTime < segStart) continue; // 이 구간 이전에 종료됨

                segActive[i]++;
                var sd = r.perSegmentData[i];
                segTemp[i]   += sd.temperatureGain;
                segBreach[i] += sd.breachCount;
                segPSpawn[i] += sd.playerSpawns;
                segESpawn[i] += sd.enemySpawns;
                segPDeath[i] += sd.playerDeaths;
                segEDeath[i] += sd.enemyDeaths;
            }

            if (r.unitStats == null) continue;
            foreach (var kv in r.unitStats)
            {
                var us = kv.Value;
                if (!uAccum.TryGetValue(kv.Key, out var acc))
                {
                    acc = new UnitAccum { name = us.name, isPlayer = us.isPlayer };
                    uAccum[kv.Key] = acc;
                }
                acc.dmgDealt   += us.totalDamageDealt;
                acc.dmgAbsorb  += us.totalDamageAbsorbed;
                acc.kills      += us.totalKills;
                acc.spawns     += us.spawnCount;
                acc.survTime   += us.totalSurvivalTime;
                acc.deaths     += us.deathCount;
                acc.runCount++;
                // 실제로 소환된 런만 채용률에 반영 (플레이어 유닛은 항상 unitStats에 등록되므로)
                if (us.spawnCount > 0) acc.usedRunCount++;
            }
        }

        int n = runs.Count;
        batch.avgEndTime          = (float)(sumEndTime  / n);
        batch.avgFinalTemperature = (float)(sumTemp     / n);
        batch.avgBreaches         = (float)(sumBreaches / n);

        for (int i = 0; i < 6; i++)
        {
            int cnt = Mathf.Max(segActive[i], 1);
            var ss  = batch.segmentStats[i];
            ss.avgTemperatureGain = (float)(segTemp[i]   / cnt);
            ss.avgBreachCount     = (float)(segBreach[i] / cnt);
            ss.avgPlayerSpawns    = (float)(segPSpawn[i] / cnt);
            ss.avgEnemySpawns     = (float)(segESpawn[i] / cnt);
            ss.avgPlayerDeaths    = (float)(segPDeath[i] / cnt);
            ss.avgEnemyDeaths     = (float)(segEDeath[i] / cnt);
            ss.survivorRate       = (float)segActive[i] / n;
        }

        foreach (var kv in uAccum)
        {
            int rc    = Mathf.Max(kv.Value.runCount, 1);
            var bu    = new BatchUnitStats
            {
                templateId         = kv.Key,
                name               = kv.Value.name,
                isPlayer           = kv.Value.isPlayer,
                avgDamageDealt     = (float)(kv.Value.dmgDealt  / rc),
                avgDamageAbsorbed  = (float)(kv.Value.dmgAbsorb / rc),
                avgKills           = (float)(kv.Value.kills     / rc),
                avgSpawnCount      = (float)(kv.Value.spawns    / rc),
                avgSurvivalTime    = (float)(kv.Value.survTime  / rc),
                avgDeathCount      = (float)(kv.Value.deaths    / rc),
                usageRate          = (float)kv.Value.usedRunCount / n,
            };
            batch.unitStats[kv.Key] = bu;
        }

        batch.difficultyIssues = DetectDifficultyIssues(batch);
        batch.recommendations  = GenerateRecommendations(batch);
        return batch;
    }

    private struct UnitAccum
    {
        public string name;
        public bool   isPlayer;
        public double dmgDealt, dmgAbsorb, kills, spawns, survTime, deaths;
        public int    runCount;
        public int    usedRunCount; // 실제로 1번 이상 소환된 런 수
    }

    // ────────────────────────────────────────────────────────────
    //  DetectDifficultyIssues  —  자동 난이도 문제 탐지
    // ────────────────────────────────────────────────────────────

    public static List<DifficultyIssue> DetectDifficultyIssues(BatchSimResult batch)
    {
        var issues = new List<DifficultyIssue>();
        float win = batch.WinRate;

        // ── 전체 승률 판단 ──────────────────────────────────
        if (win > 0.85f)
            issues.Add(new DifficultyIssue
            {
                type         = DifficultyIssueType.TooEasy,
                segmentIndex = -1,
                description  = $"전체 승률 {win:P0}  →  너무 쉬움 (목표: 60~70%)",
                severity     = Mathf.Clamp01((win - 0.85f) / 0.15f),
            });
        else if (win < 0.40f)
            issues.Add(new DifficultyIssue
            {
                type         = DifficultyIssueType.TooHard,
                segmentIndex = -1,
                description  = $"전체 승률 {win:P0}  →  너무 어려움 (목표: 60~70%)",
                severity     = Mathf.Clamp01((0.40f - win) / 0.40f),
            });

        // ── 구간별 분석 ─────────────────────────────────────
        for (int i = 0; i < 6; i++)
        {
            var ss = batch.segmentStats[i];

            // 적 소환 없는 공백 구간
            if (i > 0 && ss.avgEnemySpawns < 0.5f)
                issues.Add(new DifficultyIssue
                {
                    type         = DifficultyIssueType.EmptyPeriod,
                    segmentIndex = i,
                    description  = $"[{ss.StartSec}~{ss.EndSec}초] 평균 적 소환 {ss.avgEnemySpawns:F1}회 — 난이도 공백",
                    severity     = 0.4f,
                });

            // 온도 급상승 구간
            if (ss.avgTemperatureGain > 25f)
                issues.Add(new DifficultyIssue
                {
                    type         = DifficultyIssueType.TempSpike,
                    segmentIndex = i,
                    description  = $"[{ss.StartSec}~{ss.EndSec}초] 구간 온도 +{ss.avgTemperatureGain:F0}도 — 급상승",
                    severity     = Mathf.Clamp01(ss.avgTemperatureGain / 50f),
                });

            // 온도 기여 너무 낮음
            if (i > 0 && i < 5 && ss.avgTemperatureGain < 3f && ss.survivorRate > 0.5f)
                issues.Add(new DifficultyIssue
                {
                    type         = DifficultyIssueType.LowPressure,
                    segmentIndex = i,
                    description  = $"[{ss.StartSec}~{ss.EndSec}초] 온도 기여 {ss.avgTemperatureGain:F1}도 — 압박 부족",
                    severity     = Mathf.Clamp01(1f - ss.avgTemperatureGain / 3f),
                });
        }

        // ── 인접 구간 난이도 급변 ──────────────────────────
        for (int i = 1; i < 6; i++)
        {
            float prev = batch.segmentStats[i - 1].avgTemperatureGain;
            float curr = batch.segmentStats[i    ].avgTemperatureGain;

            if (curr > prev * 2.5f && curr > 10f)
            {
                var ss = batch.segmentStats[i];
                issues.Add(new DifficultyIssue
                {
                    type         = DifficultyIssueType.SpikeHard,
                    segmentIndex = i,
                    description  = $"[{ss.StartSec}~{ss.EndSec}초] 이전 구간 대비 온도 {curr / Mathf.Max(prev, 0.1f):F1}배 급상승",
                    severity     = Mathf.Clamp01((curr - prev * 2.5f) / 20f),
                });
            }
        }

        issues.Sort((a, b) => b.severity.CompareTo(a.severity));
        return issues;
    }

    // ────────────────────────────────────────────────────────────
    //  GenerateRecommendations  —  웨이브 밸런스 추천 생성
    // ────────────────────────────────────────────────────────────

    public static List<BalanceRecommendation> GenerateRecommendations(BatchSimResult batch)
    {
        var recs = new List<BalanceRecommendation>();

        float win = batch.WinRate;

        // 목표 범위(60~70%) 안이면 추천 없음
        if (win >= TARGET_WIN_RATE_LOW && win <= TARGET_WIN_RATE_HIGH) return recs;

        float delta        = win - (TARGET_WIN_RATE_LOW + TARGET_WIN_RATE_HIGH) / 2f;
        float adjustFactor = Mathf.Clamp(Mathf.Abs(delta) * 1.5f, 0.05f, 0.40f);

        if (win > TARGET_WIN_RATE_HIGH)
        {
            // 너무 쉬움 → 적 강화
            recs.Add(new BalanceRecommendation
            {
                target                   = RecommendationTarget.EnemyCount,
                description              = $"[적 수량 증가] 승률 {win:P0}이 목표보다 높음 — 적 수량 ×{1f + adjustFactor:F2} 추천",
                currentValue             = 1f,
                recommendedValue         = 1f + adjustFactor,
                expectedWinRateDelta     = -delta * 0.6f,
                expectedTemperatureDelta = delta * 10f,
            });
            recs.Add(new BalanceRecommendation
            {
                target                   = RecommendationTarget.EnemyHp,
                description              = $"[적 HP 증가] 승률 {win:P0}이 목표보다 높음 — 적 HP ×{1f + adjustFactor * 0.6f:F2} 추천",
                currentValue             = 1f,
                recommendedValue         = 1f + adjustFactor * 0.6f,
                expectedWinRateDelta     = -delta * 0.4f,
                expectedTemperatureDelta = delta * 5f,
            });
        }
        else // win < TARGET_WIN_RATE_LOW
        {
            // 너무 어려움 → 적 약화
            recs.Add(new BalanceRecommendation
            {
                target                   = RecommendationTarget.EnemyCount,
                description              = $"[적 수량 감소] 승률 {win:P0}이 목표보다 낮음 — 적 수량 ×{1f - adjustFactor:F2} 추천",
                currentValue             = 1f,
                recommendedValue         = Mathf.Max(0.5f, 1f - adjustFactor),
                expectedWinRateDelta     = -delta * 0.6f,
                expectedTemperatureDelta = delta * 10f,
            });
            recs.Add(new BalanceRecommendation
            {
                target                   = RecommendationTarget.EnemyHp,
                description              = $"[적 HP 감소] 승률 {win:P0}이 목표보다 낮음 — 적 HP ×{1f - adjustFactor * 0.5f:F2} 추천",
                currentValue             = 1f,
                recommendedValue         = Mathf.Max(0.5f, 1f - adjustFactor * 0.5f),
                expectedWinRateDelta     = -delta * 0.4f,
                expectedTemperatureDelta = delta * 5f,
            });
        }

        // 종료 시간 추천
        if (batch.avgEndTime < TARGET_END_TIME_LOW && win > TARGET_WIN_RATE_HIGH)
        {
            recs.Add(new BalanceRecommendation
            {
                target       = RecommendationTarget.EnemyCount,
                description  = $"[플레이 시간 연장] 평균 종료 {batch.avgEndTime:F0}초 — 후반 적 추가 권장",
                currentValue = batch.avgEndTime,
                recommendedValue = TARGET_END_TIME_LOW,
            });
        }

        // 온도 추천
        if (batch.avgFinalTemperature < TARGET_TEMP_LOW)
        {
            recs.Add(new BalanceRecommendation
            {
                target       = RecommendationTarget.EnemyCount,
                description  = $"[온도 부족] 평균 최종 온도 {batch.avgFinalTemperature:F0}도 — 목표 90~120도 미달",
                currentValue = batch.avgFinalTemperature,
                recommendedValue = TARGET_TEMP_LOW,
                expectedTemperatureDelta = TARGET_TEMP_LOW - batch.avgFinalTemperature,
            });
        }
        else if (batch.avgFinalTemperature > TARGET_TEMP_HIGH && win < TARGET_WIN_RATE_LOW)
        {
            recs.Add(new BalanceRecommendation
            {
                target       = RecommendationTarget.EnemyHp,
                description  = $"[온도 과다] 평균 최종 온도 {batch.avgFinalTemperature:F0}도 — 적 HP 또는 수량 감소 권장",
                currentValue = batch.avgFinalTemperature,
                recommendedValue = TARGET_TEMP_HIGH,
                expectedTemperatureDelta = TARGET_TEMP_HIGH - batch.avgFinalTemperature,
                expectedWinRateDelta = 0.10f,
            });
        }

        return recs;
    }

    // ────────────────────────────────────────────────────────────
    //  ComputeCostEfficiency  —  플레이어 유닛 코스트 효율 계산
    // ────────────────────────────────────────────────────────────

    public static void ComputeCostEfficiency(
        BatchSimResult batch,
        List<SimUnitTemplate> playerTemplates)
    {
        foreach (var t in playerTemplates)
        {
            if (!batch.unitStats.TryGetValue(t.id, out var bu)) continue;
            float dps = t.damage * t.attackSpeed;
            bu.costEfficiency = t.cost > 0 ? dps / t.cost : 0f;
        }
    }
}
