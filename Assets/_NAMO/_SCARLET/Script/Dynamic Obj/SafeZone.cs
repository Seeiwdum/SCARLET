using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> Domain object: Charcoal Village safe haven.
/// Encapsulates protected volume and explicit domain operations per tech lead.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    [Header("Protection")]
    [SerializeField] private Collider2D zoneCollider;
    private readonly HashSet<IDamageable> protectedEntities = new HashSet<IDamageable>();

    // Explicit domain query: Does this SafeZone contain the point?
    public bool Contains(Vector2 point)
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        return zoneCollider != null && zoneCollider.OverlapPoint(point);
    }

    public bool Contains(Collider2D col)
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        return zoneCollider != null && zoneCollider.bounds.Intersects(col.bounds);
    }

    // Explicit domain operation: Grant protection to a target (called by zone trigger)
    public void Protect(IDamageable target)
    {
        if (target == null) return;
        protectedEntities.Add(target);
        if (target is MonoBehaviour mb)
        {
            var hood = mb.GetComponent<PlayerHood>();
            if (hood != null) hood.SetSafeZone(true);
        }
    }

    // Explicit domain operation: Revoke protection
    public void Unprotect(IDamageable target)
    {
        if (target == null) return;
        protectedEntities.Remove(target);
        if (target is MonoBehaviour mb)
        {
            var hood = mb.GetComponent<PlayerHood>();
            if (hood != null) hood.SetSafeZone(false);
        }
    }

    // Explicit domain query: Is this entity currently protected by this zone?
    public bool IsProtecting(IDamageable target) => protectedEntities.Contains(target);

    public bool IsProtecting(GameObject go)
    {
        if (go == null) return false;
        var d = go.GetComponent<IDamageable>();
        return d != null && protectedEntities.Contains(d);
    }

    // Convenience for PoisonZone decoupling: ask SafeZone if entity is safe
    public static bool IsEntityInAnySafeZone(IDamageable target)
    {
        if (target is MonoBehaviour mb)
        {
            var hood = mb.GetComponent<PlayerHood>();
            if (hood != null) return hood.IsInSafeZone;
        }
        return false;
    }
}
