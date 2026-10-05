using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> Lore collectible - ancient notes. Inherits InteractableBase template.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LoreCollectible : InteractableBase
{
    [Header("Lore Data")]
    [SerializeField] private LoreData loreData;

    private bool isCurrentlyReading = false;

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
        HidePrompt();
    }

    private void HandleLoreClosed()
    {
        isCurrentlyReading = false;
        if (IsPlayerInRange()) ShowPrompt();
    }

    public override void Interact()
    {
        if (loreData != null && !isCurrentlyReading)
        {
            GameEvents.OnLoreOpened?.Invoke(loreData);
        }
    }
}
