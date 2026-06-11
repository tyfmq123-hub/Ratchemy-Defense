using UnityEngine;

public interface IUnitDisplayData
{
    int Number { get; }
    int Cost { get; }
    string UnitName { get; }
    Sprite UnitSprite { get; }
    Color UnitSpriteColor { get; }
    Vector2 UnitSpriteSize { get; }
    string RoleType { get; }
    string ElementType { get; }
    string Description { get; }
    string PopupDescription { get; }
    SkillData[] Skills { get; }
    int Damage { get; }
    int MaxHp { get; }
    float MoveSpeed { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
}
