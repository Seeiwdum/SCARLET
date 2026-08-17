using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class SwordVaultSkill : MonoBehaviour
{
    [Header("Prefab & Spawn Point")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Key Bindings")]
    [SerializeField] private KeyCode swordKey = KeyCode.J;
    [SerializeField] private KeyCode upwardModifierKey = KeyCode.Space;
    [SerializeField] private KeyCode downwardModifierKey = KeyCode.S;

    [Header("Skill Cooldown")]
    [SerializeField] private float throwCooldown = 1.0f;
    private float nextThrowTime = 0f;

    private PlayerController2D player;

    private void Awake()
    {
        player = GetComponent<PlayerController2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(swordKey) && Time.time >= nextThrowTime)
        {
            HandleThrowInput();
        }
    }

    private void HandleThrowInput()
    {
        if (swordPrefab == null)
        {
            Debug.LogWarning("[SwordVaultSkill] ยังไม่ได้ลาก swordPrefab มาใส่ใน Inspector!");
            return;
        }

        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;

        // 1. ปาลงล่าง: ต้องกด S ค้าง + ไม่อยู่บนพื้น (กลางอากาศเท่านั้น)
        if (Input.GetKey(downwardModifierKey) && !player.IsGrounded())
        {
            SpawnSword(Vector2.down, ThrownSword.SwordThrowType.DownwardVault, spawnPos);
            nextThrowTime = Time.time + throwCooldown;
        }
        // 2. ปาขึ้นบน: กด Space ค้าง + กด Key ดาบ
        else if (Input.GetKey(upwardModifierKey))
        {
            Vector2 upDir = player.IsFacingRight ? new Vector2(0.5f, 1f) : new Vector2(-0.5f, 1f);
            SpawnSword(upDir, ThrownSword.SwordThrowType.UpwardWarp, spawnPos);
            nextThrowTime = Time.time + throwCooldown;
        }
        // 3. ปาไปข้างหน้าตามปกติ
        else
        {
            Vector2 forwardDir = player.IsFacingRight ? Vector2.right : Vector2.left;
            SpawnSword(forwardDir, ThrownSword.SwordThrowType.Forward, spawnPos);
            nextThrowTime = Time.time + throwCooldown;
        }
    }

    private void SpawnSword(Vector2 direction, ThrownSword.SwordThrowType type, Vector3 position)
    {
        GameObject swordObj = Instantiate(swordPrefab, position, Quaternion.identity);
        ThrownSword swordScript = swordObj.GetComponent<ThrownSword>();

        if (swordScript != null)
        {
            swordScript.Initialize(direction, type, player);
        }
    }
}