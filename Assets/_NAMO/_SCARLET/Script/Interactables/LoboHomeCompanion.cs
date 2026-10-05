using System.Collections;
using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> LOBO companion - pet interaction with hood reaction. Inherits InteractableBase template.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LoboHomeCompanion : InteractableBase
{
    [Header("Visual & Animation")]
    [SerializeField] private SpriteRenderer loboRenderer;
    [SerializeField] private GameObject loveHeartVFXPrefab;
    [SerializeField] private GameObject alertVFXPrefab;

    [Header("Interaction Settings")]
    [SerializeField] private float petCooldown = 2.0f;
    [SerializeField] private int healAmountOnPet = 1;

    private bool canInteract = true;
    private PlayerHood playerHood;
    private PlayerHealth playerHealth;

    private void Start()
    {
        playerHood = FindFirstObjectByType<PlayerHood>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    public override void Interact()
    {
        if (!canInteract) return;

        bool isHooded = playerHood != null && playerHood.IsWearingHood;

        if (isHooded)
        {
            StartCoroutine(WearyReactionRoutine());
        }
        else
        {
            StartCoroutine(HappyPetRoutine());
        }
    }

    private IEnumerator HappyPetRoutine()
    {
        canInteract = false;

        if (loveHeartVFXPrefab != null)
        {
            Instantiate(loveHeartVFXPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        Vector3 origScale = transform.localScale;
        transform.localScale = new Vector3(origScale.x * 1.2f, origScale.y * 0.8f, origScale.z);
        yield return new WaitForSeconds(0.12f);
        transform.localScale = new Vector3(origScale.x * 0.9f, origScale.y * 1.2f, origScale.z);
        yield return new WaitForSeconds(0.12f);
        transform.localScale = origScale;

        if (playerHealth != null)
        {
            playerHealth.Heal(healAmountOnPet);
        }

        yield return new WaitForSeconds(petCooldown);
        canInteract = true;
    }

    private IEnumerator WearyReactionRoutine()
    {
        canInteract = false;

        if (alertVFXPrefab != null)
        {
            Instantiate(alertVFXPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        Vector3 origPos = transform.position;
        for (int i = 0; i < 6; i++)
        {
            transform.position = origPos + (Vector3)(Random.insideUnitCircle * 0.05f);
            yield return new WaitForSeconds(0.04f);
        }
        transform.position = origPos;

        yield return new WaitForSeconds(petCooldown);
        canInteract = true;
    }
}
