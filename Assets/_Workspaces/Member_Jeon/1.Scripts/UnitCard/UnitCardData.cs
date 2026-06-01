using UnityEngine;

// 카드 한 장의 데이터 (ScriptableObject 에셋)
// Project 창: Create → Game → Unit Card Data 로 .asset 생성 후 UnitCard에 연결
[CreateAssetMenu(menuName = "Game/Unit Card Data")]
public class UnitCardData : ScriptableObject
{
    [Header("카드 표시 정보")]
    public string cardName;            // 카드 이름
    public Sprite cardImage;           // 카드 아이콘 스프라이트
    public int cost;                   // 사용 시 필요한 코스트

    [Header("소환")]
    public PlayerUnitBase unitPrefab;  // 카드 사용 시 Instantiate 할 유닛 프리팹
}
