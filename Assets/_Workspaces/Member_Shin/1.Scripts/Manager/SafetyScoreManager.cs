using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 안전관리 점수의 실제 계산을 담당합니다.
///
/// 최종 점수는 아래 위험도를 합산하여 계산합니다.
///
/// 1. 보스 위험도
/// 2. 기지 온도 위험도
/// 3. 현재 살아 있는 일반 몬스터의 누적 압박
///
/// 몬스터가 생성되면 점수가 내려가고,
/// 몬스터가 사망하거나 기지에 도착해 사라지면 점수가 다시 올라갑니다.
/// </summary>
public class SafetyScoreManager : MonoBehaviour
{
    [Header("연결할 안전관리 점수 UI")]
    [Tooltip("SafetyScorePanel에 붙어 있는 SafetyScoreUI를 연결하세요.")]
    public SafetyScoreUI safetyScoreUI;

    [Header("배터리 온도 기준")]
    [Tooltip("이 온도까지는 온도 위험도 감점을 적용하지 않습니다.")]
    public float safeTemperature = 30f;

    [Tooltip("열폭주가 발생하는 최대 온도입니다.")]
    public float maxTemperature = 130f;

    [Header("온도 위험도")]
    [Tooltip("온도가 최대치에 도달했을 때 적용할 최대 감점입니다.")]
    public float maxTemperaturePenalty = 45f;

    [Header("일반 몬스터 누적 압박")]
    [Tooltip("이 수치까지는 정상적으로 처리 가능한 적 숫자로 판단합니다.")]
    public float safeEnemyPressure = 4f;

    [Tooltip("정상 범위를 초과한 압박 수치 1당 감소하는 점수입니다.")]
    public float penaltyPerPressure = 1.6f;

    [Tooltip("일반 몬스터 때문에 감소할 수 있는 최대 점수입니다.")]
    public float maxEnemyPenalty = 35f;

    [Header("보스 위험도")]
    [Tooltip("보스 체력이 가득 차 있을 때 적용할 최대 감점입니다.")]
    public float maxBossPenalty = 10f;

    // 현재 보스 체력입니다.
    private float currentBossHealth;

    // 보스 최대 체력입니다.
    // 0으로 나누는 문제를 막기 위해 기본값을 1로 둡니다.
    private float maxBossHealth = 1f;

    // 현재 배터리 온도입니다.
    private float currentTemperature;

    // 현재 살아 있는 일반 몬스터를 중복 없이 관리합니다.
    //
    // HashSet을 사용하면 같은 몬스터가 실수로
    // 두 번 등록되는 문제를 방지할 수 있습니다.
    private readonly HashSet<EnemySafetyPressure>
        aliveEnemies =
            new HashSet<EnemySafetyPressure>();

    private void Start()
    {
        // 게임 시작 상태를 기준으로
        // 안전관리 점수를 처음 계산합니다.
        RecalculateScore();
    }

    /// <summary>
    /// BaseHealth에서 배터리 온도가 변경될 때 호출합니다.
    /// </summary>
    public void SetTemperature(float newTemperature)
    {
        currentTemperature =
            Mathf.Clamp(
                newTemperature,
                0f,
                maxTemperature
            );

        RecalculateScore();
    }

    /// <summary>
    /// 일반 몬스터가 생성되거나 활성화될 때 호출합니다.
    /// </summary>
    public void RegisterEnemy(EnemySafetyPressure enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 새 몬스터일 때만 목록에 추가합니다.
        //
        // 이미 등록된 몬스터라면
        // HashSet.Add()가 false를 반환합니다.
        if (aliveEnemies.Add(enemy))
        {
            RecalculateScore();
        }
    }

    /// <summary>
    /// 일반 몬스터가 사망하거나 비활성화될 때 호출합니다.
    /// </summary>
    public void UnregisterEnemy(EnemySafetyPressure enemy)
    {
        if (enemy == null)
        {
            return;
        }

        // 실제로 등록되어 있던 몬스터만 제거합니다.
        if (aliveEnemies.Remove(enemy))
        {
            RecalculateScore();
        }
    }

