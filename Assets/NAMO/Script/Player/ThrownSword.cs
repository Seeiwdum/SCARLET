using System.Collections;
using UnityEngine;

public class ThrownSword : MonoBehaviour, IDamageable
{
    public enum SwordThrowType { Forward, UpwardWarp, DownwardVault }

    [Header("Movement & Settings")]
    [SerializeField] private float flightSpeed = 22f;
    [SerializeField] private float maxTravelDistance = 6.5f;
    [SerializeField] private float platformStayDuration = 1.2f; // ระยะเวลายืนบนดาบได้ก่อนสลาย

    [Header("Colliders & VFX")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private Collider2D solidCollider;
    [SerializeField] private GameObject warpVFXPrefab;

    private Vector3 startPos;
    private Vector2 flyDirection;
    private SwordThrowType throwType;
    private PlayerController2D playerRef;
    private bool isStopped = false;
    private bool isPlatformActive = false;

    public void Initialize(Vector2 direction, SwordThrowType type, PlayerController2D player)
    {
        flyDirection = direction.normalized;
        throwType = type;
        playerRef = player;
        startPos = transform.position;

        if (solidCollider != null) solidCollider.enabled = false;

        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        if (!isStopped)
        {
            transform.Translate(flyDirection * (flightSpeed * Time.deltaTime), Space.World);

            if (Vector3.Distance(startPos, transform.position) >= maxTravelDistance)
            {
                StopSword();
            }
        }
        else
        {
            HandleWarpInput();
        }
    }

    private void StopSword()
    {
        if (isStopped) return;
        isStopped = true;
    }

    private void HandleWarpInput()
    {
        if (isPlatformActive) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.J))
        {
            ExecuteWarp();
        }
    }

    private void ExecuteWarp()
    {
        if (warpVFXPrefab != null) Instantiate(warpVFXPrefab, transform.position, Quaternion.identity);

        if (playerRef != null)
        {
            playerRef.ExecuteFireWarp(transform.position, this);
        }
    }

    public void ConvertToPlatform()
    {
        isPlatformActive = true;
        if (solidCollider != null) solidCollider.enabled = true;
        StartCoroutine(PlatformLifeRoutine());
    }

    private IEnumerator PlatformLifeRoutine()
    {
        yield return new WaitForSeconds(platformStayDuration);
        BreakSword();
    }

    public void BreakSword()
    {
        StopAllCoroutines();
        if (playerRef != null) playerRef.DismountSwordPlatform();
        if (warpVFXPrefab != null) Instantiate(warpVFXPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        BreakSword();
    }
}