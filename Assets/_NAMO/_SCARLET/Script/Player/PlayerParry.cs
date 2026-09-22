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
        // เปลี่ยนมาใช้คลิกขวา (Mouse 1) แทนปุ่ม F
        if (Input.GetMouseButtonDown(1) && canParry)
        {
            StartCoroutine(ParryRoutine());
        }
    }

    private IEnumerator ParryRoutine()
    {
        canParry = false;
        IsParrying = true;

        float timer = 0f;
        bool parrySuccess = false;
        PlayerController2D playerCtrl = GetComponent<PlayerController2D>();

        while (timer < parryWindowDuration && !parrySuccess)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(parryCenter.position, parryRadius, enemyLayer);
            foreach (var hit in hits)
            {
                // Pruned IParryable (single-use) - use concrete BaseEnemy virtual + GuardianBoss domain object per tech lead
                BaseEnemy baseEnemy = hit.GetComponent<BaseEnemy>();
                GuardianBoss boss = hit.GetComponent<GuardianBoss>();
                bool isParryable = (baseEnemy != null && baseEnemy.CanBeParried()) || boss != null;
                if (isParryable)
                {
                    // Directional Parry: ตรวจสอบว่าศัตรูอยู่ด้านหน้าผู้เล่นหรือไม่
                    float dirToEnemy = Mathf.Sign(hit.transform.position.x - transform.position.x);
                    float playerFacingDir = (playerCtrl != null && playerCtrl.IsFacingRight) ? 1f : -1f;

                    if (dirToEnemy == playerFacingDir)
                    {
                        parrySuccess = true;
                        
                        if (boss != null) boss.OnParrySuccess(transform.position);
                        else baseEnemy.OnParrySuccess(transform.position);
                        
                        if (HitStopEffect.Instance != null)
                        {
                            HitStopEffect.Instance.TriggerParryHitStop(0.15f);
                        }
                        
                        // Add Screen Shake for impact!
                        if (CameraController2D.Instance != null)
                        {
                            CameraController2D.Instance.TriggerShake(0.15f, 0.5f);
                        }
                        
                        // ให้ i-frames สั้นๆ แก่ผู้เล่นเมื่อ Parry สำเร็จ
                        PlayerHealth health = GetComponent<PlayerHealth>();
                        if (health != null) health.ActivateInvincibility();

                        break; 
                    }
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