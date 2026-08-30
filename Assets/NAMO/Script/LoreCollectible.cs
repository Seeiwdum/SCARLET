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
    private bool isCurrentlyReading = false;
    private Coroutine promptCoroutine;

    private void Awake()
    {
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnLoreOpened += HandleLoreOpened;
        GameEvents.OnLoreClosed += HandleLoreClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnLoreOpened -= HandleLoreOpened;
        GameEvents.OnLoreClosed -= HandleLoreClosed;
    }

    private void HandleLoreOpened(LoreData data)
    {
        isCurrentlyReading = true;
        AnimatePrompt(false);
    }

    private void HandleLoreClosed()
    {
        isCurrentlyReading = false;
        if (isPlayerInRange) AnimatePrompt(true);
    }

    private void Update()
    {
        if (isPlayerInRange && !isCurrentlyReading && Input.GetKeyDown(KeyCode.W))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (loreData != null && !isCurrentlyReading)
        {
            GameEvents.OnLoreOpened?.Invoke(loreData);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (!isCurrentlyReading) AnimatePrompt(true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPlayerInRange)
        {
            isPlayerInRange = true;
            if (!isCurrentlyReading) AnimatePrompt(true);
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
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.unscaledDeltaTime * animationSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.unscaledDeltaTime * (animationSpeed * 1.2f));
            yield return null;
        }

        SetPromptAlpha(targetAlpha);
        promptSprite.transform.localScale = finalScale;
    }

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