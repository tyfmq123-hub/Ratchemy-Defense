using System.Collections;
using UnityEngine;

// 냉각 처리쥐(Coolant Rat) — PlayerUnitBase 상속
// - attackRange 안 가장 가까운 적을 찾아 기본 원거리 공격
// - 스킬 쿨이 끝나면 넓은 범위(skillRange)로 최대 skillTargetCount명에게 스킬 데미지
// - 적이 없으면 Move()로 전진
public class CoolantRat : PlayerUnitBase
{
    [Header("공격 설정")]
    [SerializeField] private LayerMask enemyLayer;       // 공격·탐지 대상 레이어
    [SerializeField] private float skillRange = 4f;      // 스킬 범위 반경
    [SerializeField] private int skillTargetCount = 3;   // 스킬이 맞출 최대 적 수

    [Header("스킬 설정")]
    [SerializeField] private int skillDamage = 8;        // 스킬 1회 데미지 (EnemyUnit.TakeDamage는 int)
    [SerializeField] private float skillCooldown = 6f;   // 스킬 재사용 대기 시간(초)
    [SerializeField] private float debuffDuration = 3f;  // 화염슬라임 폭발 봉인 지속 시간(초)

    private float attackCooldown;      // 기본 공격 쿨다운
    private float skillCooldownTimer;  // 스킬 쿨다운

    protected override void Awake()
    {
        base.Awake();
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    // 컴포넌트 처음 추가·Reset 시 기본 스탯 (Play 때 Inspector 값 덮어쓰지 않음)
    private void Reset()
    {
        maxHp = 60f;
        currentHp = maxHp;
        attackPower = 8;
        moveSpeed = 2.2f;
        attackSpeed = 1f;
        attackRange = 3.5f;
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        Collider2D target = FindNearestEnemy();

        if (target != null)
        {
            // 스킬 쿨이 끝났으면 스킬 우선, 아니면 기본 공격
            if (skillCooldownTimer <= 0f)
                UseSkill();
            else
                BasicAttack(target);
        }
        else
        {
            Move();
        }
    }

    // attackRange 안 Enemy 중 가장 가까운 콜라이더 1개 반환 (없으면 null)
    private Collider2D FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (hits.Length == 0)
            return null;

        Collider2D nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = hit;
            }
        }

        return nearestEnemy;
    }

    // 단일 대상 기본 원거리 공격
    private void BasicAttack(Collider2D target)
    {
        if (attackCooldown > 0f)
            return;

        EnemyUnit enemyUnit = target.GetComponent<EnemyUnit>();

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(attackPower);
            Debug.Log($"[CoolantRat] 기본 원거리 공격 → {target.name}, 데미지: {attackPower}");
        }

        attackCooldown = 1f / attackSpeed;
    }

    // skillRange 안 적에게 스킬 데미지 (최대 skillTargetCount명)
    private void UseSkill()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);

        int hitCount = 0;

        foreach (Collider2D hit in hits)
        {
            if (hitCount >= skillTargetCount)
                break;

            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();

            if (enemyUnit != null)
            {
                enemyUnit.TakeDamage(skillDamage);
                Debug.Log($"[CoolantRat] 냉각 범위 스킬 → {hit.name}, 데미지: {skillDamage}");

                // 화염슬라임이면 폭발 봉인 디버프 적용
                FlameSlime flameSlime = hit.GetComponent<FlameSlime>();
                if (flameSlime != null)
                    StartCoroutine(ApplyExplosionDisableDebuff(flameSlime));

                hitCount++;
            }
        }

        skillCooldownTimer = skillCooldown;
    }

    // 화염슬라임 폭발 스킬을 debuffDuration 동안 막음
    private IEnumerator ApplyExplosionDisableDebuff(FlameSlime flameSlime)
    {
        flameSlime.ApplyDebuff(FlameSlimeDebuff.ExplosionDisabled);

        yield return new WaitForSeconds(debuffDuration);

        if (flameSlime != null)
            flameSlime.RemoveDebuff(FlameSlimeDebuff.ExplosionDisabled);
    }

    // Scene 뷰: 파란 원 = 기본 사거리, 청록 원 = 스킬 범위
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}
