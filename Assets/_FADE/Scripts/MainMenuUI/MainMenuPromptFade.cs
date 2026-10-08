using System.Collections;
using UnityEngine;

namespace Scarlet.UI
{
    /// <summary>Runs a reusable fade loop for a menu prompt.</summary>
    public sealed class MainMenuPromptFade : MonoBehaviour
    {
        [SerializeField] private CanvasGroup target;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.8f;
        [SerializeField, Min(0f)] private float hiddenDuration = 0.25f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.8f;
        [SerializeField, Min(0f)] private float visibleDuration = 0.25f;

        private Coroutine loop;
        private float shownAlpha = 1f;
        private bool visible = true;
        private bool initialized;

        private void Awake() => EnsureInitialized();

        private bool EnsureInitialized()
        {
            if (initialized) return target != null;
            if (target == null) target = GetComponent<CanvasGroup>();
            if (target == null)
            {
                Debug.LogWarning("MainMenuPromptFade needs a CanvasGroup on its target.", this);
                enabled = false;
                return false;
            }
            shownAlpha = target.alpha;
            initialized = true;
            return true;
        }

        private void OnEnable()
        {
            if (!initialized) return;
            if (visible) StartLoop();
            else target.alpha = 0f;
        }

        private void OnDisable()
        {
            StopLoop();
            if (target != null) target.alpha = visible ? shownAlpha : 0f;
        }

        /// <summary>Shows and starts the prompt loop, or hides it immediately and stops the loop.</summary>
        public void SetVisible(bool shouldShow)
        {
            if (!EnsureInitialized()) return;
            if (visible == shouldShow && (!shouldShow || loop != null)) return;
            visible = shouldShow;
            if (!shouldShow)
            {
                StopLoop();
                target.alpha = 0f;
            }
            else if (isActiveAndEnabled)
            {
                target.alpha = shownAlpha;
                StartLoop();
            }
        }

        private void StartLoop()
        {
            if (loop == null) loop = StartCoroutine(FadeLoop());
        }

        private void StopLoop()
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
        }

        private IEnumerator FadeLoop()
        {
            while (visible)
            {
                yield return FadeTo(0f, fadeOutDuration);
                if (hiddenDuration > 0f) yield return new WaitForSecondsRealtime(hiddenDuration);
                yield return FadeTo(shownAlpha, fadeInDuration);
                if (visibleDuration > 0f) yield return new WaitForSecondsRealtime(visibleDuration);
                if (fadeOutDuration <= 0f && fadeInDuration <= 0f && hiddenDuration <= 0f && visibleDuration <= 0f)
                    yield return null;
            }
            loop = null;
        }

        private IEnumerator FadeTo(float destination, float duration)
        {
            float start = target.alpha;
            if (duration <= 0f) { target.alpha = destination; yield break; }
            float elapsed = 0f;
            while (elapsed < duration && visible)
            {
                elapsed += Time.unscaledDeltaTime;
                target.alpha = Mathf.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (visible) target.alpha = destination;
        }
    }
}

