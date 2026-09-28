using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Scarlet.Audio
{
    /// <summary>Owns pooled effects, music, sound settings, and short-lived story audio states.</summary>
    public sealed class SoundManager : MonoBehaviour
    {
        private const int CategoryCount = 4;
        [SerializeField, Min(1)] private int uiPoolSize = 6;
        [SerializeField, Min(1)] private int worldPoolSize = 16;
        [SerializeField, Min(1)] private int loopPoolSize = 8;
        private const string SettingsPrefix = "Scarlet.Audio.";

        private static SoundManager instance;
        private static int globalGeneration = 1;

        [Header("Audio routing")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [Header("Temporary story mix")]
        [SerializeField, Range(0f, 1f)] private float loreMusicGain = 0.4f;
        [SerializeField, Range(0f, 1f)] private float loreAmbienceGain = 0.35f;
        [SerializeField, Range(0f, 1f)] private float dialogueMusicGain = 1f;
        [SerializeField, Range(0f, 1f)] private float dialogueAmbienceGain = 1f;

        public static SoundManager Instance => instance;
        public bool IsReady { get; private set; }
        public Transform HearingTarget { get; private set; }
        public SoundCue CurrentMusicCue { get; private set; }

        private readonly List<Playback> active = new List<Playback>();
        private readonly List<AudioSource> uiSources = new List<AudioSource>();
        private readonly List<AudioSource> worldSources = new List<AudioSource>();
        private readonly List<AudioSource> loopSources = new List<AudioSource>();
        private readonly AudioSource[] musicSources = new AudioSource[2];
        private readonly SoundCue[] musicCues = new SoundCue[2];
        private readonly float[] musicGains = new float[2];
        private readonly MusicFade[] musicFades = new MusicFade[2];
        private readonly float[] categoryVolumes = { 1f, 1f, 1f, 1f };
        private readonly bool[] categoryMuted = new bool[CategoryCount];
        private readonly Dictionary<SoundCue, float> lastCueTimes = new Dictionary<SoundCue, float>();
        private readonly HashSet<string> warnedKeys = new HashSet<string>();
        private int nextId = 1;
        private int currentMusicIndex = -1;
        private float masterVolume = 1f;
        private bool masterMuted;
        private bool loreReading;
        private bool dialogueActive;
        private bool defaultLoreReading;
        private bool defaultDialogueActive;
        private bool settingsDirty;
        private bool hasStarted;
        private readonly List<UnityEngine.Object> loreOwners = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> dialogueOwners = new List<UnityEngine.Object>();

        private sealed class Playback
        {
            public int Id;
            public int Generation;
            public SoundCue Cue;
            public AudioSource Source;
            public UnityEngine.Object Owner;
            public Transform FollowTarget;
            public Scene OwnerScene;
            public bool HasOwnerScene;
            public bool IsLoop;
            public bool IsUi;
            public bool WasLorePaused;
            public float BaseGain;
            public float DistanceGain = 1f;
            public float FadeGain = 1f;
            public float FadeStart;
            public float FadeTarget;
            public float FadeElapsed;
            public float FadeDuration;
            public int Priority;
            public float StartedAt;
        }

        private sealed class MusicFade
        {
            public SoundCue Cue;
            public float StartGain;
            public float TargetGain;
            public float Elapsed;
            public float Duration;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateSources();
        }

        private void OnEnable()
        {
            if (instance != this)
                return;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            if (hasStarted)
            {
                ValidateMixerSetup();
                ApplyMixerSettings();
                IsReady = true;
            }
        }

        private void Start()
        {
            if (instance != this)
                return;
            LoadSettings();
            ValidateMixerSetup();
            ApplyMixerSettings();
            hasStarted = true;
            IsReady = true;
        }

        private void OnDisable()
        {
            if (instance != this)
                return;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            IsReady = false;
            SaveSettings();
            for (int i = active.Count - 1; i >= 0; i--)
                ReleasePlayback(active[i]);
            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicSources[i] != null)
                {
                    musicSources[i].Stop();
                    musicSources[i].clip = null;
                }
                musicCues[i] = null;
                musicFades[i] = null;
                musicGains[i] = 0f;
            }
            currentMusicIndex = -1;
            CurrentMusicCue = null;
            defaultLoreReading = defaultDialogueActive = loreReading = dialogueActive = false;
            loreOwners.Clear();
            dialogueOwners.Clear();
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SaveSettings();
            if (instance == this)
                instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveSettings();
        }

        private void OnApplicationQuit()
        {
            SaveSettings();
        }

        private void Update()
        {
            if (instance != this || !IsReady)
                return;
            RefreshContextOwners();
            UpdatePlayback();
            UpdateMusic();
        }

        /// <summary>Sets the transform used for XY distance checks.</summary>
        public void SetHearingTarget(Transform target)
        {
            HearingTarget = target;
        }

        public SoundHandle Play(SoundCue cue, UnityEngine.Object owner = null)
        {
            return PlayInternal(cue, null, null, owner, false);
        }

        public SoundHandle PlayAt(SoundCue cue, Vector3 position, UnityEngine.Object owner = null)
        {
            return PlayInternal(cue, position, null, owner, true);
        }

        public SoundHandle PlayFollowing(SoundCue cue, Transform target, UnityEngine.Object owner = null)
        {
            if (target == null)
            {
                WarnOnce("follow-target-null", "SoundManager.PlayFollowing needs a live target transform.", this);
                return default;
            }

            return PlayInternal(cue, target.position, target, owner != null ? owner : target, true);
        }

        private SoundHandle PlayInternal(SoundCue cue, Vector3? position, Transform followTarget, UnityEngine.Object owner, bool hasPosition)
        {
            if (cue == null)
            {
                Debug.LogWarning("SoundManager ignored a request with no SoundCue.", this);
                return default;
            }

            if (!IsReady)
            {
                WarnOnce("manager-not-ready-effect", "SoundManager is not ready yet. Wait until IsReady is true before playing audio.", this);
                return default;
            }

            AudioClip clip = cue.ChooseClip();
            if (clip == null)
            {
                WarnOnce("empty-cue-" + cue.GetInstanceID(), "SoundCue '" + cue.name + "' has no usable clips.", cue);
                return default;
            }

            if (cue.Category == SoundCategory.BGM)
            {
                WarnOnce("bgm-play-" + cue.GetInstanceID(), "Use PlayMusic for BGM cues so the music fade can be managed.", cue);
                return default;
            }

            bool applyDistance = cue.UsesDistanceVolume && cue.Category != SoundCategory.UI;
            if (applyDistance && !hasPosition)
            {
                WarnOnce("no-position-" + cue.GetInstanceID(), "SoundCue '" + cue.name + "' uses distance volume. Call PlayAt or PlayFollowing so it has a real position.", cue);
                return default;
            }

            if (applyDistance && HearingTarget == null)
            {
                WarnOnce("no-hearing-target-" + cue.GetInstanceID(), "SoundCue '" + cue.name + "' needs SoundManager.SetHearingTarget before distance-based audio can play.", cue);
                return default;
            }

            if (applyDistance && IsBeyondSilentDistance(cue, position.Value))
                return default;

            if (cue.Category == SoundCategory.SFX && loreReading)
                return default;

            if (IsRetriggerBlocked(cue))
                return default;

            int currentForCue = CountActiveCue(cue);
            if (currentForCue >= cue.MaxConcurrent && !TryReplaceCueVictim(cue))
                return default;

            bool isUi = cue.Category == SoundCategory.UI;
            bool isLoop = cue.Loop;
            if (isLoop && ReferenceEquals(owner, null) && followTarget == null)
            {
                WarnOnce("loop-no-owner-" + cue.GetInstanceID(), "Looping SoundCue '" + cue.name + "' needs an owner so it can stop when its owner goes away.", cue);
                return default;
            }
            List<AudioSource> pool = isLoop ? loopSources : (isUi ? uiSources : worldSources);
            AudioSource source = FindFreeSource(pool);
            if (source == null)
            {
                if (isLoop)
                {
                    WarnOnce("loop-pool-full", "SoundManager's loop pool is full. Increase its Loop Pool Size or stop an existing loop.", this);
                    return default;
                }

                if (!TryStealSource(pool, cue.Priority, out source))
                    return default;
            }

            ConfigureSource(source, cue, clip, position, isLoop);
            var playback = new Playback
            {
                Id = nextId++,
                Generation = globalGeneration++,
                Cue = cue,
                Source = source,
                Owner = owner,
                FollowTarget = followTarget,
                IsLoop = isLoop,
                IsUi = isUi,
                BaseGain = cue.Volume,
                Priority = cue.Priority,
                StartedAt = Time.unscaledTime,
                FadeGain = 1f,
                FadeStart = 1f,
                FadeTarget = 1f
            };
            SetOwnerScene(playback, owner, followTarget);
            active.Add(playback);
            UpdateDistanceGain(playback);
            ApplyPlaybackVolume(playback);
            source.Play();
            lastCueTimes[cue] = Time.unscaledTime;
            return new SoundHandle(playback.Id, playback.Generation);
        }

        /// <summary>Stops the matching playback only; stale handles are harmless.</summary>
        public void Stop(SoundHandle handle, float fadeDuration = 0f)
        {
            Playback playback = FindPlayback(handle);
            if (playback == null)
                return;

            if (fadeDuration <= 0f)
            {
                ReleasePlayback(playback);
                return;
            }

            playback.FadeStart = playback.FadeGain;
            playback.FadeTarget = 0f;
            playback.FadeElapsed = 0f;
            playback.FadeDuration = fadeDuration;
        }

        /// <summary>Returns true while this handle still refers to a live playback.</summary>
        public bool IsActive(SoundHandle handle) => FindPlayback(handle) != null;

        /// <summary>Plays, crossfades, or fades out scene music. Passing null fades to silence.</summary>
        public void PlayMusic(SoundCue cue, float fadeDuration = 1f)
        {
            if (!IsReady)
            {
                WarnOnce("manager-not-ready-music", "SoundManager is not ready yet. Wait until IsReady is true before playing music.", this);
                return;
            }

            if (cue != null && cue.Category != SoundCategory.BGM)
            {
                WarnOnce("music-category-" + cue.GetInstanceID(), "PlayMusic requires a SoundCue in the BGM category.", cue);
                return;
            }

            fadeDuration = Mathf.Max(0f, fadeDuration);
            if (cue == null)
            {
                CurrentMusicCue = null;
                for (int i = 0; i < musicSources.Length; i++)
                    FadeMusicSource(i, 0f, fadeDuration);
                currentMusicIndex = -1;
                return;
            }

            if (currentMusicIndex >= 0 && musicCues[currentMusicIndex] == cue && musicSources[currentMusicIndex].isPlaying)
            {
                CurrentMusicCue = cue;
                if (musicFades[currentMusicIndex] != null && musicFades[currentMusicIndex].TargetGain <= 0f)
                    FadeMusicSource(currentMusicIndex, 1f, fadeDuration);
                return;
            }

            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicCues[i] == cue && musicSources[i].isPlaying)
                {
                    CurrentMusicCue = cue;
                    currentMusicIndex = i;
                    FadeMusicSource(i, 1f, fadeDuration);
                    FadeMusicSource(1 - i, 0f, fadeDuration);
                    return;
                }
            }

            AudioClip clip = cue.ChooseClip();
            if (clip == null)
            {
                WarnOnce("empty-music-cue-" + cue.GetInstanceID(), "Music SoundCue '" + cue.name + "' has no usable clips.", cue);
                return;
            }

            CurrentMusicCue = cue;

            int nextIndex = currentMusicIndex < 0 ? 0 : 1 - currentMusicIndex;
            AudioSource next = musicSources[nextIndex];
            next.Stop();
            ConfigureSource(next, cue, clip, null, cue.Loop);
            musicCues[nextIndex] = cue;
            musicGains[nextIndex] = 0f;
            next.volume = 0f;
            next.Play();
            musicFades[nextIndex] = new MusicFade { Cue = cue, StartGain = 0f, TargetGain = 1f, Duration = fadeDuration };
            currentMusicIndex = nextIndex;

            int oldIndex = 1 - nextIndex;
            if (musicSources[oldIndex].isPlaying || musicFades[oldIndex] != null)
                FadeMusicSource(oldIndex, 0f, fadeDuration);
        }

        public float GetCategoryVolume(SoundCategory category) => categoryVolumes[CategoryIndex(category)];
        public bool GetCategoryMuted(SoundCategory category) => categoryMuted[CategoryIndex(category)];

        public void SetCategoryVolume(SoundCategory category, float value)
        {
            categoryVolumes[CategoryIndex(category)] = Mathf.Clamp01(value);
            settingsDirty = true;
            if (IsReady) ApplyMixerSettings();
            RefreshVolumes();
        }

        public void SetCategoryMuted(SoundCategory category, bool muted)
        {
            categoryMuted[CategoryIndex(category)] = muted;
            settingsDirty = true;
            if (IsReady) ApplyMixerSettings();
            RefreshVolumes();
        }

        public float GetMasterVolume() => masterVolume;
        public bool GetMasterMuted() => masterMuted;

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            settingsDirty = true;
            if (IsReady) ApplyMixerSettings();
            RefreshVolumes();
        }

        public void SetMasterMuted(bool muted)
        {
            masterMuted = muted;
            settingsDirty = true;
            if (IsReady) ApplyMixerSettings();
            RefreshVolumes();
        }

        /// <summary>Temporarily softens BGM/ambience and pauses current SFX while lore is read.</summary>
        public void SetLoreReading(bool reading)
        {
            defaultLoreReading = reading;
            RecomputeContexts();
        }

        /// <summary>Sets lore context for one owner, allowing independent adapters to overlap.</summary>
        public void SetLoreReading(UnityEngine.Object owner, bool reading)
        {
            SetContextOwner(loreOwners, owner, reading);
            RecomputeContexts();
        }

        /// <summary>Sets dialogue context for one owner, allowing independent adapters to overlap.</summary>
        public void SetDialogueActive(UnityEngine.Object owner, bool activeDialogue)
        {
            SetContextOwner(dialogueOwners, owner, activeDialogue);
            RecomputeContexts();
        }

        /// <summary>Clears every context registered by one owner.</summary>
        public void ClearContext(UnityEngine.Object owner)
        {
            RemoveContextOwner(loreOwners, owner);
            RemoveContextOwner(dialogueOwners, owner);
            RecomputeContexts();
        }

        private void RecomputeContexts()
        {
            bool nextLoreReading = defaultLoreReading || loreOwners.Count > 0;
            bool nextDialogueActive = defaultDialogueActive || dialogueOwners.Count > 0;
            bool loreChanged = loreReading != nextLoreReading;
            bool dialogueChanged = dialogueActive != nextDialogueActive;
            loreReading = nextLoreReading;
            dialogueActive = nextDialogueActive;
            if (!loreChanged && !dialogueChanged)
                return;

            for (int i = 0; i < active.Count; i++)
            {
                Playback playback = active[i];
                if (playback.Cue.Category != SoundCategory.SFX)
                    continue;

                if (loreReading && playback.Source.isPlaying)
                {
                    playback.Source.Pause();
                    playback.WasLorePaused = true;
                }
                else if (!loreReading && playback.WasLorePaused)
                {
                    playback.Source.UnPause();
                    playback.WasLorePaused = false;
                }
            }

            RefreshVolumes();
        }

        /// <summary>Applies a gentle temporary mix while dialogue is active.</summary>
        public void SetDialogueActive(bool activeDialogue)
        {
            defaultDialogueActive = activeDialogue;
            RecomputeContexts();
        }

        private void RefreshContextOwners()
        {
            bool changed = RemoveUnavailableContextOwners(loreOwners);
            changed |= RemoveUnavailableContextOwners(dialogueOwners);
            if (changed)
                RecomputeContexts();
        }

        private static void SetContextOwner(List<UnityEngine.Object> owners, UnityEngine.Object owner, bool activeContext)
        {
            if (ReferenceEquals(owner, null))
                return;
            if (activeContext)
            {
                if (!ContainsContextOwner(owners, owner))
                    owners.Add(owner);
            }
            else
            {
                RemoveContextOwner(owners, owner);
            }
        }

        private static bool ContainsContextOwner(List<UnityEngine.Object> owners, UnityEngine.Object owner)
        {
            for (int i = 0; i < owners.Count; i++)
                if (ReferenceEquals(owners[i], owner)) return true;
            return false;
        }

        private static void RemoveContextOwner(List<UnityEngine.Object> owners, UnityEngine.Object owner)
        {
            if (ReferenceEquals(owner, null))
                return;
            for (int i = owners.Count - 1; i >= 0; i--)
                if (ReferenceEquals(owners[i], owner)) owners.RemoveAt(i);
        }

        private static bool RemoveUnavailableContextOwners(List<UnityEngine.Object> owners)
        {
            bool changed = false;
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object owner = owners[i];
                if (IsContextOwnerUnavailable(owner))
                {
                    owners.RemoveAt(i);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool IsContextOwnerUnavailable(UnityEngine.Object owner)
        {
            if (owner == null)
                return true;
            if (owner is MonoBehaviour behaviour)
                return !behaviour.enabled || !behaviour.gameObject.activeInHierarchy;
            if (owner is GameObject gameObject)
                return !gameObject.activeInHierarchy;
            if (owner is Component component)
                return !component.gameObject.activeInHierarchy;
            return false;
        }

        public void SaveSettings()
        {
            if (instance != this || !hasStarted)
                return;
            if (!settingsDirty && IsReady)
                return;

            PlayerPrefs.SetFloat(SettingsPrefix + "MasterVolume", masterVolume);
            PlayerPrefs.SetInt(SettingsPrefix + "MasterMuted", masterMuted ? 1 : 0);
            for (int i = 0; i < CategoryCount; i++)
            {
                PlayerPrefs.SetFloat(SettingsPrefix + "Category" + i + "Volume", categoryVolumes[i]);
                PlayerPrefs.SetInt(SettingsPrefix + "Category" + i + "Muted", categoryMuted[i] ? 1 : 0);
            }
            PlayerPrefs.Save();
            settingsDirty = false;
        }

        private void CreateSources()
        {
            CreatePool("UI Sounds", Mathf.Max(1, uiPoolSize), uiSources);
            CreatePool("World Sounds", Mathf.Max(1, worldPoolSize), worldSources);
            CreatePool("Looping Sounds", Mathf.Max(1, loopPoolSize), loopSources);
            musicSources[0] = CreateSource("Music A");
            musicSources[1] = CreateSource("Music B");
        }

        private void CreatePool(string poolName, int count, List<AudioSource> pool)
        {
            for (int i = 0; i < count; i++)
                pool.Add(CreateSource(poolName + " " + (i + 1)));
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.loop = false;
            source.priority = 128;
            return source;
        }

        private void ConfigureSource(AudioSource source, SoundCue cue, AudioClip clip, Vector3? position, bool loop)
        {
            source.Stop();
            source.clip = clip;
            source.outputAudioMixerGroup = GetUsableMixerGroup(cue.Category);
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.pitch = Random.Range(cue.PitchRange.x, cue.PitchRange.y);
            source.loop = loop;
            source.priority = Mathf.Clamp(256 - cue.Priority, 0, 256);
            source.panStereo = 0f;
            source.reverbZoneMix = 1f;
            source.volume = 1f;
            if (position.HasValue)
                source.transform.position = position.Value;
        }

        private void UpdatePlayback()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Playback playback = active[i];
                if (playback.Source == null || !playback.Source.isPlaying && !playback.WasLorePaused)
                {
                    ReleasePlayback(playback);
                    continue;
                }

                if (playback.IsLoop && IsOwnerUnavailable(playback))
                {
                    ReleasePlayback(playback);
                    continue;
                }

                if (playback.FollowTarget != null)
                    playback.Source.transform.position = playback.FollowTarget.position;

                UpdateDistanceGain(playback);
                if (playback.FadeDuration > 0f)
                {
                    playback.FadeElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(playback.FadeElapsed / playback.FadeDuration);
                    playback.FadeGain = Mathf.Lerp(playback.FadeStart, playback.FadeTarget, t);
                    if (t >= 1f && playback.FadeTarget <= 0f)
                    {
                        ReleasePlayback(playback);
                        continue;
                    }
                }

                ApplyPlaybackVolume(playback);
            }
        }

        private void UpdateDistanceGain(Playback playback)
        {
            if (!playback.Cue.UsesDistanceVolume || playback.Cue.Category == SoundCategory.UI)
            {
                playback.DistanceGain = 1f;
                return;
            }

            if (HearingTarget == null)
            {
                playback.DistanceGain = 0f;
                return;
            }

            Vector3 sourcePosition = playback.Source.transform.position;
            Vector3 targetPosition = HearingTarget.position;
            float distance = Vector2.Distance(new Vector2(sourcePosition.x, sourcePosition.y), new Vector2(targetPosition.x, targetPosition.y));
            float range = playback.Cue.SilentDistance - playback.Cue.FullDistance;
            playback.DistanceGain = 1f - Mathf.Clamp01((distance - playback.Cue.FullDistance) / range);
        }

        private bool IsBeyondSilentDistance(SoundCue cue, Vector3 position)
        {
            if (HearingTarget == null)
                return false;

            Vector3 target = HearingTarget.position;
            float distance = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(target.x, target.y));
            return distance >= cue.SilentDistance;
        }

        private void UpdateMusic()
        {
            for (int i = 0; i < musicSources.Length; i++)
            {
                MusicFade fade = musicFades[i];
                if (fade == null)
                    continue;

                if (!musicSources[i].isPlaying && fade.TargetGain > 0f)
                {
                    SoundCue endedCue = musicCues[i];
                    musicFades[i] = null;
                    if (currentMusicIndex == i)
                    {
                        currentMusicIndex = -1;
                        if (CurrentMusicCue == endedCue)
                            CurrentMusicCue = null;
                    }
                    continue;
                }

                fade.Elapsed += Time.unscaledDeltaTime;
                float t = fade.Duration <= 0f ? 1f : Mathf.Clamp01(fade.Elapsed / fade.Duration);
                float gain = Mathf.Lerp(fade.StartGain, fade.TargetGain, t);
                musicGains[i] = gain;

                if (t >= 1f)
                {
                    musicGains[i] = fade.TargetGain;
                    if (fade.TargetGain <= 0f)
                    {
                        SoundCue fadedCue = musicCues[i];
                        musicSources[i].Stop();
                        musicSources[i].clip = null;
                        musicCues[i] = null;
                        musicGains[i] = 0f;
                        musicFades[i] = null;
                        if (currentMusicIndex == i && CurrentMusicCue == fadedCue)
                        {
                            currentMusicIndex = -1;
                            CurrentMusicCue = null;
                        }
                    }
                    else
                    {
                        musicFades[i] = null;
                    }
                }
            }

            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicFades[i] == null && musicCues[i] != null && !musicSources[i].isPlaying)
                {
                    SoundCue endedCue = musicCues[i];
                    musicCues[i] = null;
                    musicGains[i] = 0f;
                    if (currentMusicIndex == i)
                    {
                        currentMusicIndex = -1;
                        if (CurrentMusicCue == endedCue)
                            CurrentMusicCue = null;
                    }
                }
                ApplyMusicVolume(i);
            }
        }

        private void FadeMusicSource(int index, float target, float duration)
        {
            AudioSource source = musicSources[index];
            if (!source.isPlaying && target <= 0f)
            {
                musicFades[index] = null;
                return;
            }

            MusicFade previous = musicFades[index];
            float current = previous != null
                ? Mathf.Lerp(previous.StartGain, previous.TargetGain, previous.Duration <= 0f ? 1f : Mathf.Clamp01(previous.Elapsed / previous.Duration))
                : musicGains[index];
            SoundCue cue = previous != null ? previous.Cue : musicCues[index];
            musicFades[index] = new MusicFade { Cue = cue, StartGain = current, TargetGain = target, Duration = duration };
            if (duration <= 0f)
                UpdateMusic();
        }

        private void ApplyPlaybackVolume(Playback playback)
        {
            float sourceMix = playback.BaseGain * playback.DistanceGain * playback.FadeGain * ContextGain(playback.Cue.Category) * MuteGain(playback.Cue.Category);
            sourceMix *= DirectVolumeGain(playback.Cue.Category);
            playback.Source.volume = Mathf.Clamp01(sourceMix);
        }

        private float ContextGain(SoundCategory category)
        {
            float gain = 1f;
            if (loreReading)
            {
                if (category == SoundCategory.BGM) gain *= loreMusicGain;
                else if (category == SoundCategory.Ambience) gain *= loreAmbienceGain;
            }
            if (dialogueActive)
            {
                if (category == SoundCategory.BGM) gain *= dialogueMusicGain;
                else if (category == SoundCategory.Ambience) gain *= dialogueAmbienceGain;
            }
            return gain;
        }

        private float MuteGain(SoundCategory category)
        {
            return masterMuted || categoryMuted[CategoryIndex(category)] || masterVolume <= 0f || categoryVolumes[CategoryIndex(category)] <= 0f ? 0f : 1f;
        }

        private void RefreshVolumes()
        {
            for (int i = 0; i < active.Count; i++)
                ApplyPlaybackVolume(active[i]);
            for (int i = 0; i < musicSources.Length; i++)
                ApplyMusicVolume(i);
        }

        private void ApplyMusicVolume(int index)
        {
            SoundCue cue = musicCues[index];
            if (cue == null)
                return;
            musicSources[index].volume = musicGains[index] * cue.Volume * ContextGain(SoundCategory.BGM) * MuteGain(SoundCategory.BGM) * DirectVolumeGain(SoundCategory.BGM);
        }

        private void ApplyMixerSettings()
        {
            if (mixer == null)
                return;

            SetMixerVolume("MasterVolume", masterVolume);
            SetMixerVolume("BGMVolume", categoryVolumes[(int)SoundCategory.BGM]);
            SetMixerVolume("SFXVolume", categoryVolumes[(int)SoundCategory.SFX]);
            SetMixerVolume("UIVolume", categoryVolumes[(int)SoundCategory.UI]);
            SetMixerVolume("AmbienceVolume", categoryVolumes[(int)SoundCategory.Ambience]);
        }

        private AudioMixerGroup GetMixerGroup(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.BGM: return bgmGroup;
                case SoundCategory.SFX: return sfxGroup;
                case SoundCategory.UI: return uiGroup;
                case SoundCategory.Ambience: return ambienceGroup;
                default: return null;
            }
        }

        private AudioMixerGroup GetUsableMixerGroup(SoundCategory category)
        {
            AudioMixerGroup group = GetMixerGroup(category);
            return mixer != null && group != null && group.audioMixer == mixer ? group : null;
        }

        private float DirectVolumeGain(SoundCategory category)
        {
            return GetUsableMixerGroup(category) == null
                ? masterVolume * categoryVolumes[CategoryIndex(category)]
                : 1f;
        }

        private void SetMixerVolume(string parameter, float value)
        {
            float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            if (!mixer.SetFloat(parameter, decibels))
                WarnOnce("mixer-param-" + parameter, "AudioMixer has no exposed parameter named '" + parameter + "'.", mixer);
        }

        private void ValidateMixerSetup()
        {
            if (mixer == null)
            {
                WarnOnce("mixer-null", "SoundManager has no AudioMixer assigned. Volume sliders will work through AudioSource volume instead.", this);
                return;
            }

            ValidateMixerGroup(SoundCategory.BGM, bgmGroup);
            ValidateMixerGroup(SoundCategory.SFX, sfxGroup);
            ValidateMixerGroup(SoundCategory.UI, uiGroup);
            ValidateMixerGroup(SoundCategory.Ambience, ambienceGroup);
        }

        private void ValidateMixerGroup(SoundCategory category, AudioMixerGroup group)
        {
            string categoryName = category.ToString();
            if (group == null)
            {
                WarnOnce("mixer-group-null-" + categoryName, "No AudioMixerGroup is assigned for " + categoryName + ". That category will use AudioSource volume controls.", this);
            }
            else if (group.audioMixer != mixer)
            {
                WarnOnce("mixer-group-owner-" + categoryName, "The " + categoryName + " AudioMixerGroup belongs to a different mixer than SoundManager's assigned AudioMixer.", group);
            }
        }

        private void WarnOnce(string key, string message, UnityEngine.Object context)
        {
            if (!warnedKeys.Add(key))
                return;
            Debug.LogWarning(message, context != null ? context : this);
        }

        private void LoadSettings()
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SettingsPrefix + "MasterVolume", 1f));
            masterMuted = PlayerPrefs.GetInt(SettingsPrefix + "MasterMuted", 0) != 0;
            for (int i = 0; i < CategoryCount; i++)
            {
                categoryVolumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(SettingsPrefix + "Category" + i + "Volume", 1f));
                categoryMuted[i] = PlayerPrefs.GetInt(SettingsPrefix + "Category" + i + "Muted", 0) != 0;
            }
        }

        private void SetOwnerScene(Playback playback, UnityEngine.Object owner, Transform followTarget)
        {
            GameObject ownerObject = GetGameObject(owner);
            if (ownerObject == null && followTarget != null)
                ownerObject = followTarget.gameObject;
            if (ownerObject != null)
            {
                playback.OwnerScene = ownerObject.scene;
                playback.HasOwnerScene = true;
            }
            else
            {
                playback.OwnerScene = SceneManager.GetActiveScene();
                playback.HasOwnerScene = playback.OwnerScene.IsValid();
            }
        }

        private static GameObject GetGameObject(UnityEngine.Object value)
        {
            if (value is GameObject gameObject)
                return gameObject;
            if (value is Component component)
                return component.gameObject;
            return null;
        }

        private bool IsOwnerUnavailable(Playback playback)
        {
            if (ReferenceEquals(playback.Owner, null))
                return false;
            if (playback.Owner == null)
                return true;
            if (playback.Owner is MonoBehaviour behaviour)
                return behaviour == null || !behaviour.enabled || !behaviour.gameObject.activeInHierarchy;
            if (playback.Owner is GameObject gameObject)
                return gameObject == null || !gameObject.activeInHierarchy;
            if (playback.Owner is Component component)
                return component == null || !component.gameObject.activeInHierarchy;
            return false;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Playback playback = active[i];
                if (playback.HasOwnerScene && playback.OwnerScene == scene)
                    ReleasePlayback(playback);
            }
        }

        private int CountActiveCue(SoundCue cue)
        {
            int count = 0;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Cue == cue) count++;
            return count;
        }

        private bool IsRetriggerBlocked(SoundCue cue)
        {
            if (cue.MinRetriggerSeconds <= 0f || !lastCueTimes.TryGetValue(cue, out float lastTime))
                return false;
            return Time.unscaledTime - lastTime < cue.MinRetriggerSeconds;
        }

        private bool TryReplaceCueVictim(SoundCue incoming)
        {
            Playback victim = null;
            for (int i = 0; i < active.Count; i++)
            {
                Playback candidate = active[i];
                if (candidate.Cue != incoming || candidate.Priority >= incoming.Priority)
                    continue;
                if (victim == null || candidate.Priority < victim.Priority || candidate.Priority == victim.Priority && candidate.StartedAt < victim.StartedAt)
                    victim = candidate;
            }
            if (victim == null)
                return false;
            ReleasePlayback(victim);
            return true;
        }

        private bool TryStealSource(List<AudioSource> pool, int incomingPriority, out AudioSource source)
        {
            Playback victim = null;
            for (int i = 0; i < active.Count; i++)
            {
                Playback candidate = active[i];
                if (!pool.Contains(candidate.Source) || candidate.IsLoop || candidate.Priority >= incomingPriority)
                    continue;
                if (victim == null || candidate.Priority < victim.Priority || candidate.Priority == victim.Priority && candidate.StartedAt < victim.StartedAt)
                    victim = candidate;
            }
            if (victim == null)
            {
                source = null;
                return false;
            }
            source = victim.Source;
            ReleasePlayback(victim);
            return true;
        }

        private AudioSource FindFreeSource(List<AudioSource> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                AudioSource source = pool[i];
                if (source == null || source.isPlaying)
                    continue;
                bool owned = false;
                for (int activeIndex = 0; activeIndex < active.Count; activeIndex++)
                {
                    if (active[activeIndex].Source == source)
                    {
                        owned = true;
                        break;
                    }
                }
                if (!owned) return source;
            }
            return null;
        }

        private Playback FindPlayback(SoundHandle handle)
        {
            if (!handle.IsValid)
                return null;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Id == handle.Id && active[i].Generation == handle.Generation) return active[i];
            return null;
        }

        private void ReleasePlayback(Playback playback)
        {
            if (playback == null)
                return;
            active.Remove(playback);
            if (playback.Source == null)
                return;
            playback.Source.Stop();
            playback.Source.clip = null;
            playback.Source.loop = false;
            playback.Source.volume = 1f;
            playback.Source.pitch = 1f;
        }

        private static int CategoryIndex(SoundCategory category)
        {
            int index = (int)category;
            if (index < 0 || index >= CategoryCount)
                throw new System.ArgumentOutOfRangeException(nameof(category), category, "Unknown sound category.");
            return index;
        }
    }
}
