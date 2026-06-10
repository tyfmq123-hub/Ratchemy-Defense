#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 목표 승률 / 종료시간 / 온도 범위를 만족하는 웨이브 구성을 자동 탐색한다.
/// 힐-클라이밍 알고리즘 기반 반복 최적화.
/// </summary>
public static class WaveSimOptimizer
{
    // ────────────────────────────────────────────────────────────
    //  최적화 목표
    // ────────────────────────────────────────────────────────────
    public class OptimizationTargets
    {
        public float winRateLow  = WaveSimAnalyzer.TARGET_WIN_RATE_LOW;
        public float winRateHigh = WaveSimAnalyzer.TARGET_WIN_RATE_HIGH;
        public float endTimeLow  = WaveSimAnalyzer.TARGET_END_TIME_LOW;
        public float endTimeHigh = WaveSimAnalyzer.TARGET_END_TIME_HIGH;
        public float tempLow     = WaveSimAnalyzer.TARGET_TEMP_LOW;
        public float tempHigh    = WaveSimAnalyzer.TARGET_TEMP_HIGH;

        // 최적화 파라미터
        public int  maxIterations    = 30;
        public int  runsPerIteration = 200;
        public bool adjustCount      = true;
        public bool adjustHp         = true;
        public bool adjustDamage     = false;
        public bool adjustInterval   = false;

        // 조정 폭 제한 (원본 대비 배율 범위)
        public float minScaleFactor = 0.5f;
        public float maxScaleFactor = 2.5f;
    }

    // ────────────────────────────────────────────────────────────
    //  Run  —  동기 최적화 실행
    // ────────────────────────────────────────────────────────────

    /// <param name="baseConfig">원본 WaveSimConfig (복사해서 조정)</param>
    /// <param name="waveEntries">원본 WaveData.entries (수치 추천 생성에 사용)</param>
    /// <param name="targets">최적화 목표</param>
    /// <param name="onProgress">진행률 콜백 (0~1, message)</param>
    public static OptimizationResult Run(
        WaveSimConfig baseConfig,
        List<WaveEnemyEntry> waveEntries,
        OptimizationTargets targets,
        Action<float, string> onProgress = null)
    {
        var result = new OptimizationResult();

        // ── 적 엔트리별 조정 배율 초기화 (entryIndex → scale) ──
        var countScales    = new Dictionary<int, float>();
        var hpScales       = new Dictionary<int, float>();
        var dmgScales      = new Dictionary<int, float>();
        var intervalScales = new Dictionary<int, float>();

        // 스폰 스케줄 원본에서 엔트리 인덱스별 템플릿 수집
        var entryTemplates = new Dictionary<int, SimUnitTemplate>();
        foreach (var sp in baseConfig.spawnSchedule)
        {
            if (!entryTemplates.ContainsKey(sp.originalEntryIndex))
                entryTemplates[sp.originalEntryIndex] = sp.template;
            countScales   [sp.originalEntryIndex] = 1f;
            hpScales      [sp.originalEntryIndex] = 1f;
            dmgScales     [sp.originalEntryIndex] = 1f;
            intervalScales[sp.originalEntryIndex] = 1f;
        }

        float bestScore     = float.MaxValue;
        var   bestCountSc   = new Dictionary<int, float>(countScales);
        var   bestHpSc      = new Dictionary<int, float>(hpScales);
        var   bestDmgSc     = new Dictionary<int, float>(dmgScales);
        var   bestItvSc     = new Dictionary<int, float>(intervalScales);
        BatchSimResult lastBatch = null;

        for (int iter = 0; iter < targets.maxIterations; iter++)
        {
            float progress = (float)iter / targets.maxIterations;
            string msg = $"반복 {iter + 1}/{targets.maxIterations} ...";
            onProgress?.Invoke(progress, msg);

            // 현재 배율로 새 config 생성
            var config = BuildAdjustedConfig(baseConfig, countScales, hpScales, dmgScales, intervalScales);

            // N회 시뮬레이션
            var runs = RunBatch(config, targets.runsPerIteration, iter);
            lastBatch = WaveSimAnalyzer.Aggregate(runs);

            float score = ComputeScore(lastBatch, targets);
            if (score < bestScore)
            {
                bestScore   = score;
                bestCountSc = new Dictionary<int, float>(countScales);
                bestHpSc    = new Dictionary<int, float>(hpScales);
                bestDmgSc   = new Dictionary<int, float>(dmgScales);
                bestItvSc   = new Dictionary<int, float>(intervalScales);
                result.finalWinRate         = lastBatch.WinRate;
                result.finalAvgEndTime      = lastBatch.avgEndTime;
                result.finalAvgTemperature  = lastBatch.avgFinalTemperature;
            }

            // 수렴 판정
            if (IsWithinTargets(lastBatch, targets))
            {
                result.converged = true;
                break;
            }

            // 배율 조정
            AdjustScales(
                lastBatch, targets,
                countScales, hpScales, dmgScales, intervalScales,
                targets, iter);
        }

        result.iterationsUsed = Math.Min(targets.maxIterations,
            result.converged ? result.iterationsUsed + 1 : targets.maxIterations);

        // ── 최종 조정 결과 생성 ─────────────────────────────────
        result.adjustments = BuildAdjustments(
            waveEntries, entryTemplates,
            bestCountSc, bestHpSc, bestDmgSc, bestItvSc);

        onProgress?.Invoke(1f, result.converged ? "수렴 완료" : "최대 반복 도달");
        return result;
    }

