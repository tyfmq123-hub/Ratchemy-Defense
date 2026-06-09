using UnityEngine;

[CreateAssetMenu(fileName = "PlayerUnitDisplayData", menuName = "Game/Player Unit Display Data")]
public class PlayerUnitDisplayData : ScriptableObject, IUnitDisplayData
{
    [Header("표시 정보")]
    public int number;
    public string unitName;
    public Sprite unitSprite;
    public Color unitSpriteColor = Color.white;
    public Vector2 unitSpriteSize = new Vector2(100f, 100f);
    public string roleType;
    public string elementType;
    [TextArea] public string description;
    [TextArea(3, 6)] public string popupDescription;

    [Header("스킬")]
    public SkillData[] skills;

    [Header("프리팹")]
    public PlayerUnitBase unitPrefab;

    public int Number => number;
    public int Cost => number;
    public string UnitName => unitName;
    public Sprite UnitSprite => unitSprite;
    public Color UnitSpriteColor => unitSpriteColor;
    public Vector2 UnitSpriteSize => unitSpriteSize;
    public string RoleType => roleType;
    public string ElementType => elementType;
    public string Description => description;
    public string PopupDescription => popupDescription;
    public SkillData[] Skills => skills;
    public int Damage => unitPrefab != null ? unitPrefab.AttackPower : 0;
    public int MaxHp => unitPrefab != null ? (int)unitPrefab.MaxHp : 0;
    public float MoveSpeed => unitPrefab != null ? unitPrefab.MoveSpeed : 0f;
    public float AttackSpeed => unitPrefab != null ? unitPrefab.AttackSpeed : 0f;
    public float AttackRange => unitPrefab != null ? unitPrefab.AttackRange : 0f;
}
