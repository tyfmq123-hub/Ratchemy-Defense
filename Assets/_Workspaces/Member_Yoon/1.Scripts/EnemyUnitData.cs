using UnityEngine;

[CreateAssetMenu(fileName = "EnemyUnitData", menuName = "EnemyUnit")]
public class EnemyUnitData : ScriptableObject, IUnitDisplayData
{
    [Header("표시 정보")]
    public string unitName;
    public Sprite unitSprite;
    public Color unitSpriteColor = Color.white;
    public Vector2 unitSpriteSize = new Vector2(100f, 100f);
    public string roleType;
    public string elementType;
    [TextArea] public string description;

    [Header("전투 스탯")]
    public int damage;
    public int hp;
    public int maxHp;

    [Header("이동 및 공격")]
    public float attackRange;
    public float attackSpeed;
    public float moveSpeed;

    public int Number => 0;
    public int Cost => 0;
    public string UnitName => unitName;
    public Sprite UnitSprite => unitSprite;
    public Color UnitSpriteColor => unitSpriteColor;
    public Vector2 UnitSpriteSize => unitSpriteSize;
    public string RoleType => roleType;
    public string ElementType => elementType;
    public string Description => description;
    public int Damage => damage;
    public int MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public float AttackRange => attackRange;
}
