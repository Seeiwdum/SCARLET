using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private bool isDownHitbox = false;
    [SerializeField] private LayerMask enemyLayer; 

    private void Awake()
    {
        if (combat == null) combat = GetComponentInParent<PlayerCombat>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            // 1. เปลี่ยนจากค้นหา BaseEnemy เป็น IDamageable
            IDamageable damageableTarget = other.GetComponent<IDamageable>();
            
            if (damageableTarget != null)
            {
                int damage = combat != null ? combat.CurrentDamage : 1;
                
                // 2. เรียก TakeDamage (ไม่สนว่ามันคือศัตรู, ดาบที่ปาไป, หรือสิ่งของ)
                damageableTarget.TakeDamage(damage, transform.position);

                if (combat != null) combat.AddFlameEnergyOnHit();

                Debug.Log($"<color=red>[Hit Success] โจมตีโดน {other.name} (Damage: {damage})</color>");

                // 3. ส่งสัญญาณบอกศูนย์กลางว่าฟันโดนศัตรูแล้ว (อาจจะเอาไปทำ Hitstop เฟรมหยุดในอนาคต)
                GameEvents.OnEnemyHit?.Invoke();

                // ปิด CameraShake ตรงนี้ เพราะเราย้ายไปให้ CameraController ฟัง Event ได้ (แต่ถ้าจะเก็บไว้ก่อนก็ไม่เป็นไร)
                CameraController2D.Instance?.TriggerShake(0.1f, 0.2f);

                if (isDownHitbox)
                {
                    PlayerController2D playerCtrl = combat != null ? combat.GetComponent<PlayerController2D>() : GetComponentInParent<PlayerController2D>();
                    if (playerCtrl != null) playerCtrl.Bounce(14f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);

        if (myCol is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (myCol is CircleCollider2D circle)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(circle.offset, circle.radius);
        }
    }
}