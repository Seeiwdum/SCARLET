using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> Bonfire - heal point. Inherits InteractableBase template.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Bonfire : InteractableBase
{
    [Header("Bonfire Settings")]
    [SerializeField] private int healAmount = 5;

    public override void Interact()
    {
        Debug.Log("<color=orange>[BONFIRE] นั่งพักผ่อนที่กองไฟ...</color>");

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        HidePrompt();
    }
}
