using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlameHeartCollectible : MonoBehaviour, IInteractable
{
    [Header("Dynamic Prompt (W Key)")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 targetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float animationSpeed = 8f;

    [Header("Juice VFX")]
    [SerializeField] private GameObject collectVFX;
    [SerializeField] private float bobbingSpeed = 4f;
    [SerializeField] private float bobbingAmount = 0.15f;

    private bool isPlayerInRange = false;
    private bool isCollected = false;
    private Vector3 basePosition;
    private Coroutine promptCoroutine;

    private void Start()
    {
        basePosition = transform.position;
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // ขยับลอยขึ้นลงเบาๆ
        float offset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        transform.position = basePosition + new Vector3(0f, offset, 0f);

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.W))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (isCollected) return;
        isCollected = true;

        if (collectVFX != null)
        {
            Instantiate(collectVFX, transform.position, Quaternion.identity);
        }

        // เพิ่มคะแนนเควสต์ 1 ดวง
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CollectFlameHeart();
        }

        AnimatePrompt(false);
        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
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
        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? targetScale : Vector3.zero;

        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.02f)
        {
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * animationSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (animationSpeed * 1.2f));
            yield return null;
        }

        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

    private void SetPromptAlpha(float alpha)
    {
        Color c = promptSprite.color;
        c.a = alpha;
        promptSprite.color = c;
    }
}