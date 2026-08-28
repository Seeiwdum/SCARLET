using UnityEngine;

public class BeaconTarget : MonoBehaviour
{
    // กำหนดประเภทเป้าหมายเพื่อเปลี่ยนพฤติกรรมแสงของสร้อยคอ
    public enum TargetType
    {
        LoreFragment,   // เศษบันทึกโบราณ (แสงสีฟ้า/ทอง สว่างนุ่มนวล)
        GuardianBoss,   // รังบอสผู้พิทักษ์ / หัวใจเพลิง (แสงสีส้มเพลิง เต้นรัว)
        CorruptedOrigin // ประภาคาร / ยายคาเรน (แสงสีม่วงดำ กะพริบติดๆ ดับๆ)
    }

    [Header("Beacon Settings")]
    [SerializeField] private TargetType targetType = TargetType.LoreFragment;
    [Tooltip("ระยะที่สร้อยคอจะเริ่มตรวจจับและตอบสนอง (หน่วยเป็นเมตร/ช่อง)")]
    [SerializeField] private float detectionRadius = 12f;

    public TargetType Type => targetType;
    public float DetectionRadius => detectionRadius;

    private void OnEnable()
    {
        // ลงทะเบียนเป้าหมายเข้าสู่ระบบเมื่อ Object ปรากฏในฉาก
        MagicStoneBeacon.RegisterTarget(this);
    }

    private void OnDisable()
    {
        // ถอนการลงทะเบียนเมื่อ Object ถูกเก็บหรือถูกทำลาย
        MagicStoneBeacon.UnregisterTarget(this);
    }

    private void OnDrawGizmosSelected()
    {
        // วาดวงกลมรัศมีตรวจจับในหน้า Scene
        Gizmos.color = targetType == TargetType.CorruptedOrigin ? Color.magenta : (targetType == TargetType.GuardianBoss ? Color.red : Color.cyan);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}