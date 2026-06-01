using System.Collections;
using UnityEngine;

// 화염슬라임 2단계 - 1단계 능력 유지 + 화염 오라 범위 데미지
// 슬라임 주변에 오라가 항상 활성화되며 auraInterval(기본 3초)마다 범위 내 플레이어에게 데미지
// AuraEffect 자식 오브젝트의 Animator에 "AuraPulse" 트리거로 데미지와 애니메이션 동기화
// Inspector에서 enemyUnitData 슬롯에 FlameSlimeTier2Data 에셋을 연결해야 합니다.
// [범위 시각화] 에디터/개발 빌드에서만 표시 (릴리즈 빌드에서 자동 제거)
public class FlameSlimeTier2 : FlameSlime
{
    protected FlameSlimeTier2Data flameData2;
    private Animator auraAnimator;
    protected Transform auraEffectTransform;

    // AuraEffect lossyScale.x 기반 실제 공격 반지름
    protected float runtimeAuraRadius;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("디버그 범위 시각화 (에디터/개발 빌드 전용)")]
    [SerializeField] protected bool showDebugCircles = false;
    [SerializeField] private Color rangeCircleColor = new Color(1f, 0.4f, 0f, 0.6f);
    [SerializeField] private float rangeCircleWidth = 0.05f;

    private LineRenderer rangeCircle;
#endif

    protected override void Start()
    {
        base.Start();

        flameData2 = enemyUnitData as FlameSlimeTier2Data;
        if (flameData2 == null)
            Debug.LogError($"[FlameSlimeTier2] enemyUnitData에 FlameSlimeTier2Data를 연결해주세요. ({gameObject.name})");

        runtimeAuraRadius = flameData2 != null ? flameData2.auraRadius : 0f;

        auraEffectTransform = transform.Find("AuraEffect");
        if (auraEffectTransform != null)
            auraAnimator = auraEffectTransform.GetComponent<Animator>();
        else
            Debug.LogWarning("[FlameSlimeTier2] 자식 오브젝트 'AuraEffect'를 찾을 수 없습니다.");

        StartCoroutine(InitRangeFromAuraEffect());
        StartCoroutine(AuraLoop());
    }

    private IEnumerator InitRangeFromAuraEffect()
    {
        yield return null;

        if (auraEffectTransform != null)
        {
            runtimeAuraRadius = auraEffectTransform.lossyScale.x;
            Debug.Log($"[FlameSlimeTier2] AuraEffect 반지름 설정: {runtimeAuraRadius:F3}");
        }
        else
        {
            Debug.LogWarning("[FlameSlimeTier2] AuraEffect를 찾지 못해 SO의 auraRadius를 사용합니다.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showDebugCircles)
            rangeCircle = CreateDebugCircle("AuraRangeCircle", runtimeAuraRadius, rangeCircleColor, rangeCircleWidth);
#endif
    }

    // 에디터/개발 빌드 전용 - 자식 오브젝트로 LineRenderer 원을 생성해 반환
    protected LineRenderer CreateDebugCircle(string objName, float radius, Color color, float width = 0.05f, int segments = 48)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 10;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
        return lr;
#else
        return null;
#endif
    }

    private IEnumerator AuraLoop()
    {
        Debug.Log($"[FlameSlimeTier2] 오라 루프 시작 - 간격: {flameData2.auraInterval}초");
        while (!IsDead())
        {
            yield return new WaitForSeconds(flameData2.auraInterval);

            if (!IsDead())
                DealAuraDamage();
        }
        Debug.Log("[FlameSlimeTier2] 오라 루프 종료 (사망)");
    }

    private void DealAuraDamage()
    {
        if (auraAnimator != null)
            auraAnimator.SetTrigger("AuraPulse");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (rangeCircle != null)
            StartCoroutine(PulseRangeCircle());
#endif

        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, runtimeAuraRadius, targetLayer);
        Debug.Log($"[FlameSlimeTier2] 오라 틱 - 범위 {runtimeAuraRadius:F3} 내 감지된 콜라이더 수: {targets.Length}");

        int hitCount = 0;
        foreach (Collider2D col in targets)
        {
            PlayerUnitBase player = col.GetComponent<PlayerUnitBase>();
            if (player != null)
            {
                player.TakeDamage(flameData2.auraDamage);
                hitCount++;
                Debug.Log($"[FlameSlimeTier2] 오라 데미지 {flameData2.auraDamage} → {col.gameObject.name} (HP: {player.CurrentHp})");
            }
        }

        if (hitCount == 0)
            Debug.Log("[FlameSlimeTier2] 오라 범위 내 플레이어 없음");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private IEnumerator PulseRangeCircle()
    {
        rangeCircle.startColor = Color.white;
        rangeCircle.endColor = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (rangeCircle != null)
        {
            rangeCircle.startColor = rangeCircleColor;
            rangeCircle.endColor = rangeCircleColor;
        }
    }
#endif

    private void OnDrawGizmosSelected()
    {
        if (flameData2 == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, runtimeAuraRadius > 0f ? runtimeAuraRadius : flameData2.auraRadius);
    }
}
