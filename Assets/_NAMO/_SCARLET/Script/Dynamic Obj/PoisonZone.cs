using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PoisonZone : MonoBehaviour
{
    [Header("Poison Settings")]
    [Tooltip("ดาเมจที่ทำต่อ 1 รอบ")]
    [SerializeField] private int damagePerTick = 1;
    [Tooltip("ความถี่ในการโดนดาเมจ (เช่น โดนทุกๆ 1.5 วินาที)")]
    [SerializeField] private float tickRate = 1.5f;

    private Collider2D zoneCollider;
    // Explicit domain collection: entities currently inside corrosive volume
    private List<IDamageable> targetsInZone = new List<IDamageable>();
    private Coroutine poisonCoroutine;

    private void Awake() { zoneCollider = GetComponent<Collider2D>(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ถ้าสิ่งที่เข้ามา มีความสามารถในการรับดาเมจ (IDamageable)
        IDamageable target = other.GetComponent<IDamageable>();
        
        if (target != null && !targetsInZone.Contains(target))
        {
            targetsInZone.Add(target);
            
            // ถ้าเป็นเป้าหมายแรกที่เข้ามา ให้เริ่มเปิดเครื่องทำดาเมจ!
            if (poisonCoroutine == null) 
            {
                poisonCoroutine = StartCoroutine(PoisonTickRoutine());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null && targetsInZone.Contains(target))
        {
            targetsInZone.Remove(target);
            
            // ถ้าไม่มีใครอยู่ในหมอกแล้ว ปิดเครื่องทำดาเมจ
            if (targetsInZone.Count == 0 && poisonCoroutine != null)
            {
                StopCoroutine(poisonCoroutine);
                poisonCoroutine = null;
            }
        }
    }

    // --- Explicit domain methods (tech lead point 5) ---
    public bool Contains(IDamageable target) => targetsInZone.Contains(target);
    public bool Contains(Collider2D col) => col != null && zoneCollider != null && zoneCollider.OverlapPoint(col.transform.position);
    public IReadOnlyList<IDamageable> CurrentTargets() => targetsInZone;

    // Domain query: is target protected via Hood or SafeZone? Decoupled via SafeZone
    public bool IsProtected(IDamageable target)
    {
        if (target is MonoBehaviour mb)
        {
            // Prefer SafeZone domain check, fall back to HoodState
            if (SafeZone.IsEntityInAnySafeZone(target)) return true;
            var hood = mb.GetComponent<PlayerHood>();
            if (hood != null) return hood.IsProtectedFromPoison();
        }
        return false;
    }

    // Domain operation: apply single poison tick to all unprotected targets
    public void ApplyPoisonTick()
    {
        for (int i = targetsInZone.Count - 1; i >= 0; i--)
        {
            if (IsProtected(targetsInZone[i])) continue;
            targetsInZone[i].TakeDamage(damagePerTick, transform.position);
        }
    }

    private IEnumerator PoisonTickRoutine()
    {
        while (targetsInZone.Count > 0)
        {
            yield return new WaitForSeconds(tickRate);
            ApplyPoisonTick();
        }
        poisonCoroutine = null;
    }
}