using System.Collections;
using UnityEngine;

// 탱커 쥐(Tank Rat) — PlayerUnitBase 상속
// - 기본 공격은 근접 단일 대상
// - 스킬 쿨이 돌면 skillRange 안 적들에게 데미지 + 짧은 밀치기 적용
// - 적이 없으면 전진
public class TankRat : PlayerUnitBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private string skillTriggerName = "skill";
    private LayerMask enemyLayer;      // "Enemy" 레이어만 감지
    private float attackCooldown;      // 기본 공격 쿨다운
    private float skillCooldownTimer;  // 스킬 쿨다운
    private bool isCastingSkill;       // 스킬 애니메이션 재생 중 여부
    private bool skillEventReceived;   // Animation Event 수신 여부

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
        if (animator == null)
            animator = GetComponent<Animator>();
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    protected override void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (skillCooldownTimer > 0f)
            skillCooldownTimer -= Time.deltaTime;

        // 스킬 모션 중에는 기본 공격/이동 중지
        if (isCastingSkill)
            return;

        EnemyUnit enemy = GetEnemyInAttackRange();

        if (enemy != null)
        {
            // 스킬 쿨이 끝나면 스킬 우선, 아니면 기본 공격
            if (skillCooldownTimer <= 0f)
            {
                SetAnimatorPaused(false);
                StartCoroutine(CastKnockbackSkillByEvent());
            }
            else
            {
                // 기본 공격 딜레이 중에는 이동도 안 하고 애니메이션도 정지
                bool waitingBasicAttackDelay = attackCooldown > 0f;
                SetAnimatorPaused(waitingBasicAttackDelay);
                BasicAttack(enemy);
            }
        }
        else
        {
            SetAnimatorPaused(false);
            Move();
        }
    }

    // 단일 대상 기본 공격
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

    private void BasicAttack(EnemyUnit target)
    {
        if (attackCooldown > 0f)
            return;
        target.TakeDamage(attackPower);
        Debug.Log($"[TankRat] 기본 공격 → {target.name}, 데미지: {attackPower}");

        attackCooldown = 1f / attackSpeed;
    }

    // 스킬 트리거 발동 후 Animation Event를 기다렸다가 실제 효과 적용
    private IEnumerator CastKnockbackSkillByEvent()
    {
        isCastingSkill = true;
        skillEventReceived = false;
        skillCooldownTimer = skillCooldown;
        FireSkillTrigger();

        float timeout = 2.5f; // 이벤트 누락 시 무한 대기 방지
        while (!skillEventReceived && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!skillEventReceived)
            Debug.LogWarning("[TankRat] Skill Animation Event 미수신. 이벤트를 확인해 주세요.");

        ApplyKnockbackSkillDamage();
        isCastingSkill = false;
    }

    // Animation Clip Event 함수명으로 등록: OnSkillAnimationEvent
    public void OnSkillAnimationEvent()
    {
        if (!isCastingSkill)
            return;

        skillEventReceived = true;
    }

    // 범위 내 적들에게 스킬 데미지 + 밀치기
    private void ApplyKnockbackSkillDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyUnit enemyUnit = hit.GetComponent<EnemyUnit>();

            if (enemyUnit != null && !enemyUnit.IsDead())
            {
                enemyUnit.TakeDamage(skillDamage);

                // 적을 오른쪽으로 살짝 밀어냄
                StartCoroutine(KnockbackEnemy(hit.transform));

                Debug.Log($"[TankRat] 밀치기 스킬 → {hit.name}");
            }
        }
    }

    private void FireSkillTrigger()
    {
        if (animator == null || string.IsNullOrEmpty(skillTriggerName))
            return;

        animator.ResetTrigger(skillTriggerName);
        animator.SetTrigger(skillTriggerName);
    }

    // 딜레이 구간에서 run 반복이 거슬릴 때 애니메이션 시간을 잠시 멈춤
    private void SetAnimatorPaused(bool paused)
    {
        if (animator == null)
            return;

        animator.speed = paused ? 0f : 1f;
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