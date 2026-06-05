using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 유닛 소환 버튼 UI 1개 (총 5개를 씬에 두고 cardIndex만 0~4로 다르게 설정)
// - 카드 정보·프리팹·코스트: UnitCardData.cards[cardIndex]
// - 소환 위치 A/B 교대: UnitCardSpawner (씬에 1개)
// - 코스트 차감: CostManager (팀원 스크립트 추가 후 연동)
public class UnitCard : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private UnitCardData cardDatabase;   // 5장 데이터가 들어 있는 에셋
    [SerializeField] private int cardIndex;               // 이 버튼이 몇 번째 카드인지 (0~4)

    [Header("소환")]
    [SerializeField] private UnitCardSpawner cardSpawner; // 비우면 씬에서 자동 검색

    // TODO: CostManager 연동 후 해제
    [Header("코스트")]
    [SerializeField] private CostManager costManager;

    [Header("UI")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;               // 이 카드의 소환 버튼

    private void Awake()
    {
        if (cardSpawner == null)
            cardSpawner = FindAnyObjectByType<UnitCardSpawner>();

        if (costManager == null)
            costManager = FindAnyObjectByType<CostManager>();
    }

    private void Start()
    {
        UnitCardEntry entry = GetEntry();
        if (entry == null)
            return;

        if (costText != null)
            costText.text = GetRequiredCost().ToString();

        if (button != null)
            button.onClick.AddListener(UseCard);
        else
            Debug.LogWarning($"[UnitCard] {name}: button이 연결되지 않았습니다.");
    }

    private UnitCardEntry GetEntry()
    {
        if (cardDatabase == null || cardDatabase.cards == null)
        {
            Debug.LogWarning($"[UnitCard] {name}: cardDatabase가 비어 있습니다.");
            return null;
        }

        if (cardIndex < 0 || cardIndex >= cardDatabase.cards.Length)
        {
            Debug.LogWarning($"[UnitCard] {name}: cardIndex {cardIndex}가 범위를 벗어났습니다. (0 ~ {cardDatabase.cards.Length - 1})");
            return null;
        }

        return cardDatabase.cards[cardIndex];
    }

    // UnitCardData에 설정한 이 카드의 사용 코스트
    private int GetRequiredCost()
    {
        UnitCardEntry entry = GetEntry();
        return entry != null ? entry.cost : 0;
    }

    private void UseCard()
    {
        UnitCardEntry entry = GetEntry();
        if (entry == null)
            return;

        if (entry.unitPrefab == null)
        {
            Debug.LogWarning($"[UnitCard] {name}: cards[{cardIndex}].unitPrefab이 비어 있습니다. UnitCardData에서 프리팹을 넣어 주세요.");
            return;
        }

        if (cardSpawner == null)
        {
            Debug.LogWarning($"[UnitCard] {name}: UnitCardSpawner를 찾을 수 없습니다. 씬에 1개 추가하고 A/B 스폰 포인트를 연결하세요.");
            return;
        }

        int requiredCost = GetRequiredCost();

        // TODO: CostManager 연동 후 해제 — requiredCost는 UnitCardData.cards[cardIndex].cost
        if (costManager == null)
        {
            Debug.LogWarning($"[UnitCard] {name}: CostManager를 찾을 수 없습니다.");
            return;
        }
        
        if (!costManager.TrySpendCost(requiredCost))
            return;

        cardSpawner.SpawnUnit(entry.unitPrefab, requiredCost, entry.deathRefundRatio);
    }
}
