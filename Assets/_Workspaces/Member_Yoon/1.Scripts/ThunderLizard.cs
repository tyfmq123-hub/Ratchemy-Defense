using System.Collections;
using UnityEngine;

// 번개도마뱀 1단계 - 이동 + 근접 일반공격
// Inspector에서 enemyUnitData 슬롯에 ThunderLizardData 에셋을 연결해야 합니다.
// Animator Parameters: Bool "IsWalking", Trigger "Attack", Trigger "Die"
public class ThunderLizard : EnemyUnit
{
    protected ThunderLizardData thunderData;
    protected Animator animator;

    [SerializeField] protected float dieAnimDuration = 0.5f;

    protected override void Start()
    {
        base.Start();

        thunderData = CastData<ThunderLizardData>("ThunderLizardData");

        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning($"[ThunderLizard] Animator 컴포넌트가 없습니다. ({gameObject.name})");
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
        if (!BeginDeath()) return;

        if (animator != null)
            StartCoroutine(DieRoutine());
        else
            base.OnDie();
    }

    private IEnumerator DieRoutine()
    {
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Die");

        yield return new WaitForSeconds(dieAnimDuration);

        base.OnDie();
    }
}
