using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bonfire : MonoBehaviour, IInteractable
{
    [Header("Bonfire Settings")]
    [SerializeField] private int healAmount = 5;
    [SerializeField] private KeyCode interactKey = KeyCode.W; // ปุ่มสำหรับกดสำรวจ

    [Header("Dynamic Prompt (UI นูนขึ้น & Fade)")]
    [Tooltip("ลาก Sprite รูปปุ่ม W หรือรูปลูกศรมาใส่ช่องนี้")]
    [SerializeField] private SpriteRenderer promptSprite; 
    [SerializeField] private Vector3 targetScale = new Vector3(1.2f, 1.2f, 1f); // ขนาดตอนนูนสุด
    [SerializeField] private float animationSpeed = 8f; // ความเร็วในการเด้ง/Fade

    private bool isPlayerInRange = false;
    private Coroutine promptCoroutine;

    private void Start()
    {
        // ซ่อนปุ่มไว้ก่อนตอนเริ่มเกม
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }
    }

    private void Update()
    {
        // ถ้ายืนอยู่ในระยะ และผู้เล่นกดปุ่ม W ให้ทำงาน!
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // ทำตามสัญญาของ IInteractable
    public void Interact()
    {
        Debug.Log("<color=orange>[BONFIRE] นั่งพักผ่อนที่กองไฟ...</color>");
        
        // หา PlayerHealth และสั่ง Heal
        // หมายเหตุ: โค้ดจะฉลาดขึ้นถ้าส่ง Event บอก GameManager ให้ฮีล/เซฟเกม 
        // แต่ตอนนี้เราดึง PlayerHealth ในฉากมาใช้ก่อนได้ครับ
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        // TODO: เรียก Event สั่งเซฟเกม / รีเซ็ตมอนสเตอร์
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            AnimatePrompt(true); // สั่งปุ่มให้โผล่ขึ้นมา
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            AnimatePrompt(false); // สั่งปุ่มให้หดกลับไป
        }
    }

    #region Dynamic Prompt Animation (Juice!)
    
    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;

        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? targetScale : Vector3.zero;

        // วนลูปสมูทค่า Alpha และ Scale (สร้างเอฟเฟกต์ Fade-in และนูนขึ้น)
        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.01f)
        {
            // สมูทความโปร่งใส (Fade)
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * animationSpeed));
            
            // สมูทขนาด (Scale / นูนขึ้น)
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (animationSpeed * 1.2f));

            yield return null; // รอเฟรมถัดไป
        }

        // ทำให้ค่าเป๊ะ 100% ตอนจบอนิเมชัน
        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

    private void SetPromptAlpha(float alpha)
    {
        Color c = promptSprite.color;
        c.a = alpha;
        promptSprite.color = c;
    }

    #endregion
}