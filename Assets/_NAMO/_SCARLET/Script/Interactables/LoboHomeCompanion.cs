using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LoboHomeCompanion : MonoBehaviour, IInteractable
{
    [Header("Visual & Animation")]
    [SerializeField] private SpriteRenderer loboRenderer;
    [SerializeField] private GameObject loveHeartVFXPrefab;
    [SerializeField] private GameObject alertVFXPrefab;

    [Header("Dynamic Prompt (ปุ่ม W เหนือหัว)")]
    [SerializeField] private SpriteRenderer promptSprite;
    [SerializeField] private Vector3 promptTargetScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float promptAnimSpeed = 8f;

    [Header("Interaction Settings")]
    [SerializeField] private float petCooldown = 2.0f;
    [SerializeField] private int healAmountOnPet = 1;

    private bool isPlayerInRange = false;
    private bool canInteract = true;
    private PlayerHood playerHood;
    private PlayerHealth playerHealth;
    private Coroutine promptCoroutine;

    private void Start()
    {
        if (promptSprite != null)
        {
            SetPromptAlpha(0f);
            promptSprite.transform.localScale = Vector3.zero;
        }

        playerHood = FindFirstObjectByType<PlayerHood>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (isPlayerInRange && canInteract && Input.GetKeyDown(KeyCode.W))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (!canInteract) return;

        bool isHooded = playerHood != null && playerHood.IsWearingHood;

        if (isHooded)
        {
            // ปฏิกิริยาเมื่อสวมผ้าคลุมแดง: LOBO หวาดกลัว/ถอยหนี
            StartCoroutine(WearyReactionRoutine());
        }
        else
        {
            // ปฏิกิริยาปกติเมื่อถอดฮู้ด: ดีใจ ได้รับความรัก ฟื้นฟูเลือด
            StartCoroutine(HappyPetRoutine());
        }
    }

    private IEnumerator HappyPetRoutine()
    {
        canInteract = false;

        // สปอนหัวใจฟุ้งขึ้นเหนือหัว LOBO
        if (loveHeartVFXPrefab != null)
        {
            Instantiate(loveHeartVFXPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        // เอฟเฟกต์กระโดดดุ๊กดิ๊ก (Juice Bounce)
        Vector3 origScale = transform.localScale;
        transform.localScale = new Vector3(origScale.x * 1.2f, origScale.y * 0.8f, origScale.z);
        yield return new WaitForSeconds(0.12f);
        transform.localScale = new Vector3(origScale.x * 0.9f, origScale.y * 1.2f, origScale.z);
        yield return new WaitForSeconds(0.12f);
        transform.localScale = origScale;

        // ฟื้นฟูเลือดให้ Scarlet
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

        // สปอนเครื่องหมายตกใจ/ระแวง
        if (alertVFXPrefab != null)
        {
            Instantiate(alertVFXPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        // เอฟเฟกต์ตัวสั่นเทาเล็กน้อย
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

    #region Dynamic Prompt Animation
    private void AnimatePrompt(bool show)
    {
        if (promptSprite == null) return;
        if (promptCoroutine != null) StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(PromptRoutine(show));
    }

    private IEnumerator PromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        Vector3 finalScale = show ? promptTargetScale : Vector3.zero;

        while (Mathf.Abs(promptSprite.color.a - targetAlpha) > 0.02f)
        {
            SetPromptAlpha(Mathf.Lerp(promptSprite.color.a, targetAlpha, Time.deltaTime * promptAnimSpeed));
            promptSprite.transform.localScale = Vector3.Lerp(promptSprite.transform.localScale, finalScale, Time.deltaTime * (promptAnimSpeed * 1.2f));
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
    #endregion
}