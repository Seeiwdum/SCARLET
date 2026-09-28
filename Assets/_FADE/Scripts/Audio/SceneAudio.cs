using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scarlet.Audio
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SCARLET/Audio/Scene Audio")]
    public sealed class SceneAudio : MonoBehaviour
    {
        public static SceneAudio Active { get; private set; }

        [Header("Player")]
        [Tooltip("Drag the scene Player here. Otherwise the Player tag is checked once at startup.")]
        [SerializeField] private Transform player;
        [Header("Scene sounds")]
        [SerializeField] private SoundCue mainMusic;
        [Tooltip("Additional ambience cues. The legacy Background Ambience field is still used.")]
        [SerializeField] private SoundCue backgroundAmbience;
        [SerializeField] private SoundCue[] additionalAmbience = System.Array.Empty<SoundCue>();
        [Min(0f)] [SerializeField] private float musicFadeDuration = 1f;

        private SoundManager manager;
        private readonly List<SoundHandle> ambienceHandles = new List<SoundHandle>();
        private readonly HashSet<SoundCue> playedAmbienceCues = new HashSet<SoundCue>();
        private MusicZone currentZone;
        private SoundCue selectedMusic;
        private bool musicSelected;
        private bool initialized;
        private bool tieWarningShown;
        private Coroutine setup;
        private MusicZone[] zones = System.Array.Empty<MusicZone>();

        public Transform Player => player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Active = null; }

        private void OnEnable()
        {
            if (Active != null && Active != this)
            {
                Debug.LogWarning("Only one SceneAudio can control music at a time. This extra component was disabled.", this);
                enabled = false;
                return;
            }
            Active = this;
            setup = StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            // Give all scene objects their Awake/OnEnable calls before choosing music.
            yield return null;
            while (SoundManager.Instance == null || !SoundManager.Instance.IsReady) yield return null;
            manager = SoundManager.Instance;
            if (player == null)
            {
                try
                {
                    GameObject found = GameObject.FindGameObjectWithTag("Player");
                    if (found != null) player = found.transform;
                }
                catch (UnityException) { Debug.LogWarning("Assign the Player field on SceneAudio. The Player tag is unavailable.", this); }
            }
            manager.SetHearingTarget(player);
            if (player == null) Debug.LogWarning("SceneAudio has no Player. Music works, but distance sounds and music zones need this reference.", this);
            RefreshZones();
            initialized = true;
            SelectMusic();
            playedAmbienceCues.Clear();
            PlayAmbience(backgroundAmbience);
            if (additionalAmbience != null)
            {
                for (int i = 0; i < additionalAmbience.Length; i++)
                    PlayAmbience(additionalAmbience[i]);
            }
            setup = null;
        }

        private void PlayAmbience(SoundCue cue)
        {
            if (cue == null)
                return;
            if (!playedAmbienceCues.Add(cue))
            {
                Debug.LogWarning("SceneAudio has the same ambience cue assigned more than once. It will only play once.", this);
                return;
            }
            if (cue.Category != SoundCategory.Ambience)
            {
                Debug.LogWarning("Scene ambience cues should use the Ambience category.", cue);
                return;
            }
            ambienceHandles.Add(manager.Play(cue, this));
        }

        private void Update()
        {
            if (initialized) SelectMusic();
        }

        // Zones request a refresh when enabled/disabled. No full scene search every frame.
        public void RefreshZones()
        {
            zones = FindObjectsByType<MusicZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (initialized) SelectMusic();
        }

        private void SelectMusic()
        {
            if (manager == null) return;
            MusicZone winner = null;
            // Keep the current zone when two overlapping zones have equal priority.
            if (currentZone != null && currentZone.Contains(player)) winner = currentZone;
            foreach (MusicZone zone in zones)
            {
                if (zone == null || zone.gameObject.scene != gameObject.scene || !zone.Contains(player)) continue;
                if (winner == null || zone.Priority > winner.Priority) winner = zone;
                else if (zone != winner && zone.Priority == winner.Priority)
                {
                    if (!tieWarningShown)
                    {
                        tieWarningShown = true;
                        Debug.LogWarning("Overlapping music zones have equal priority. The current zone is kept; otherwise the first zone is used. Give them different priorities.", this);
                    }
                    if (currentZone == null && zone.GetInstanceID() < winner.GetInstanceID()) winner = zone;
                }
            }
            currentZone = winner;
            SoundCue desired = winner != null ? winner.Music : mainMusic;
            if (desired != null && desired.Category != SoundCategory.BGM)
            {
                // Invalid BGM assignment is treated as silence, not as a world effect.
                desired = null;
            }
            if (musicSelected && desired == selectedMusic) return;
            musicSelected = true;
            selectedMusic = desired;
            manager.PlayMusic(desired, musicFadeDuration);
        }

        /// <summary>Reasserts this scene's selected track after a temporary test override.</summary>
        public void ReapplyMusicSelection()
        {
            if (manager != null && musicSelected)
                manager.PlayMusic(selectedMusic, musicFadeDuration);
        }

        private void OnValidate()
        {
            musicFadeDuration = Mathf.Max(0f, musicFadeDuration);
            if (mainMusic != null && mainMusic.Category != SoundCategory.BGM)
                Debug.LogWarning("Scene Main Music must use a BGM cue.", this);
        }

        private void OnDisable()
        {
            if (setup != null) { StopCoroutine(setup); setup = null; }
            if (manager != null)
            {
                for (int i = 0; i < ambienceHandles.Count; i++)
                    manager.Stop(ambienceHandles[i]);
            }
            ambienceHandles.Clear();
            if (Active == this)
            {
                Active = null;
                if (manager != null && manager.HearingTarget == player) manager.SetHearingTarget(null);
            }
            initialized = musicSelected = false;
            currentZone = null;
            // The incoming SceneAudio decides the next track. Do not interrupt shared music here.
        }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
            if (initialized && manager != null)
            {
                manager.SetHearingTarget(player);
                SelectMusic();
            }
        }
    }
}
