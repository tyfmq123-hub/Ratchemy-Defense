using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════════
//  SimRunResult  —  단일 시뮬레이션 실행 결과
// ═══════════════════════════════════════════════════════════════════
public class SimRunResult
{
    public bool  playerWon;
    public float endTime;
    public float finalTemperature;
    public int   totalBreaches;

    public SegmentData[]                    perSegmentData; // 길이 6 (30초 단위)
    public Dictionary<string, UnitRunStats> unitStats;      // templateId → 통계
}

// ═══════════════════════════════════════════════════════════════════
//  SegmentData  —  30초 구간 단위 데이터 (단일 런)
// ═══════════════════════════════════════════════════════════════════
public class SegmentData
{
    public int   segmentIndex;      // 0~5
    public float temperatureGain;
    public int   breachCount;
    public int   playerSpawns;
    public int   enemySpawns;
    public int   playerDeaths;
    public int   enemyDeaths;

    public int StartSec => segmentIndex * 30;
    public int EndSec   => (segmentIndex + 1) * 30;
}

// ═══════════════════════════════════════════════════════════════════
//  UnitRunStats  —  단일 런 내 특정 유닛 유형의 통계
// ═══════════════════════════════════════════════════════════════════
public class UnitRunStats
{
    public string templateId;
    public string name;
    public bool   isPlayer;

    public float totalDamageDealt;
    public float totalDamageAbsorbed;
    public int   totalKills;
    public int   spawnCount;
    public float totalSurvivalTime;
    public int   deathCount;
}

// ═══════════════════════════════════════════════════════════════════
//  BatchSimResult  —  N회 시뮬레이션 배치 집계 결과
// ═══════════════════════════════════════════════════════════════════
public class BatchSimResult
{
    public int totalRuns;
    public int playerWins;

    public float WinRate  => totalRuns > 0 ? (float)playerWins / totalRuns : 0f;
    public float LossRate => 1f - WinRate;

    public float avgEndTime;
    public float avgFinalTemperature;
    public float avgBreaches;

    public BatchSegmentStats[]                  segmentStats;           // 길이 6
    public Dictionary<string, BatchUnitStats>   unitStats = new();

    public List<DifficultyIssue>        difficultyIssues    = new();
    public List<BalanceRecommendation>  recommendations     = new();
}

// ═══════════════════════════════════════════════════════════════════
//  BatchSegmentStats  —  30초 구간 배치 집계
// ═══════════════════════════════════════════════════════════════════
public class BatchSegmentStats
{
    public int segmentIndex;

    public float avgTemperatureGain;
    public float avgBreachCount;
    public float avgPlayerSpawns;
    public float avgEnemySpawns;
    public float avgPlayerDeaths;
    public float avgEnemyDeaths;

    /// <summary>이 구간 시작 시 게임이 아직 진행 중이었던 런 비율</summary>
    public float survivorRate;

    public int StartSec => segmentIndex * 30;
    public int EndSec   => (segmentIndex + 1) * 30;
}

// ═══════════════════════════════════════════════════════════════════
//  BatchUnitStats  —  배치 기준 유닛 통계
// ═══════════════════════════════════════════════════════════════════
public class BatchUnitStats
{
    public string templateId;
    public string name;
    public bool   isPlayer;

    public float avgDamageDealt;
    public float avgDamageAbsorbed;
    public float avgKills;
    public float avgSpawnCount;
    public float avgSurvivalTime;
    public float avgDeathCount;

    /// <summary>전체 런 중 이 유닛이 1번 이상 등장한 비율</summary>
    public float usageRate;
    /// <summary>DPS / Cost 기준 코스트 효율 (플레이어 유닛 전용)</summary>
    public float costEfficiency;
}

// ═══════════════════════════════════════════════════════════════════
//  DifficultyIssue  —  자동 난이도 문제 탐지 결과
// ═══════════════════════════════════════════════════════════════════
public enum DifficultyIssueType
{
    TooEasy,        // 전체 승률 > 85%
    TooHard,        // 전체 승률 < 40%
    SpikeHard,      // 인접 구간 대비 갑작스러운 난이도 급상승
    EmptyPeriod,    // 적 소환 없는 공백 구간
    TempSpike,      // 구간 온도 급상승
    LowPressure,    // 구간 온도 기여 너무 낮음 (난이도 공백)
}

public class DifficultyIssue
{
    public DifficultyIssueType type;
    public int    segmentIndex; // -1 = 전체
    public string description;
    public float  severity;    // 0~1 (높을수록 심각)
}

// ═══════════════════════════════════════════════════════════════════
//  BalanceRecommendation  —  자동 밸런스 추천 항목
// ═══════════════════════════════════════════════════════════════════
public enum RecommendationTarget
{
    EnemyCount,
    EnemyHp,
    EnemyDamage,
    EnemySpawnTime,
    BossHp,
    BossDamage,
    BossSpawnTime,
    CostIncome,
}

public class BalanceRecommendation
{
    public RecommendationTarget target;
    public string entryKey;             // 식별자 (enemyName@spawnTime)
    public string description;

    public float currentValue;
    public float recommendedValue;
    public float expectedWinRateDelta;
    public float expectedTemperatureDelta;

    public float ChangePercent => currentValue > 0f
        ? (recommendedValue - currentValue) / currentValue * 100f
        : 0f;
}

// ═══════════════════════════════════════════════════════════════════
//  OptimizationResult  —  목표 기반 자동 최적화 결과
// ═══════════════════════════════════════════════════════════════════
public class OptimizationResult
{
    public bool  converged;
    public int   iterationsUsed;

    public float finalWinRate;
    public float finalAvgEndTime;
    public float finalAvgTemperature;

    // 조정된 적 수치 목록 (originalEntryIndex → 배율)
    public List<EntryAdjustment> adjustments = new();
}

public class EntryAdjustment
{
    public int    originalEntryIndex;
    public int    waveSettingIndex;   // WaveManager.waveSettings 배열 인덱스
    public string enemyName;
    public float  spawnTime;

    public int   originalCount;
    public int   recommendedCount;

    public float originalHp;
    public float recommendedHp;

    public float originalDamage;
    public float recommendedDamage;

    public float originalInterval;
    public float recommendedInterval;
}
