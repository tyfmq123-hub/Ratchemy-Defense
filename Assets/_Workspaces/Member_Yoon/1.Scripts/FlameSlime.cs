using System.Collections;
using UnityEngine;

public enum FlameSlimeDebuff
{
    ExplosionDisabled,   // 폭발 봉인 (3단계 사망 폭발 비활성화)
}

// 화염슬라임 1단계 - 이동 + 근접 일반공격
// Inspector에서 enemyUnitData 슬롯에 FlameSlimeData 에셋을 연결해야 합니다.
// Animator Parameters: Bool "IsWalking", Trigger "Attack", Trigger "Die"
public class FlameSlime : EnemyUnit
{
    protected FlameSlimeData flameData;
    protected Animator animator;
    protected bool isDying = false;

    protected bool isSkillDisabled = false;

    public void ApplyDebuff(FlameSlimeDebuff debuff)
    {
        switch (debuff)
        {
            case FlameSlimeDebuff.ExplosionDisabled:
                isSkillDisabled = true;
                break;
        }
    }

    public void RemoveDebuff(FlameSlimeDebuff debuff)
    {
        switch (debuff)
        {
            case FlameSlimeDebuff.ExplosionDisabled:
                isSkillDisabled = false;
                break;
        }
    }

    [SerializeField] protected float dieAnimDuration = 0.5f;

    protected override void Start()
    {
        base.Start();

        flameData = enemyUnitData as FlameSlimeData;
        if (flameData == null)
            Debug.LogError($"[FlameSlime] enemyUnitData에 FlameSlimeData를 연결해주세요. ({gameObject.name})");

        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning($"[FlameSlime] Animator 컴포넌트가 없습니다. ({gameObject.name})");
    }

    protected override void MoveLeft()
    {
        base.MoveLeft();
        animator?.SetBool("IsWalking", true);
    }

    protected override void Attack(Collider2D target)
    {
        animator?.SetBool("IsWalking", false);
        animator?.SetTrigger("Attack");
        base.Attack(target);
    }

    protected override void OnDie()
    {
        if (isDying) return;
        isDying = true;

        if (animator != null)
            StartCoroutine(DieRoutine());
        else
            base.OnDie();
    }

    private IEnumerator DieRoutine()
    {
        moveSpeed = 0f;
        attackSpeed = 0f;

        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Die");

        // Inspector의 dieAnimDuration 값만큼 대기 후 제거
        yield return new WaitForSeconds(dieAnimDuration);

        base.OnDie();
    }
}
