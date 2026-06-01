using UnityEngine;

// 아군 유닛 공통 베이스
// - maxHp / currentHp: Inspector에서 직접 수정 가능 (Play 시 코드가 덮어쓰지 않음)
// - InsulatorRat 등 자식 클래스는 Move·Attack 등을 override
public class PlayerUnitBase : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] protected float maxHp = 100f;    // 최대 체력
    [SerializeField] protected float currentHp;       // 현재 체력 (Inspector에서 시작 체력 설정 가능)

    [Header("전투·이동")]
    [SerializeField] protected int attackPower = 10;  // 한 번에 주는 데미지 (EnemyUnit.TakeDamage와 동일 int)
    [SerializeField] protected float moveSpeed = 2f;       // 초당 이동 거리
    [SerializeField] protected float attackSpeed = 1f;     // 초당 공격 횟수 (쿨다운 = 1 / attackSpeed)
    [SerializeField] protected float attackRange = 1.5f;   // 공격·감지 반경

    // 다른 스크립트에서 읽기 전용으로 접근
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public int AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public float AttackRange => attackRange;

    // Inspector에서 숫자 바꿀 때마다 호출 → currentHp가 maxHp를 넘지 않게 맞춤
    protected virtual void OnValidate()
    {
        if (maxHp < 1f)
            maxHp = 1f;

        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        // 에디터에서 currentHp를 0으로 두었으면 maxHp와 같게 보여 줌 (미설정 처리)
        if (!Application.isPlaying && currentHp <= 0f)
            currentHp = maxHp;
    }

    protected virtual void Awake()
    {
        // Inspector에 currentHp가 있으면 그대로 사용, 0이면 만땅으로 시작
        if (currentHp <= 0f)
            currentHp = maxHp;
        else
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
    }

    protected virtual void Update()
    {
        Move(); // 자식에서 override하면 이동 방식 변경 가능
    }

    // 기본 이동: 오른쪽 직진
    protected virtual void Move()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    // 자식 클래스에서 공격 구현 (베이스는 비어 있음)
    protected virtual void Attack() { }

    public virtual void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
