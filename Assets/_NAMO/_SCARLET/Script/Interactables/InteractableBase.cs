using System.Collections;
using UnityEngine;

/// <summary>
/// <<Abstract>> <<MonoBehaviour>> Template for all IInteractable domain objects.
/// Eliminates duplication of prompt animation + trigger plumbing per tech lead.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Prompt")]
    [SerializeField] protected SpriteRenderer promptSprite;
    [SerializeField] protected Vector3 targetScale = Vector3.one;
    [SerializeField] protected float animationSpeed = 8f;
    [SerializeField] protected KeyCode interactKey = KeyCode.W;

    protected bool isPlayerInRange = false;
    protected Coroutine promptCoroutine;

    // Domain operation - subclasses implement concrete behavior
    public abstract void Interact();

    // Domain queries
    public bool IsPlayerInRange() => isPlayerInRange;
    public bool Contains(Collider2D col) => col != null && col.CompareTag("Player");

    // Domain operation: show/hide prompt with animation
    public void ShowPrompt() => AnimatePrompt(true);
    public void HidePrompt() => AnimatePrompt(false);

    protected void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? targetScale : Vector3.zero;
        while (promptSprite != null && Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.01f)
        {
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * animationSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (animationSpeed * 1.2f));
            yield return null;
        }
        SetPromptAlpha(targetAlpha);
        if (promptSprite != null) promptSprite.transform.localScale = finalScale;
    }

    protected void SetPromptAlpha(float alpha)
    {
        if (promptSprite == null) return;
        Color c = promptSprite.color;
        c.a = alpha;
        promptSprite.color = c;
    }

    // Protected trigger plumbing kept as domain boundary (not exposed in diagram per tech lead point 4,
    // but retained here as concrete MonoBehaviour wiring to call domain methods)
}
