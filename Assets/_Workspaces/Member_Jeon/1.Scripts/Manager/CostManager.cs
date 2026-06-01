using UnityEngine;

// 유닛 카드 등에 쓰는 코스트(자원) 관리
// - 씬에 1개만 두고 UnitCard 등에서 CostManager.Instance로 접근
// - 일정 시간마다 코스트가 자동으로 증가 (최대 maxCost까지)
public class CostManager : MonoBehaviour
{
    public static CostManager Instance { get; private set; }

    [Header("코스트 설정")]
    [SerializeField] private int currentCost = 0;  // 지금 사용 가능한 코스트
    [SerializeField] private int maxCost = 10;       // 코스트 상한 (이 이상은 안 올라감)
    [SerializeField] private float addInterval = 15f; // 자동 증가 주기(초)
    [SerializeField] private int addAmount = 1;        // 한 번에 올라가는 양

    private float timer;                      // addInterval까지 경과 시간 누적
    private int lastLoggedProgressSecond;     // 이번 회복 주기에서 마지막으로 찍은 초 (1/15, 2/15 …)

    public int CurrentCost => currentCost;
    public int MaxCost => maxCost;

    private void Awake()
    {
        // 싱글톤: 씬에 CostManager가 2개면 나중에 생긴 쪽 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // UI 대신 콘솔로 회복까지 남은 시간 확인 (1/15, 2/15 …)
        int totalSeconds = Mathf.Max(1, Mathf.CeilToInt(addInterval));
        int progressSecond = Mathf.Min(totalSeconds, Mathf.FloorToInt(timer) + 1);

        if (progressSecond > lastLoggedProgressSecond)
        {
            lastLoggedProgressSecond = progressSecond;
            Debug.Log($"[CostManager] 회복 진행 {progressSecond}/{totalSeconds} | 보유 코스트 {currentCost}/{maxCost}");
        }

        // addInterval초마다 addAmount만큼 코스트 회복
        if (timer >= addInterval)
        {
            AddCost(addAmount, fromAutoRecover: true);
            timer = 0f;
            lastLoggedProgressSecond = 0;
        }
    }

    // 카드 사용 전에 "낼 수 있는지"만 확인 (차감 안 함)
    public bool CanUseCost(int cost)
    {
        return currentCost >= cost;
    }

    // 코스트를 실제로 차감. 부족하면 false
    public bool UseCost(int cost)
    {
        if (currentCost < cost)
        {
            Debug.Log("코스트가 부족합니다.");
            return false;
        }

        currentCost -= cost;
        Debug.Log($"코스트 사용: {cost}, 현재 코스트: {currentCost}");
        return true;
    }

    // 코스트 증가 (자동 회복·보상 등). 0 ~ maxCost 사이로 제한
    public void AddCost(int amount)
    {
        AddCost(amount, fromAutoRecover: false);
    }

    private void AddCost(int amount, bool fromAutoRecover)
    {
        int before = currentCost;
        currentCost += amount;
        currentCost = Mathf.Clamp(currentCost, 0, maxCost);

        if (fromAutoRecover)
            Debug.Log($"[CostManager] 코스트 +{amount} 회복 완료 ({before} → {currentCost}) | 상한 {maxCost}");
        else
            Debug.Log($"[CostManager] 코스트 +{amount} ({before} → {currentCost}) | 상한 {maxCost}");
    }
}
