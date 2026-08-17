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

    // เก็บรายชื่อของทุกอย่างที่มี IDamageable ที่อยู่ในหมอกตอนนี้
    private List<IDamageable> targetsInZone = new List<IDamageable>();
    private Coroutine poisonCoroutine;

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

    private IEnumerator PoisonTickRoutine()
    {
        // ตราบใดที่ยังมีคนอยู่ในหมอก จะวนทำดาเมจไปเรื่อยๆ
        while (targetsInZone.Count > 0)
        {
            yield return new WaitForSeconds(tickRate); // หน่วงเวลาตามค่า Tick

            // วนทำดาเมจถอยหลัง (กัน Error เวลามีตัวละครตายและถูกเตะออกจาก List กลางทาง)
            for (int i = targetsInZone.Count - 1; i >= 0; i--)
            {
                // บังคับทำดาเมจใส่ (ทิศทาง = จากจุดศูนย์กลางหมอก)
                targetsInZone[i].TakeDamage(damagePerTick, transform.position);
            }
        }
        poisonCoroutine = null;
    }
}