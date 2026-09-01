using System.Collections;
using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    public static CameraController2D Instance { get; private set; }

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);

    [Header("Smooth Movement Settings")]
    [SerializeField] private float smoothSpeed = 0.18f;
    private Vector3 velocity = Vector3.zero;

    [Header("Look Ahead Settings (Silksong Style)")]
    [SerializeField] private float lookAheadDistance = 2.5f;
    [SerializeField] private float lookAheadSpeed = 3.5f;
    private float currentLookAheadX;

    [Header("Look Up / Down Pan Settings")]
    [SerializeField] private float verticalPanDistance = 3.5f;
    [SerializeField] private float panHoldTime = 0.4f; // ระยะเวลาที่ต้องกดค้างก่อนกล้องแพน
    private float verticalHoldTimer;
    private float currentPanY;

    [Header("Camera Shake Settings")]
    private float shakeTimer;
    private float shakeMagnitude;

    [Header("Map Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private PlayerController2D playerController;

    private void Awake()
    {
        // สร้าง Singleton เพื่อให้สคริปต์อื่นเรียกใช้ Camera Shake ได้ง่ายๆ
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (target != null)
        {
            playerController = target.GetComponent<PlayerController2D>();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. คำนวณ Look Ahead ขยับกล้องนำสายตาไปข้างหน้า
        HandleLookAhead();

        // 2. คำนวณ Look Up / Down แพนกล้องมองบน-ล่าง
        HandleVerticalPan();

        // 3. ตำแหน่งเป้าหมายของกล้อง
        Vector3 targetPosition = target.position + offset;
        targetPosition.x += currentLookAheadX;
        targetPosition.y += currentPanY;

        // 4. จำกัดขอบเขตกล้องตามขนาดแมพ (Bounds)
        if (useBounds && Camera.main != null)
        {
            float camHeight = Camera.main.orthographicSize;
            float camWidth = camHeight * Camera.main.aspect;

            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x + camWidth, maxBounds.x - camWidth);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y + camHeight, maxBounds.y - camHeight);
        }

        // 5. เคลื่อนที่กล้องอย่างนุ่มนวลด้วย SmoothDamp
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);

        // 6. ประมวลผลระบบสั่นสะเทือนกล้อง (Camera Shake)
        if (shakeTimer > 0)
        {
            Vector2 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            smoothedPosition += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
            shakeTimer -= Time.deltaTime;
        }

        transform.position = smoothedPosition;
    }

    private void HandleLookAhead()
    {
        float targetLookAhead = 0f;

        if (playerController != null)
        {
            targetLookAhead = (playerController.IsFacingRight ? 1f : -1f) * lookAheadDistance;
        }
        else
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(inputX) > 0.1f)
            {
                targetLookAhead = Mathf.Sign(inputX) * lookAheadDistance;
            }
        }

        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAhead, Time.deltaTime * lookAheadSpeed);
    }

    private void HandleVerticalPan()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");

        // ตรวจสอบว่ายืนบนพื้นและกดปุ่มขึ้น/ลงค้างไว้หรือไม่
        if (Mathf.Abs(verticalInput) > 0.5f && playerController != null && playerController.IsGrounded())
        {
            verticalHoldTimer += Time.deltaTime;
            if (verticalHoldTimer >= panHoldTime)
            {
                float targetPan = Mathf.Sign(verticalInput) * verticalPanDistance;
                currentPanY = Mathf.Lerp(currentPanY, targetPan, Time.deltaTime * lookAheadSpeed);
            }
        }
        else
        {
            verticalHoldTimer = 0f;
            currentPanY = Mathf.Lerp(currentPanY, 0f, Time.deltaTime * lookAheadSpeed);
        }
    }

    /// <summary>
    /// สั่งให้กล้องสั่นสะเทือน (เรียกใช้จากสคริปต์อื่นได้ เช่น CameraController2D.Instance.TriggerShake(0.15f, 0.3f);)
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}