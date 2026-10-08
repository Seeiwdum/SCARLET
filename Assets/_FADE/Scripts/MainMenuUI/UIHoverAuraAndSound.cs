using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scarlet.UI
{
    /// <summary>Shows an optional hover visual and plays SCARLET UI cues on a Button.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIHoverAuraAndSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject hoverVisual;
        [SerializeField] private Scarlet.Audio.SoundCue hoverCue;
        [SerializeField] private Scarlet.Audio.SoundCue clickCue;
        private Button button;
        private readonly List<Graphic> graphics = new List<Graphic>();
        private readonly List<bool> originalRaycastTargets = new List<bool>();
        private CanvasGroup auraCanvasGroup;
        private bool originalBlocksRaycasts;
        private bool originalInteractable;
        private bool hovering;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (hoverVisual == null) return;
            if (hoverVisual == gameObject || transform.IsChildOf(hoverVisual.transform))
            {
                Debug.LogWarning("Hover visual must not be this Button or one of its parents.", this);
                hoverVisual = null;
                return;
            }
            hoverVisual.SetActive(false);
            auraCanvasGroup = hoverVisual.GetComponent<CanvasGroup>();
            if (auraCanvasGroup != null)
            {
                originalBlocksRaycasts = auraCanvasGroup.blocksRaycasts;
                originalInteractable = auraCanvasGroup.interactable;
                auraCanvasGroup.blocksRaycasts = false;
                auraCanvasGroup.interactable = false;
            }
            hoverVisual.GetComponentsInChildren(true, graphics);
            for (int i = 0; i < graphics.Count; i++)
            {
                originalRaycastTargets.Add(graphics[i].raycastTarget);
                graphics[i].raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(PlayClickSound);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayClickSound);
            SetHover(false);
        }

        private void OnDestroy()
        {
            if (auraCanvasGroup != null)
            {
                auraCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
                auraCanvasGroup.interactable = originalInteractable;
            }
            for (int i = 0; i < graphics.Count && i < originalRaycastTargets.Count; i++)
                if (graphics[i] != null) graphics[i].raycastTarget = originalRaycastTargets[i];
        }

        private void Update()
        {
            if (hovering && (button == null || !button.IsActive() || !button.IsInteractable())) SetHover(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            SetHover(true);
            PlayCue(hoverCue);
        }

        public void OnPointerExit(PointerEventData eventData) => SetHover(false);

        private void SetHover(bool visible)
        {
            hovering = visible;
            if (hoverVisual != null) hoverVisual.SetActive(visible);
        }

        private void PlayClickSound() => PlayCue(clickCue);

        private void PlayCue(Scarlet.Audio.SoundCue cue)
        {
            Scarlet.Audio.SoundManager manager = Scarlet.Audio.SoundManager.Instance;
            if (cue == null || manager == null || !manager.IsReady) return;
            if (cue.Category != Scarlet.Audio.SoundCategory.UI)
            {
                Debug.LogWarning("Menu hover and click cues must use the UI category.", this);
                return;
            }
            manager.Play(cue);
        }
    }
}