    // ────────────────────────────────────────────────────────────
    //  내부 헬퍼
    // ────────────────────────────────────────────────────────────

    private static WaveSimConfig BuildAdjustedConfig(
        WaveSimConfig base_,
        Dictionary<int, float> countSc,
        Dictionary<int, float> hpSc,
        Dictionary<int, float> dmgSc,
        Dictionary<int, float> intervalSc)
    {
        // 기본 필드 복사
        var cfg = new WaveSimConfig
        {
            gameDuration         = base_.gameDuration,
            tickDuration         = base_.tickDuration,
            startTemperature     = base_.startTemperature,
            temperaturePerBreach = base_.temperaturePerBreach,
            defeatTemperature    = base_.defeatTemperature,
            costIncomePerSecond  = base_.costIncomePerSecond,
            initialCost          = base_.initialCost,
            battlefieldLength    = base_.battlefieldLength,
            playerSpawnX         = base_.playerSpawnX,
            enemySpawnX          = base_.enemySpawnX,
            playerBaseX          = base_.playerBaseX,
            enemyBaseX           = base_.enemyBaseX,
            playerTemplates      = base_.playerTemplates,
            seed                 = base_.seed,
        };

        // 스폰 스케줄을 배율에 맞게 재생성
        cfg.spawnSchedule = new List<WaveSpawnScheduleEntry>();

        // 엔트리 인덱스별로 그룹핑 후 count 배율 적용
        var grouped = new Dictionary<int, List<WaveSpawnScheduleEntry>>();
        foreach (var sp in base_.spawnSchedule)
        {
            if (!grouped.TryGetValue(sp.originalEntryIndex, out var list))
            {
                list = new List<WaveSpawnScheduleEntry>();
                grouped[sp.originalEntryIndex] = list;
            }
            list.Add(sp);
        }

        foreach (var kv in grouped)
        {
            int idx         = kv.Key;
            var origList    = kv.Value;

            float cScale = countSc   .TryGetValue(idx, out float cs) ? cs : 1f;
            float hScale = hpSc      .TryGetValue(idx, out float hs) ? hs : 1f;
            float dScale = dmgSc     .TryGetValue(idx, out float ds) ? ds : 1f;
            float iScale = intervalSc.TryGetValue(idx, out float is_) ? is_ : 1f;

            // 수량 배율: origList.Count × cScale → 반올림
            int newCount = Mathf.Max(1, Mathf.RoundToInt(origList.Count * cScale));

            // 원본 첫 항목에서 템플릿 복사 후 HP/데미지 조정
            var origTmpl = origList[0].template;
            var adjTmpl = origTmpl.Clone();
            adjTmpl.maxHp  = Mathf.Max(1f, origTmpl.maxHp  * hScale);
            adjTmpl.damage = Mathf.Max(1f, origTmpl.damage * dScale);

            // 소환 간격: 원본 간격 × iScale (작을수록 빠르게 = 어려워짐)
            float startTime = origList[0].exactSpawnTime;
            float interval  = origList.Count > 1
                ? (origList[origList.Count - 1].exactSpawnTime - startTime) / (origList.Count - 1)
                : 0.5f;
            interval = Mathf.Max(0.05f, interval * iScale);

            for (int k = 0; k < newCount; k++)
            {
                cfg.spawnSchedule.Add(new WaveSpawnScheduleEntry
                {
                    exactSpawnTime     = startTime + k * interval,
                    template           = adjTmpl,
                    isBoss             = origList[0].isBoss,
                    originalEntryIndex = idx,
                });
            }
        }

        cfg.spawnSchedule.Sort((a, b) => a.exactSpawnTime.CompareTo(b.exactSpawnTime));
        return cfg;
    }

    private static List<SimRunResult> RunBatch(WaveSimConfig config, int runs, int seedOffset)
    {
        var results = new List<SimRunResult>(runs);
        for (int i = 0; i < runs; i++)
            results.Add(WaveSimEngine.Run(config, config.seed + seedOffset * 1000 + i));
        return results;
    }

