using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> Flame Heart collectible - main quest item. Inherits InteractableBase template.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FlameHeartCollectible : InteractableBase
{
    [Header("Juice VFX")]
    [SerializeField] private GameObject collectVFX;
    [SerializeField] private float bobbingSpeed = 4f;
    [SerializeField] private float bobbingAmount = 0.15f;

    private bool isCollected = false;
    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        float offset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
        transform.position = basePosition + new Vector3(0f, offset, 0f);
    }

    public override void Interact()
    {
        if (isCollected) return;
        isCollected = true;

        if (collectVFX != null)
        {
            Instantiate(collectVFX, transform.position, Quaternion.identity);
        }

        if (FlameHeartLedger.Instance != null)
        {
            FlameHeartLedger.Instance.Deposit(1);
        }

        HidePrompt();
        Destroy(gameObject, 0.1f);
    }
}
