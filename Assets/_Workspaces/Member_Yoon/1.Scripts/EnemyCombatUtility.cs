using UnityEngine;

public static class EnemyCombatUtility
{
    public static bool TryGetPlayer(Collider2D col, out PlayerUnitBase player)
    {
        player = col.GetComponentInParent<PlayerUnitBase>();
        return player != null;
    }

    // 사거리 내 Player 유닛 중 적 위치에서 가장 가까운 대상 1명 (동률 시 InstanceID 작은 쪽)
    public static bool TryFindClosestPlayer(Vector2 center, float radius, LayerMask layer, out Collider2D closestCollider)
    {
        closestCollider = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, layer);
        if (hits.Length == 0) return false;

        PlayerUnitBase closestPlayer = null;
        float closestSqrDist = float.MaxValue;
        Collider2D closestCol = null;

        foreach (Collider2D col in hits)
        {
            if (!TryGetPlayer(col, out PlayerUnitBase player)) continue;

            float sqrDist = ((Vector2)player.transform.position - center).sqrMagnitude;
            if (!IsCloserTarget(player, sqrDist, closestPlayer, closestSqrDist)) continue;

            closestPlayer = player;
            closestSqrDist = sqrDist;
            closestCol = col;
        }

        if (closestCol == null) return false;

        closestCollider = closestCol;
        return true;
    }

    private static bool IsCloserTarget(PlayerUnitBase candidate, float candidateSqrDist,
        PlayerUnitBase current, float currentSqrDist)
    {
        if (current == null) return true;
        if (candidateSqrDist < currentSqrDist) return true;
        if (candidateSqrDist > currentSqrDist) return false;
        return candidate.GetInstanceID() < current.GetInstanceID();
    }

    public static void DamagePlayersInRadius(Vector2 center, float radius, LayerMask layer, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, layer);
        foreach (Collider2D col in hits)
        {
            if (TryGetPlayer(col, out PlayerUnitBase player))
                player.TakeDamage(damage);
        }
    }

    public static void DamagePlayersInRadius(Vector2 center, float radius, LayerMask layer, int damage)
    {
        DamagePlayersInRadius(center, radius, layer, (float)damage);
    }
}
