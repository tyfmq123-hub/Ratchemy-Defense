using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 화면의 카드 UI 한 장
// UnitCardData(.asset)로 아이콘·코스트 표시, 클릭 시 유닛 소환
public class UnitCard : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private UnitCardData cardData; // Inspector: Unit Card Data 에셋 드래그

    [Header("UI")]
    [SerializeField] private Image iconImage;       // 카드 아이콘
    [SerializeField] private TMP_Text costText;     // 코스트 숫자
    [SerializeField] private Button button;         // 카드 클릭

    [Header("소환 위치")]
    [SerializeField] private Transform spawnPoint;  // 유닛 생성 위치 (빈 오브젝트 Transform 권장)

    private void Start()
    {
        iconImage.sprite = cardData.cardImage;
        costText.text = cardData.cost.ToString();

        button.onClick.AddListener(UseCard);
    }

    private void UseCard()
    {
        // TODO: CostManager 연동 후 구현 예시
        // if (CostManager.Instance.UseCost(cardData.cost) == false)
        // {
        //     Debug.Log("코스트가 부족합니다.");
        //     return;
        // }
        // Instantiate(cardData.unitPrefab, spawnPoint.position, Quaternion.identity);
    }
}
