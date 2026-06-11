using UnityEngine;

// 카드 한 장 분량 (배열의 요소 하나)
[System.Serializable]
public class UnitCardEntry
{
    [Header("카드 표시 정보")]
    public string cardName;            // 카드 이름
    public int cost;                   // 사용 시 필요한 코스트

    [Header("소환 (카드마다 다른 프리팹)")]
    public PlayerUnitBase unitPrefab;  // 이 카드 버튼으로 소환할 유닛 프리팹 (2.Prefabs/player 쪽)

    [Header("소환 사운드")]
    public AudioClip spawnSound;
    [Range(0f, 3f)] public float spawnSoundVolume = 1.5f;

    [Header("코스트 환급")]
    [Range(0f, 1f)] public float deathRefundRatio = 0.5f; // 유닛 사망 시 소환 코스트의 몇 %를 반환할지
}

// 카드 5장 데이터를 한 에셋에 모아 두는 ScriptableObject
// cards[0]~[4]: 각 소환 버튼에 맞는 프리팹·이름·코스트 (UnitCard의 cardIndex와 짝)
// 소환 위치 A/B 교대는 UnitCardSpawner가 담당 (여기에는 프리팹만 넣음)
[CreateAssetMenu(menuName = "Game/Unit Card Data")]
public class UnitCardData : ScriptableObject
{
    public UnitCardEntry[] cards;
}
