using System.Collections;
using UnityEngine;

public class ThrownSword : MonoBehaviour, IDamageable
{
    public enum SwordThrowType { Forward, UpwardWarp, DownwardVault }

    [Header("Movement Settings")]
    [SerializeField] private float flightSpeed = 22f;
    [SerializeField] private float maxTravelDistance = 6.5f;

    [Header("Inspector Configurable Timers")]
    [Tooltip("ระยะเวลารอให้กด Warp กลางอากาศ (ปาขึ้น/หน้า)")]
    [SerializeField] private float warpWindowDuration = 1.8f;

    [Tooltip("ระยะเวลาที่ Scarlet ยืนพักบนดาบได้ก่อนดาบสลาย (ปาลงล่าง)")]
    [SerializeField] private float platformStandDuration = 1.5f;

    [Header("Vault Settings")]
    [Tooltip("แรงเด้งขึ้นฟ้าเมื่อกด Interact เหยียบดาบ")]
    [SerializeField] private float vaultBounceForce = 18f;

    [Header("Components & Layer")]
    [SerializeField] private Collider2D triggerCollider;  // Trigger เช็กชน
    [SerializeField] private Collider2D solidCollider;    // แท่นยืน Platform
    [SerializeField] private GameObject fireVFXPrefab;

    private Vector3 startPos;
    private Vector2 flyDirection;
    private SwordThrowType throwType;
    private PlayerController2D playerRef;
    private bool isStopped = false;
    private bool isPlayerTouching = false;

    public void Initialize(Vector2 direction, SwordThrowType type, PlayerController2D player)
    {
        flyDirection = direction.normalized;
        throwType = type;
        playerRef = player;
        startPos = transform.position;

        if (solidCollider != null) solidCollider.enabled = false;

        // หมุนตัวดาบตามทิศทาง
        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        if (!isStopped)
        {
            transform.position += (Vector3)(flyDirection * flightSpeed * Time.deltaTime);

            if (Vector3.Distance(startPos, transform.position) >= maxTravelDistance)
            {
                StopSword();
            }
        }
        else
        {
            HandleInteractionInput();
        }
    }

    private void StopSword()
    {
        isStopped = true;

        if (throwType == SwordThrowType.DownwardVault)
        {
            // เปิด Solid Collider เพื่อให้ผู้เล่นยืนพักได้
            if (solidCollider != null) solidCollider.enabled = true;
            StartCoroutine(PlatformTimeoutRoutine());
        }
        else
        {
            StartCoroutine(WarpWindowTimeoutRoutine());
        }
    }

    private void HandleInteractionInput()
    {
        // Key ดาบ (ค่าเริ่มต้นคือ J หรือคลิกขวา)
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(1))
        {
            if (throwType == SwordThrowType.DownwardVault && isPlayerTouching)
            {
                ExecuteVault();
            }
            else if (throwType != SwordThrowType.DownwardVault)
            {
                ExecuteWarp();
            }
        }
    }

    private void ExecuteVault()
    {
        StopAllCoroutines();
        SpawnVFX();
        if (playerRef != null) playerRef.ExecuteSwordVaultBounce(vaultBounceForce);
        Destroy(gameObject);
    }

    private void ExecuteWarp()
    {
        StopAllCoroutines();
        SpawnVFX();
        if (playerRef != null) playerRef.ExecuteFireWarp(transform.position);
        Destroy(gameObject);
    }

    private void SpawnVFX()
    {
        if (fireVFXPrefab != null)
        {
            Instantiate(fireVFXPrefab, transform.position, Quaternion.identity);
        }
    }

    private IEnumerator WarpWindowTimeoutRoutine()
    {
        yield return new WaitForSeconds(warpWindowDuration);
        Destroy(gameObject);
    }

    private IEnumerator PlatformTimeoutRoutine()
    {
        yield return new WaitForSeconds(platformStandDuration);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerTouching = true;
            if (throwType == SwordThrowType.DownwardVault && !isStopped)
            {
                StopSword();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerTouching = false;
        }
    }

    // รองรับ Polymorphism: ถ้าฟันใส่ดาบ ก็จะเด้งตัวได้เช่นกัน
    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        ExecuteVault();
    }
}