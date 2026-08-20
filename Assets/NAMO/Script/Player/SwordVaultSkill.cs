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

        if (Input.GetKey(downwardModifierKey) && !player.IsGrounded())
        {
            SpawnSword(Vector2.down, ThrownSword.SwordThrowType.DownwardVault, spawnPos);
            nextThrowTime = Time.time + throwCooldown;
        }
        else if (Input.GetKey(upwardModifierKey))
        {
            Vector2 upDir = player.IsFacingRight ? new Vector2(0.5f, 1f) : new Vector2(-0.5f, 1f);
            SpawnSword(upDir, ThrownSword.SwordThrowType.UpwardWarp, spawnPos);
            nextThrowTime = Time.time + throwCooldown;
        }
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
            
            // ไม่ให้ชนกับ Collider ของตัวผู้เล่นทันทีที่ปล่อย
            if (player.PlayerCollider != null)
            {
                Collider2D[] swordCols = swordObj.GetComponentsInChildren<Collider2D>();
                foreach (var c in swordCols)
                {
                    Physics2D.IgnoreCollision(c, player.PlayerCollider, true);
                }
            }
        }
    }
}