using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scarlet.Audio
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SCARLET/Audio/SCARLET Game Audio Adapter")]
    public sealed class ScarletGameAudioAdapter : MonoBehaviour
    {
        [Serializable]
        public sealed class LegacyWorldCueBinding
        {
            public WorldSoundAction action;
            public SoundCue cue;
        }

        [Header("Existing reliable game events")]
        [SerializeField] private SoundCue hoodOn;
        [SerializeField] private SoundCue hoodOff;
        [SerializeField] private SoundCue playerDeath;
        [SerializeField] private SoundCue loreOpen;
        [SerializeField] private SoundCue loreClose;
        [SerializeField] private SoundCue flameHeartPickup;
        [SerializeField] private SoundCue dialogueOpen;
        [SerializeField] private SoundCue dialogueClose;

        [Header("Optional legacy effects (no hit position)")]
        [Tooltip("Keep disabled for final combat audio: OnEnemyHit can report a missed warp slash.")]
        [SerializeField] private bool playLegacyEnemyHit;
        [SerializeField] private SoundCue legacyEnemyHit;
        [Tooltip("The current game uses this one event for both vault and warp.")]
        [SerializeField] private SoundCue sharedVaultAndWarp;

        [Header("Legacy world actions carrying a position")]
        [Tooltip("Compatibility for existing WorldAudioEvents calls. Use AudioSignal bindings for new actions.")]
        [SerializeField] private LegacyWorldCueBinding[] worldCues = Array.Empty<LegacyWorldCueBinding>();

        private static ScarletGameAudioAdapter activeBridge;
        private readonly Dictionary<WorldSoundAction, SoundCue> worldLookup = new Dictionary<WorldSoundAction, SoundCue>();
        private bool listening;
        private bool reading;
        private bool dialogue;
        private bool missingManagerReported;
        private int lastHeartCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { activeBridge = null; }

        // Used only to preserve assignments from the previous Bridge version.
        internal void ConfigureLegacy(LegacyCueSettings settings)
        {
            hoodOn = settings.hoodOn;
            hoodOff = settings.hoodOff;
            playerDeath = settings.playerDeath;
            loreOpen = settings.loreOpen;
            loreClose = settings.loreClose;
            flameHeartPickup = settings.flameHeartPickup;
            dialogueOpen = settings.dialogueOpen;
            dialogueClose = settings.dialogueClose;
            legacyEnemyHit = settings.legacyEnemyHit;
            sharedVaultAndWarp = settings.sharedVaultAndWarp;
            playLegacyEnemyHit = settings.playLegacyEnemyHit;
            worldCues = settings.worldCues ?? Array.Empty<LegacyWorldCueBinding>();
            BuildLookup();
        }

        internal sealed class LegacyCueSettings
        {
            public SoundCue hoodOn, hoodOff, playerDeath, loreOpen, loreClose, flameHeartPickup;
            public SoundCue dialogueOpen, dialogueClose, legacyEnemyHit, sharedVaultAndWarp;
            public bool playLegacyEnemyHit;
            public LegacyWorldCueBinding[] worldCues;
        }

        private void OnEnable()
        {
            if (activeBridge != null && activeBridge != this)
            {
                Debug.LogWarning("Only one ScarletGameAudioAdapter should listen at a time. This extra adapter was disabled.", this);
                enabled = false;
                return;
            }
            activeBridge = this;
            lastHeartCount = FlameHeartLedger.Instance != null ? FlameHeartLedger.Instance.CurrentFlameHearts : 0;
            BuildLookup();
            Subscribe();
            StartCoroutine(SyncContextWhenReady());
        }

        private void BuildLookup()
        {
            worldLookup.Clear();
            if (worldCues == null) return;
            foreach (LegacyWorldCueBinding binding in worldCues)
            {
                if (binding == null || binding.cue == null) continue;
                if (worldLookup.ContainsKey(binding.action))
                {
                    Debug.LogWarning($"Two world cues use {binding.action}. The first assignment is used.", this);
                    continue;
                }
                worldLookup.Add(binding.action, binding.cue);
            }
        }

        private IEnumerator SyncContextWhenReady()
        {
            while (SoundManager.Instance == null || !SoundManager.Instance.IsReady) yield return null;
            SoundManager.Instance.SetLoreReading(this, reading);
            SoundManager.Instance.SetDialogueActive(this, dialogue);
        }

        private void Subscribe()
        {
            GameEvents.OnHoodToggled += OnHood;
            GameEvents.OnPlayerDied += OnDeath;
            GameEvents.OnLoreOpened += OnLoreOpen;
            GameEvents.OnLoreClosed += OnLoreClose;
            GameEvents.OnFlameHeartCollected += OnHeart;
            GameEvents.OnDialogueStarted += OnDialogueOpen;
            GameEvents.OnDialogueEnded += OnDialogueClose;
            GameEvents.OnSwordVaultPerformed += OnVault;
            if (playLegacyEnemyHit) GameEvents.OnEnemyHit += OnLegacyHit;
            WorldAudioEvents.AtPosition += OnWorldPosition;
            WorldAudioEvents.FollowingObject += OnWorldFollowing;
            listening = true;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (listening)
            {
                GameEvents.OnHoodToggled -= OnHood;
                GameEvents.OnPlayerDied -= OnDeath;
                GameEvents.OnLoreOpened -= OnLoreOpen;
                GameEvents.OnLoreClosed -= OnLoreClose;
                GameEvents.OnFlameHeartCollected -= OnHeart;
                GameEvents.OnDialogueStarted -= OnDialogueOpen;
                GameEvents.OnDialogueEnded -= OnDialogueClose;
                GameEvents.OnSwordVaultPerformed -= OnVault;
                GameEvents.OnEnemyHit -= OnLegacyHit;
                WorldAudioEvents.AtPosition -= OnWorldPosition;
                WorldAudioEvents.FollowingObject -= OnWorldFollowing;
                listening = false;
            }
            if (activeBridge == this)
            {
                activeBridge = null;
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.ClearContext(this);
                }
            }
            reading = dialogue = false;
        }

        private SoundManager Manager()
        {
            SoundManager manager = SoundManager.Instance;
            if (manager == null && !missingManagerReported)
            {
                missingManagerReported = true;
                Debug.LogWarning("Add the configured AudioSystem prefab to this scene to hear audio.", this);
            }
            return manager != null && manager.IsReady ? manager : null;
        }

        private void PlayAssigned(SoundCue cue)
        {
            if (cue != null) Manager()?.Play(cue, this);
        }

        private void OnHood(bool wearing) { PlayAssigned(wearing ? hoodOn : hoodOff); }
        private void OnDeath() { PlayAssigned(playerDeath); }
        private void OnVault() { PlayAssigned(sharedVaultAndWarp); }
        private void OnLegacyHit() { PlayAssigned(legacyEnemyHit); }
        private void OnHeart(int current, int total)
        {
            if (current > lastHeartCount) PlayAssigned(flameHeartPickup);
            lastHeartCount = current;
        }
        private void OnLoreOpen(LoreData data)
        {
            if (reading) return;
            reading = true;
            Manager()?.SetLoreReading(this, true);
            PlayAssigned(loreOpen);
        }
        private void OnLoreClose()
        {
            if (!reading) return;
            reading = false;
            Manager()?.SetLoreReading(this, false);
            PlayAssigned(loreClose);
        }
        private void OnDialogueOpen()
        {
            if (dialogue) return;
            dialogue = true;
            Manager()?.SetDialogueActive(this, true);
            PlayAssigned(dialogueOpen);
        }
        private void OnDialogueClose()
        {
            if (!dialogue) return;
            dialogue = false;
            Manager()?.SetDialogueActive(this, false);
            PlayAssigned(dialogueClose);
        }
        private void OnWorldPosition(WorldSoundAction action, Vector3 position)
        {
            if (worldLookup.TryGetValue(action, out SoundCue cue)) Manager()?.PlayAt(cue, position, this);
        }
        private void OnWorldFollowing(WorldSoundAction action, Transform source)
        {
            if (source != null && worldLookup.TryGetValue(action, out SoundCue cue))
                Manager()?.PlayFollowing(cue, source, source);
        }
    }
}
