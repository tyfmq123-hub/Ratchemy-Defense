using UnityEngine;

// 절연체 쥐(Insulator Rat) — PlayerUnitBase 상속
// - 사거리 안에 Enemy가 있으면: 멈추고 주기적으로 공격
// - Enemy가 없으면: 오른쪽으로 이동 (PlayerUnitBase.Move)
// - 스탯은 Inspector에서 수정 (Play 시 Awake가 숫자를 덮어쓰지 않음)
public class InsulatorRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolName = "isattack"; // Animator Bool 파라미터명
    private LayerMask enemyLayer;  // "Enemy" 레이어만 공격 대상
    private float attackCooldown;  // 다음 공격까지 남은 시간(초)

    protected override void Awake()
    {
        base.Awake(); // currentHp 등 부모 초기화
        if (animator == null)
            animator = GetComponent<Animator>();
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    // 컴포넌트를 처음 붙이거나 Reset 메뉴 실행 시에만 기본 스탯 채움 (Play 때는 실행 안 됨)
    private void Reset()
    {
        maxHp = 80f;
        currentHp = maxHp;
        attackPower = 12;
        moveSpeed = 3f;
        attackSpeed = 1.2f;
        attackRange = 1f;
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        EnemyUnit enemy = GetEnemyInAttackRange();

        if (enemy != null)
        {
            SetAttackAnimation(true);
            Attack(enemy); // 적 있으면 공격만
        }
        else
        {
            SetAttackAnimation(false);
            Move();        // 없으면 전진
        }
    }

    private EnemyUnit GetEnemyInAttackRange()
    {
        Collider2D collider = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);
        if (collider == null)
            return null;

        EnemyUnit enemyUnit = collider.GetComponent<EnemyUnit>();
        if (enemyUnit == null || enemyUnit.IsDead())
            return null;

        return enemyUnit;
    }

    private void Attack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;

        target.TakeDamage(attackPower);
        Debug.Log($"[InsulatorRat] {target.name} 공격 → 데미지: {attackPower}");
        attackCooldown = 1f / attackSpeed;
    }

    // 적이 있을 때 true, 없을 때 false로 유지
    private void SetAttackAnimation(bool isAttacking)
    {
        if (animator == null || string.IsNullOrEmpty(attackBoolName))
            return;

        animator.SetBool(attackBoolName, isAttacking);
    }

    // Scene 뷰에서 선택 시 공격 범위 빨간 원 표시 (게임 로직과 무관, 에디터용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
