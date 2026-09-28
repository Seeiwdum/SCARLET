using UnityEngine;

namespace Scarlet.Audio
{
    /// <summary>Reusable sound settings. Create one asset for each sound or small sound group.</summary>
    [CreateAssetMenu(menuName = "SCARLET/Audio/Sound Cue", fileName = "Sound Cue")]
    public sealed class SoundCue : ScriptableObject
    {
        [SerializeField] private SoundCategory category = SoundCategory.SFX;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField] private bool loop;
        [SerializeField, Range(0, 256)] private int priority = 128;
        [SerializeField, Min(1)] private int maxConcurrent = 4;
        [SerializeField, Min(0f)] private float minRetriggerSeconds;
        [SerializeField] private bool useDistanceVolume;
        [SerializeField, Min(0f)] private float fullDistance = 5f;
        [SerializeField, Min(0.01f)] private float silentDistance = 25f;

        public SoundCategory Category => category;
        public bool Loop => loop;
        public float Volume => Mathf.Clamp01(volume);
        public int Priority => Mathf.Clamp(priority, 0, 256);
        public int MaxConcurrent => Mathf.Max(1, maxConcurrent);
        public float MinRetriggerSeconds => Mathf.Max(0f, minRetriggerSeconds);
        public bool UsesDistanceVolume => useDistanceVolume;
        public float FullDistance => Mathf.Max(0f, fullDistance);
        public float SilentDistance => Mathf.Max(FullDistance + 0.01f, silentDistance);
        public Vector2 PitchRange => new Vector2(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));

        /// <summary>Chooses one assigned clip at random. Returns null when none are assigned.</summary>
        public AudioClip ChooseClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            int start = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip clip = clips[(start + offset) % clips.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            pitchRange.x = Mathf.Clamp(pitchRange.x, 0.1f, 3f);
            pitchRange.y = Mathf.Clamp(pitchRange.y, 0.1f, 3f);
            priority = Mathf.Clamp(priority, 0, 256);
            maxConcurrent = Mathf.Max(1, maxConcurrent);
            minRetriggerSeconds = Mathf.Max(0f, minRetriggerSeconds);
            fullDistance = Mathf.Max(0f, fullDistance);
            silentDistance = Mathf.Max(fullDistance + 0.01f, silentDistance);
        }
    }
}
