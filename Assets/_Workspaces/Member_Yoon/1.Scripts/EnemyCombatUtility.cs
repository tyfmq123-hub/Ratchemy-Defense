using UnityEngine;

public static class EnemyCombatUtility
{
    public static bool TryGetPlayer(Collider2D col, out PlayerUnitBase player)
    {
        player = col.GetComponentInParent<PlayerUnitBase>();
        return player != null;
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