    /// <summary>목표 대비 오차 점수 (낮을수록 좋음)</summary>
    private static float ComputeScore(BatchSimResult batch, OptimizationTargets t)
    {
        float winMid  = (t.winRateLow  + t.winRateHigh)  / 2f;
        float timeMid = (t.endTimeLow  + t.endTimeHigh)  / 2f;
        float tempMid = (t.tempLow     + t.tempHigh)     / 2f;

        float winErr  = Mathf.Abs(batch.WinRate               - winMid)  / winMid;
        float timeErr = Mathf.Abs(batch.avgEndTime            - timeMid) / timeMid;
        float tempErr = Mathf.Abs(batch.avgFinalTemperature   - tempMid) / tempMid;

        return winErr * 2f + timeErr + tempErr * 0.5f;
    }

    private static bool IsWithinTargets(BatchSimResult batch, OptimizationTargets t)
    {
        return batch.WinRate >= t.winRateLow && batch.WinRate <= t.winRateHigh
            && batch.avgEndTime >= t.endTimeLow && batch.avgEndTime <= t.endTimeHigh
            && batch.avgFinalTemperature >= t.tempLow && batch.avgFinalTemperature <= t.tempHigh;
    }

    private static void AdjustScales(
        BatchSimResult batch,
        OptimizationTargets targets,
        Dictionary<int, float> countSc,
        Dictionary<int, float> hpSc,
        Dictionary<int, float> dmgSc,
        Dictionary<int, float> intervalSc,
        OptimizationTargets t,
        int iter)
    {
        float win    = batch.WinRate;
        float midWin = (t.winRateLow + t.winRateHigh) / 2f;
        float delta  = win - midWin;  // + = 너무 쉬움 → 적 강화, - = 너무 어려움 → 적 약화

        float lr         = Mathf.Lerp(0.15f, 0.04f, (float)iter / Mathf.Max(t.maxIterations - 1, 1));
        float adjustment = delta * lr;

        var keys = new List<int>(countSc.Keys);
        foreach (int idx in keys)
        {
            if (t.adjustCount)
            {
                countSc[idx] = Mathf.Clamp(countSc[idx] + adjustment, t.minScaleFactor, t.maxScaleFactor);
            }
            if (t.adjustHp)
            {
                hpSc[idx] = Mathf.Clamp(hpSc[idx] + adjustment * 0.6f, t.minScaleFactor, t.maxScaleFactor);
            }
            if (t.adjustDamage)
            {
                dmgSc[idx] = Mathf.Clamp(dmgSc[idx] + adjustment * 0.3f, t.minScaleFactor, t.maxScaleFactor);
            }
            if (t.adjustInterval)
            {
                // 간격: 너무 쉬우면 줄이고(빠르게), 너무 어려우면 늘림(느리게) → delta 반대 방향
                intervalSc[idx] = Mathf.Clamp(intervalSc[idx] - adjustment * 0.4f, 0.3f, t.maxScaleFactor);
            }
        }
    }

    private static List<EntryAdjustment> BuildAdjustments(
        List<WaveEnemyEntry>             waveEntries,
        Dictionary<int, SimUnitTemplate> entryTemplates,
        Dictionary<int, float> countSc,
        Dictionary<int, float> hpSc,
        Dictionary<int, float> dmgSc,
        Dictionary<int, float> intervalSc)
    {
        var list = new List<EntryAdjustment>();

        foreach (var kv in entryTemplates)
        {
            int idx  = kv.Key;
            var tmpl = kv.Value;

            WaveEnemyEntry origEntry = (idx >= 0 && idx < waveEntries.Count)
                ? waveEntries[idx] : null;

            float cSc = countSc   .TryGetValue(idx, out float c)  ? c  : 1f;
            float hSc = hpSc      .TryGetValue(idx, out float h)  ? h  : 1f;
            float dSc = dmgSc     .TryGetValue(idx, out float d)  ? d  : 1f;
            float iSc = intervalSc.TryGetValue(idx, out float iv) ? iv : 1f;

            int   origCount    = origEntry?.count          ?? 1;
            float origHp       = tmpl.maxHp;
            float origDamage   = tmpl.damage;
            float origInterval = origEntry?.spawnInterval  ?? 1f;

            list.Add(new EntryAdjustment
            {
                originalEntryIndex  = idx,
                waveSettingIndex    = origEntry?.waveSettingIndex ?? -1,
                enemyName           = tmpl.displayName,
                spawnTime           = origEntry?.spawnTime        ?? 0f,
                originalCount       = origCount,
                recommendedCount    = Mathf.Max(1, Mathf.RoundToInt(origCount * cSc)),
                originalHp          = origHp,
                recommendedHp       = Mathf.Max(1f, Mathf.Round(origHp  * hSc)),
                originalDamage      = origDamage,
                recommendedDamage   = Mathf.Max(1f, Mathf.Round(origDamage * dSc)),
                originalInterval    = origInterval,
                recommendedInterval = Mathf.Max(0.05f, origInterval * iSc),
            });
        }

        list.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        return list;
    }
}
#endif
