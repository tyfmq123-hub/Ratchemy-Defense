using System.Collections;
using UnityEngine;

// 탱커 쥐(Tank Rat) — PlayerUnitBase 상속
// - 기본 공격은 근접 단일 대상
// - 스킬 쿨이 돌면 skillRange 안 적들에게 데미지 + 짧은 밀치기 적용
// - 적이 없으면 전진
public class TankRat : PlayerUnitBase
{
    private LayerMask enemyLayer;      // "Enemy" 레이어만 감지
    private float attackCooldown;      // 기본 공격 쿨다운
    private float skillCooldownTimer;  // 스킬 쿨다운

    [Header("스킬 설정")]
    [SerializeField] private float skillCooldown = 5f;      // 스킬 재사용 대기 시간
    [SerializeField] private float knockbackDistance = 1.2f; // 밀쳐내는 거리
    [SerializeField] private float knockbackDuration = 0.15f; // 밀쳐내는 데 걸리는 시간
    [SerializeField] private float skillRange = 1.6f;       // 스킬 판정 범위
    [SerializeField] private int skillDamage = 3;           // 스킬 데미지 (EnemyUnit.TakeDamage int와 동일)

    protected override void Awake()
    {
        // 탱커 기본 스탯 (Inspector 값보다 우선)
        maxHp = 220f;
        attackPower = 4;
        moveSpeed = 2.2f;
        attackSpeed = 0.7f;
        attackRange = 1.1f;

        base.Awake();
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        Collider2D enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayer);

        if (enemy != null)
        {
            // 스킬 쿨이 끝나면 스킬 우선, 아니면 기본 공격
            if (skillCooldownTimer <= 0f)
                UseKnockbackSkill();
            else
                BasicAttack(enemy);
        }
        else
        {
            Move();
        }
    }

    // 단일 대상 기본 공격
    private void BasicAttack(Collider2D target)
    {
        if (attackCooldown > 0f)
            return;

        EnemyUnit enemyUnit = target.GetComponent<EnemyUnit>();

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(attackPower);
            Debug.Log($"[TankRat] 기본 공격 → {target.name}, 데미지: {attackPower}");
        }

        attackCooldown = 1f / attackSpeed;
    }

    // 범위 내 적들에게 스킬 데미지 + 밀치기
    private void UseKnockbackSkill()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();

            if (enemyUnit != null)
            {
                enemyUnit.TakeDamage(skillDamage);

                // 적을 오른쪽으로 살짝 밀어냄
                StartCoroutine(KnockbackEnemy(hit.transform));

                Debug.Log($"[TankRat] 밀치기 스킬 → {hit.name}");
            }
        }

        skillCooldownTimer = skillCooldown;
    }

    // enemy를 knockbackDuration 동안 knockbackDistance만큼 보간 이동
    private IEnumerator KnockbackEnemy(Transform enemy)
    {
        if (enemy == null)
            yield break;

        Vector3 startPos = enemy.position;
        Vector3 endPos = startPos + Vector3.right * knockbackDistance;

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (enemy == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;

            enemy.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        if (enemy != null)
        {
            enemy.position = endPos;
        }
    }

    // Scene 뷰: 회색 원 = 기본 사거리, 초록 원 = 스킬 범위
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}