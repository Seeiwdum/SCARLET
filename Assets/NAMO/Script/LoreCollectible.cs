using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LoreCollectible : MonoBehaviour, IInteractable
{
    [Header("Lore Data")]
    [SerializeField] private LoreData loreData;

    [Header("Dynamic Prompt")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 targetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float animationSpeed = 8f;

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (promptSprite != null)
        {
            SetPromptAlpha(0f); // ตอนนี้มีฟังก์ชันรองรับแล้วครับ
            promptSprite.transform.localScale = Vector3.zero;
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.W))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (loreData != null)
        {
            GameEvents.OnLoreOpened?.Invoke(loreData);
            AnimatePrompt(false); // ซ่อนปุ่ม W ตอนกำลังอ่าน
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            AnimatePrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            AnimatePrompt(false);
        }
    }

    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        StopAllCoroutines();
        StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? targetScale : Vector3.zero;

        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.01f)
        {
            // ใช้ Lerp เพื่อให้ปุ่ม W ค่อยๆ เด้งขึ้นมา
            Color c = promptSprite.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * animationSpeed);
            promptSprite.color = c;

            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (animationSpeed * 1.2f));
            yield return null;
        }
        
        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

    // ฟังก์ชันที่ขาดหายไป เพิ่มเข้ามาตรงนี้ครับ
    private void SetPromptAlpha(float alpha)
    {
        if (promptSprite != null)
        {
            Color c = promptSprite.color;
            c.a = alpha;
            promptSprite.color = c;
        }
    }
}