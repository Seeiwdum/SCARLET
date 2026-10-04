using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerHood : MonoBehaviour
{
    [Header("Key Bindings")]
    [SerializeField] private KeyCode toggleHoodKey = KeyCode.C;

    [Header("Hood Status")]
    [SerializeField] private PlayerState.Hood currentHood = PlayerState.Hood.OFF;
    [SerializeField] private bool isInSafeZone = false;

    private Animator anim;
    private float currentHoodWeight = 0f;
    private float blendSpeed = 10f;

    public PlayerState.Hood CurrentHood => currentHood;
    public bool IsWearingHood => currentHood == PlayerState.Hood.ON;
    public bool IsInSafeZone => isInSafeZone;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // รับ Input สลับการใส่/ถอดฮู้ด
        if (Input.GetKeyDown(toggleHoodKey))
        {
            ToggleHood();
        }

        // ค่อยๆ Lerp ค่าแกน Y (0 ถึง 1) ส่งเข้า Blend Tree เพื่อความนุ่มนวล
        float targetWeight = isWearingHood ? 1f : 0f;
        currentHoodWeight = Mathf.Lerp(currentHoodWeight, targetWeight, Time.deltaTime * blendSpeed);

        if (anim != null)
        {
            anim.SetFloat("HoodState", currentHoodWeight);
            anim.SetBool("IsWearingHood", isWearingHood);
        }
    }

    public void ToggleHood()
    {
        currentHood = currentHood == PlayerState.Hood.ON ? PlayerState.Hood.OFF : PlayerState.Hood.ON;
        GameEvents.OnHoodToggled?.Invoke(IsWearingHood);
        Debug.Log($"<color=magenta>[HOOD] {(IsWearingHood ? "สวมฮู้ดผ้าคลุมแดง 🧥" : "ถอดฮู้ด 🧒")}</color>");
    }

    public void SetSafeZone(bool isSafe)
    {
        isInSafeZone = isSafe;
    }

    /// <summary>
    /// ตรวจสอบว่าได้รับการคุ้มกันจากพิษหรือไม่ (สวมฮู้ด หรือ อยู่ในหมู่บ้านใต้ดิน)
    /// </summary>
    public bool IsProtectedFromPoison()
    {
        return isWearingHood || isInSafeZone;
    }
}