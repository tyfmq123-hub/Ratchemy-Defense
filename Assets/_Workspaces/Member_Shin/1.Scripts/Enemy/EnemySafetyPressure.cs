using UnityEngine;

/// <summary>
/// 일반 몬스터 하나가 전장에 주는 압박 수치를 관리합니다.
///
/// 이 스크립트를 적 프리팹에 붙이면,
/// 적이 생성될 때 SafetyScoreManager에 자동 등록됩니다.
///
/// 적이 사망하거나 기지에 도착하여 사라질 때는
/// 자동으로 등록이 해제됩니다.
/// </summary>
public class EnemySafetyPressure : MonoBehaviour
{
    [Header("안전관리 점수 압박 수치")]
    [Tooltip("이 몬스터가 전장에 남아 있을 때 적용하는 압박 수치입니다.")]
    public float pressureValue = 1f;

    // 안전관리 점수를 계산하는 매니저입니다.
    private SafetyScoreManager safetyScoreManager;

    private void Awake()
    {
        // Inspector 연결 작업을 줄이기 위해
        // Scene 안의 SafetyScoreManager를 자동으로 찾습니다.
        safetyScoreManager =
            FindFirstObjectByType<SafetyScoreManager>();
    }

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때
        // 살아 있는 몬스터 목록에 등록합니다.
        //
        // 프리팹 생성 방식과 오브젝트 풀링 방식에서
        // 모두 사용할 수 있습니다.
        if (safetyScoreManager == null)
        {
            safetyScoreManager =
                FindFirstObjectByType<SafetyScoreManager>();
        }

        if (safetyScoreManager != null)
        {
            safetyScoreManager.RegisterEnemy(
                this
            );
        }
        else
        {
            Debug.LogError(
                "[EnemySafetyPressure] SafetyScoreManager를 찾지 못했습니다."
            );
        }
    }

    private void OnDisable()
    {
        // 적이 사망하거나 기지에 도착해서 비활성화되면
        // 살아 있는 몬스터 목록에서 제거합니다.
        if (safetyScoreManager != null)
        {
            safetyScoreManager.UnregisterEnemy(
                this
            );
        }
    }
}