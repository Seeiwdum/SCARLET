using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Hitbox GameObjects")]
    [SerializeField] private GameObject sideHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;
    [SerializeField] private GameObject flameBurstHitbox;
    [SerializeField] private float hitboxActiveTime = 0.15f;

    [Header("Attack Settings")]
    [SerializeField] private int baseAttackDamage = 1;
    [SerializeField] private float attackRate = 3.5f;

    [Header("3-Hit Combo & Sheathe Settings")]
    [SerializeField] private float comboResetTime = 0.8f;
    [Tooltip("ระยะเวลาหลังจากหยุดโจมตี ก่อนที่ดาบจะสลายไป")]
    [SerializeField] private float sheatheDelay = 1.5f; 
    private int currentComboStep = 1;
    private float lastAttackTime = 0f;
    private bool isWeaponDrawn = false;

    [Header("Pure Flame Gauge Settings")]
    [SerializeField] private float maxFlameEnergy = 100f;
    [SerializeField] private float currentFlameEnergy = 100f;
    [SerializeField] private float flameDrainRate = 5f;
    [SerializeField] private float flameRegenRate = 8f;
    [SerializeField] private float energyGainOnHit = 10f;
    [SerializeField] private KeyCode pureFlameKey = KeyCode.F;
    [SerializeField] private KeyCode flameSkillKey = KeyCode.E;
    [SerializeField] private float skillEnergyCost = 30f;

    [Header("Pure Flame Buffs")]
    [SerializeField] private float flameModeDamageMultiplier = 1.8f;
    [SerializeField] private float moveSpeedBuffMultiplier = 1.35f;

    // Components
    private bool isFlameActive = false;
    private float nextAttackTime = 0f;
    private PlayerController2D playerController;
    private Animator anim;

    public float CurrentFlameEnergy => currentFlameEnergy;
    public float MaxFlameEnergy => maxFlameEnergy;
    public int CurrentDamage => Mathf.RoundToInt(baseAttackDamage * (isFlameActive ? flameModeDamageMultiplier : 1f));
    public bool IsPureFlameMode => isFlameActive;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        DisableAllHitboxes();
    }

    private void Update()
    {
        HandlePureFlameMode();
        HandleComboAndSheatheTimer();
        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Z))
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
    }

    private void PerformAttack()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        DisableAllHitboxes();
        lastAttackTime = Time.time;

        // สั่งชักดาบ / เล่นสถานะถือดาบ
        if (!isWeaponDrawn)
        {
            isWeaponDrawn = true;
            if (anim != null) anim.SetBool("IsWeaponDrawn", true);
        }

        if (verticalInput > 0.1f && upHitbox != null)
        {
            if (anim != null) anim.SetTrigger("AttackUp");
            StartCoroutine(ActivateHitboxRoutine(upHitbox));
        }
        else if (verticalInput < -0.1f && !playerController.IsGrounded() && downHitbox != null)
        {
            if (anim != null) anim.SetTrigger("AttackDown");
            StartCoroutine(ActivateHitboxRoutine(downHitbox));
        }
        else if (sideHitbox != null)
        {
            if (anim != null)
            {
                anim.SetInteger("ComboStep", currentComboStep);
                anim.SetTrigger("AttackSide");
            }
            StartCoroutine(ActivateHitboxRoutine(sideHitbox));
            currentComboStep = (currentComboStep % 3) + 1;
            
            // Forward Thrust (Game Feel) - ขยับไปข้างหน้าเล็กน้อยตอนโจมตี
            if (playerController.IsGrounded())
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float thrustForce = 5f;
                    Vector2 thrustDir = playerController.IsFacingRight ? Vector2.right : Vector2.left;
                    rb.linearVelocity = new Vector2(thrustDir.x * thrustForce, rb.linearVelocity.y);
                }
            }
        }
    }

    private void HandleComboAndSheatheTimer()
    {
        // รีเซ็ตสเต็ปคอมโบ
        if (Time.time - lastAttackTime > comboResetTime && currentComboStep != 1)
        {
            currentComboStep = 1;
            if (anim != null) anim.SetInteger("ComboStep", 1);
        }

        // เช็กเวลาเพื่อสลายดาบ
        if (isWeaponDrawn && (Time.time - lastAttackTime > sheatheDelay))
        {
            isWeaponDrawn = false;
            if (anim != null)
            {
                anim.SetBool("IsWeaponDrawn", false);
                anim.SetTrigger("SheatheSword"); // เล่นแอนิเมชันดาบสลาย
            }
            Debug.Log("<color=grey>[SWORD] ดาบสลายกลับไปแล้ว...</color>");
        }
    }

    private void HandlePureFlameMode()
    {
        if (Input.GetKeyDown(pureFlameKey))
        {
            if (!isFlameActive && currentFlameEnergy > 0 && !playerController.IsOverheated)
            {
                ActivatePureFlameMode();
            }
            else if (isFlameActive)
            {
                DeactivatePureFlameMode(false);
            }
        }

        if (isFlameActive)
        {
            currentFlameEnergy -= flameDrainRate * Time.deltaTime;
            currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);

            if (currentFlameEnergy <= 0f)
            {
                DeactivatePureFlameMode(true);
            }

            if (Input.GetKeyDown(flameSkillKey))
            {
                UseFlameBurstSkill();
            }
        }
        else
        {
            if (currentFlameEnergy < maxFlameEnergy)
            {
                currentFlameEnergy += flameRegenRate * Time.deltaTime;
                currentFlameEnergy = Mathf.Clamp(currentFlameEnergy, 0f, maxFlameEnergy);
            }
        }
    }

    private void ActivatePureFlameMode()
    {
        isFlameActive = true;
        playerController.ApplySpeedBuff(moveSpeedBuffMultiplier);
        if (anim != null) anim.SetBool("IsPureFlame", true);
        Debug.Log($"<color=orange>[PURE FLAME] 💥 เปิดโหมดไฟ!</color>");
    }

    private void DeactivatePureFlameMode(bool isOverheated)
    {
        isFlameActive = false;
        playerController.RemoveSpeedBuff();
        if (anim != null) anim.SetBool("IsPureFlame", false);

        if (isOverheated)
        {
            playerController.ApplyOverheatPenalty(2f, 0.5f);
        }
    }

    private void UseFlameBurstSkill()
    {
        if (currentFlameEnergy >= skillEnergyCost)
        {
            currentFlameEnergy -= skillEnergyCost;
            if (anim != null) anim.SetTrigger("FlameBurst");
            if (flameBurstHitbox != null) StartCoroutine(ActivateHitboxRoutine(flameBurstHitbox));
            CameraController2D.Instance?.TriggerShake(0.25f, 0.5f);
        }
    }

    private IEnumerator ActivateHitboxRoutine(GameObject targetHitbox)
    {
        targetHitbox.SetActive(true);
        yield return new WaitForSeconds(hitboxActiveTime);
        targetHitbox.SetActive(false);
    }

    private void DisableAllHitboxes()
    {
        if (sideHitbox != null) sideHitbox.SetActive(false);
        if (upHitbox != null) upHitbox.SetActive(false);
        if (downHitbox != null) downHitbox.SetActive(false);
        if (flameBurstHitbox != null) flameBurstHitbox.SetActive(false);
    }

    public void AddFlameEnergyOnHit()
    {
        currentFlameEnergy = Mathf.Clamp(currentFlameEnergy + energyGainOnHit, 0f, maxFlameEnergy);
    }
}