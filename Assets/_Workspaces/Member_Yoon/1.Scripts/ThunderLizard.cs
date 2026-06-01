using System.Collections;
using UnityEngine;

// 번개도마뱀 1단계 - 이동 + 근접 일반공격
// Inspector에서 enemyUnitData 슬롯에 ThunderLizardData 에셋을 연결해야 합니다.
// Animator Parameters: Bool "IsWalking", Trigger "Attack", Trigger "Die"
public class ThunderLizard : EnemyUnit
{
    protected ThunderLizardData thunderData;
    protected Animator animator;
    private bool isDying = false;

    [SerializeField] protected float dieAnimDuration = 0.5f;

    protected override void Start()
    {
        base.Start();

        thunderData = enemyUnitData as ThunderLizardData;
        if (thunderData == null)
            Debug.LogError($"[ThunderLizard] enemyUnitData에 ThunderLizardData를 연결해주세요. ({gameObject.name})");

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

        yield return new WaitForSeconds(dieAnimDuration);

        base.OnDie();
    }
}
