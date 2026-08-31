using System.Collections;
using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("Parry Settings")]
    [SerializeField] private KeyCode parryKey = KeyCode.F;
    [SerializeField] private float parryWindowDuration = 0.2f;
    [SerializeField] private float parryCooldown = 0.5f;
    [SerializeField] private Transform parryCenter;
    [SerializeField] private float parryRadius = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    private bool canParry = true;
    public bool IsParrying { get; private set; }

    private void Update()
    {
        if (Input.GetKeyDown(parryKey) && canParry)
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        canParry = false;
        IsParrying = true;

        // วนลูปเช็กศัตรูในระยะช่วงที่ Parry Window เปิดอยู่
        float timer = 0f;
        bool parrySuccess = false;

        while (timer < parryWindowDuration && !parrySuccess)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(parryCenter.position, parryRadius, enemyLayer);
            foreach (var hit in hits)
            {
                IParryable parryTarget = hit.GetComponent<IParryable>();
                if (parryTarget != null)
                {
                    // 1. Parry สำเร็จ!
                    parrySuccess = true;
                    
                    // 2. สั่งบอสให้กระเด็น/สตั้น
                    parryTarget.OnParrySuccess(transform.position);
                    
                    // 3. เรียก Game Feel (Hit Stop + Flash)
                    if (HitStopManager.Instance != null)
                    {
                        HitStopManager.Instance.TriggerParryHitStop(0.15f);
                    }
                    
                    break; // หยุดเช็กตัวอื่น
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        IsParrying = false;
        yield return new WaitForSeconds(parryCooldown);
        canParry = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (parryCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(parryCenter.position, parryRadius);
        }
    }
}