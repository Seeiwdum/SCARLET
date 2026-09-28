using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scarlet.Audio
{
    /// <summary>Connects menu sliders and mute toggles to SoundManager.</summary>
    public sealed class AudioSettingsUI : MonoBehaviour
    {
        [Header("Master")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Toggle masterMuteToggle;

        [Header("Category controls, order: BGM, SFX, UI, Ambience")]
        [SerializeField] private Slider[] categorySliders = new Slider[4];
        [SerializeField] private Toggle[] categoryMuteToggles = new Toggle[4];

        [Header("Optional menu sounds")]
        [SerializeField] private SoundCue clickCue;
        [SerializeField] private SoundCue hoverCue;
        [SerializeField] private SoundCue backCue;

        private static readonly SoundCategory[] Categories =
        {
            SoundCategory.BGM, SoundCategory.SFX, SoundCategory.UI, SoundCategory.Ambience
        };

        private readonly List<SliderBinding> sliderBindings = new List<SliderBinding>();
        private readonly List<ToggleBinding> toggleBindings = new List<ToggleBinding>();
        private Coroutine bindRoutine;
        private SoundManager boundManager;

        private struct SliderBinding
        {
            public Slider Target;
            public UnityEngine.Events.UnityAction<float> Callback;
        }

        private struct ToggleBinding
        {
            public Toggle Target;
            public UnityEngine.Events.UnityAction<bool> Callback;
        }

        private void OnEnable()
        {
            bindRoutine = StartCoroutine(WaitForManager());
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }

            UnbindControls();
            if (SoundManager.Instance != null)
                Save();
        }

        private IEnumerator WaitForManager()
        {
            while (SoundManager.Instance == null || !SoundManager.Instance.IsReady)
                yield return null;

            UnbindControls();
            boundManager = SoundManager.Instance;
            LoadControlsFromManager();
            BindControls();
            bindRoutine = null;
        }

        private void LoadControlsFromManager()
        {
            if (masterSlider != null)
                masterSlider.SetValueWithoutNotify(boundManager.GetMasterVolume());
            if (masterMuteToggle != null)
                masterMuteToggle.SetIsOnWithoutNotify(boundManager.GetMasterMuted());

            for (int i = 0; i < Categories.Length; i++)
            {
                if (categorySliders != null && i < categorySliders.Length && categorySliders[i] != null)
                    categorySliders[i].SetValueWithoutNotify(boundManager.GetCategoryVolume(Categories[i]));
                if (categoryMuteToggles != null && i < categoryMuteToggles.Length && categoryMuteToggles[i] != null)
                    categoryMuteToggles[i].SetIsOnWithoutNotify(boundManager.GetCategoryMuted(Categories[i]));
            }
        }

        private void BindControls()
        {
            if (masterSlider != null)
            {
                UnityEngine.Events.UnityAction<float> callback = OnMasterVolumeChanged;
                masterSlider.onValueChanged.AddListener(callback);
                sliderBindings.Add(new SliderBinding { Target = masterSlider, Callback = callback });
            }
            if (masterMuteToggle != null)
            {
                UnityEngine.Events.UnityAction<bool> callback = OnMasterMuteChanged;
                masterMuteToggle.onValueChanged.AddListener(callback);
                toggleBindings.Add(new ToggleBinding { Target = masterMuteToggle, Callback = callback });
            }

            for (int i = 0; i < Categories.Length; i++)
            {
                int categoryIndex = i;
                if (categorySliders != null && i < categorySliders.Length && categorySliders[i] != null)
                {
                    UnityEngine.Events.UnityAction<float> callback = value => OnCategoryVolumeChanged(categoryIndex, value);
                    categorySliders[i].onValueChanged.AddListener(callback);
                    sliderBindings.Add(new SliderBinding { Target = categorySliders[i], Callback = callback });
                }
                if (categoryMuteToggles != null && i < categoryMuteToggles.Length && categoryMuteToggles[i] != null)
                {
                    UnityEngine.Events.UnityAction<bool> callback = muted => OnCategoryMuteChanged(categoryIndex, muted);
                    categoryMuteToggles[i].onValueChanged.AddListener(callback);
                    toggleBindings.Add(new ToggleBinding { Target = categoryMuteToggles[i], Callback = callback });
                }
            }
        }

        private void UnbindControls()
        {
            for (int i = 0; i < sliderBindings.Count; i++)
            {
                SliderBinding binding = sliderBindings[i];
                if (binding.Target != null)
                    binding.Target.onValueChanged.RemoveListener(binding.Callback);
            }
            for (int i = 0; i < toggleBindings.Count; i++)
            {
                ToggleBinding binding = toggleBindings[i];
                if (binding.Target != null)
                    binding.Target.onValueChanged.RemoveListener(binding.Callback);
            }
            sliderBindings.Clear();
            toggleBindings.Clear();
            boundManager = null;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (boundManager != null) boundManager.SetMasterVolume(value);
        }

        private void OnMasterMuteChanged(bool muted)
        {
            if (boundManager != null) boundManager.SetMasterMuted(muted);
        }

        private void OnCategoryVolumeChanged(int index, float value)
        {
            if (boundManager != null && index >= 0 && index < Categories.Length)
                boundManager.SetCategoryVolume(Categories[index], value);
        }

        private void OnCategoryMuteChanged(int index, bool muted)
        {
            if (boundManager != null && index >= 0 && index < Categories.Length)
                boundManager.SetCategoryMuted(Categories[index], muted);
        }

        // These methods can be connected to Button or EventTrigger callbacks in the Inspector.
        public void PlayClickSound() => PlayCue(clickCue);
        public void PlayHoverSound() => PlayCue(hoverCue);
        public void PlayBackSound() => PlayCue(backCue);
        public void PlayCue(SoundCue cue) => PlayMenuSound(cue);
        public void Save()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.SaveSettings();
        }

        private void PlayMenuSound(SoundCue cue)
        {
            if (cue != null && SoundManager.Instance != null && SoundManager.Instance.IsReady)
            {
                if (cue.Category != SoundCategory.UI)
                {
                    Debug.LogWarning("AudioSettingsUI expects menu sounds to use the UI category.", this);
                    return;
                }
                SoundManager.Instance.Play(cue, this);
            }
        }
    }
}
