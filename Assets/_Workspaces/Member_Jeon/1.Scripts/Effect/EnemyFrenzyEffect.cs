using System.Collections;
using UnityEngine;

// SafetyManagerRat 3타 미침 — 맞은 적 1마리만 짧게 뒤로 밀기
public class EnemyFrenzyEffect : MonoBehaviour
{
    private EnemyUnit enemyUnit;
    private bool restoreEnemyUnitOnEnd = true;
    private Coroutine knockbackCoroutine;

    public static void Apply(GameObject enemyObject, float knockbackDistance, float knockbackDuration)
    {
        if (enemyObject == null || knockbackDistance <= 0f || knockbackDuration <= 0f)
            return;

        EnemyUnit unit = enemyObject.GetComponent<EnemyUnit>();
        if (unit == null || unit.IsDead())
            return;

        EnemyFrenzyEffect effect = enemyObject.GetComponent<EnemyFrenzyEffect>();
        if (effect == null)
            effect = enemyObject.AddComponent<EnemyFrenzyEffect>();

        effect.BeginKnockback(knockbackDistance, knockbackDuration, unit);
    }

    private void BeginKnockback(float knockbackDistance, float knockbackDuration, EnemyUnit unit)
    {
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        if (enemyUnit == null)
        {
            enemyUnit = unit;
            restoreEnemyUnitOnEnd = enemyUnit.enabled;
        }

        enemyUnit.enabled = false;
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(knockbackDistance, knockbackDuration));
    }

    private IEnumerator KnockbackRoutine(float knockbackDistance, float knockbackDuration)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.right * knockbackDistance;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (enemyUnit == null || enemyUnit.IsDead())
            {
                EndKnockback();
                yield break;
            }

            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / knockbackDuration);
            yield return null;
        }

        transform.position = endPos;
        EndKnockback();
    }

    private void EndKnockback()
    {
        knockbackCoroutine = null;

        if (enemyUnit != null && restoreEnemyUnitOnEnd && !enemyUnit.IsDead())
            enemyUnit.enabled = true;

        Destroy(this);
    }

    private void OnDestroy()
    {
        if (enemyUnit == null || enemyUnit.IsDead())
            return;

        if (enemyUnit.enabled)
            return;

        if (restoreEnemyUnitOnEnd)
            enemyUnit.enabled = true;
    }
}