    /// <summary>
    /// BossBase에서 보스 체력이 바뀔 때마다 호출합니다.
    ///
    /// 보스 체력이 높을수록 위험도가 높고,
    /// 보스 체력이 감소할수록 안전관리 점수가 회복됩니다.
    /// </summary>
    public void SetBossHealth(
        float newCurrentHealth,
        float newMaxHealth
    )
    {
        maxBossHealth =
            Mathf.Max(
                1f,
                newMaxHealth
            );

        currentBossHealth =
            Mathf.Clamp(
                newCurrentHealth,
                0f,
                maxBossHealth
            );

        RecalculateScore();
    }

    /// <summary>
    /// 현재 전장 상태를 기준으로
    /// 안전관리 점수를 다시 계산합니다.
    /// </summary>
    private void RecalculateScore()
    {
        if (safetyScoreUI == null)
        {
            Debug.LogError(
                "[SafetyScoreManager] SafetyScoreUI가 연결되지 않았습니다."
            );

            return;
        }

        float bossPenalty =
         CalculateBossPenalty();

        float temperaturePenalty =
            CalculateTemperaturePenalty();

        float enemyPenalty =
            CalculateEnemyPenalty();

        float calculatedScore =
            100f
            - bossPenalty
            - temperaturePenalty
            - enemyPenalty;

        int roundedScore =
            Mathf.RoundToInt(
                calculatedScore
            );

        safetyScoreUI.SetScore(
            roundedScore
        );

        // Console에서 계산 상태를 확인할 수 있습니다.
        Debug.Log(
            "[SafetyScoreManager] "
            + "점수: " + roundedScore
            + " / 온도 감점: " + temperaturePenalty.ToString("0.0")
            + " / 적 감점: " + enemyPenalty.ToString("0.0")
            + " / 살아 있는 적: " + aliveEnemies.Count
        );
    }

    /// <summary>
    /// 현재 보스 체력 비율을 기준으로
    /// 보스 위험도 감점을 계산합니다.
    /// </summary>
    private float CalculateBossPenalty()
    {
        // 보스 최대 체력이 정상적으로 전달되지 않았다면
        // 보스 감점을 적용하지 않습니다.
        if (maxBossHealth <= 0f)
        {
            return 0f;
        }

        // 보스 체력 비율을 0 ~ 1 사이 값으로 계산합니다.
        //
        // 예:
        // 50,000 / 50,000 → 1.0
        // 25,000 / 50,000 → 0.5
        float bossHealthRatio =
            currentBossHealth
            / maxBossHealth;

        // 보스 체력이 가득 차 있으면 10점 감점,
        // 절반이면 5점 감점,
        // 사망하면 감점하지 않습니다.
        return
            maxBossPenalty
            * bossHealthRatio;
    }

    /// <summary>
    /// 현재 배터리 온도를 기준으로
    /// 온도 위험도 감점을 계산합니다.
    /// </summary>
    private float CalculateTemperaturePenalty()
    {
        if (currentTemperature <= safeTemperature)
        {
            return 0f;
        }

        float temperatureRatio =
            Mathf.InverseLerp(
                safeTemperature,
                maxTemperature,
                currentTemperature
            );

        float curvedRatio =
            Mathf.Pow(
                temperatureRatio,
                1.5f
            );

        return
            maxTemperaturePenalty
            * curvedRatio;
    }

    /// <summary>
    /// 현재 살아 있는 일반 몬스터의 압박 수치를 합산하고
    /// 일반 몬스터 위험도 감점을 계산합니다.
    /// </summary>
    private float CalculateEnemyPenalty()
    {
        float totalPressure = 0f;

        foreach (EnemySafetyPressure enemy in aliveEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            totalPressure +=
                enemy.pressureValue;
        }

        // 일정 수치까지는 정상적으로 처리 가능한 범위이므로
        // 점수를 감소시키지 않습니다.
        float dangerousPressure =
            Mathf.Max(
                0f,
                totalPressure - safeEnemyPressure
            );

        float penalty =
            dangerousPressure
            * penaltyPerPressure;

        return
            Mathf.Min(
                penalty,
                maxEnemyPenalty
            );
    }
}