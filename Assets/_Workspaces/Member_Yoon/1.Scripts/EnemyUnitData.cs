using UnityEngine;

[CreateAssetMenu(fileName = "EnemyUnitData", menuName = "EnemyUnit")]
public class EnemyUnitData : ScriptableObject
{
    [Header("전투 스탯")]
    public int damage;
    public int hp;
    public int maxHp;

    [Header("이동 및 공격")]
    public float attackRange;
    public float attackSpeed;
    public float moveSpeed;
}
