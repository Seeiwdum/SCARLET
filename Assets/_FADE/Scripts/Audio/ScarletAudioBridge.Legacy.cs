using System;
using UnityEngine;

namespace Scarlet.Audio
{
    // Kept separate from the reusable Bridge. Old scene/prefab assignments still deserialize here.
    public sealed partial class ScarletAudioBridge
    {
        [Serializable]
        private sealed class WorldCueBinding
        {
            public WorldSoundAction action;
            public SoundCue cue;
        }

        [SerializeField, HideInInspector] private SoundCue hoodOn;
        [SerializeField, HideInInspector] private SoundCue hoodOff;
        [SerializeField, HideInInspector] private SoundCue playerDeath;
        [SerializeField, HideInInspector] private SoundCue loreOpen;
        [SerializeField, HideInInspector] private SoundCue loreClose;
        [SerializeField, HideInInspector] private SoundCue flameHeartPickup;
        [SerializeField, HideInInspector] private SoundCue dialogueOpen;
        [SerializeField, HideInInspector] private SoundCue dialogueClose;
        [SerializeField, HideInInspector] private bool playLegacyEnemyHit;
        [SerializeField, HideInInspector] private SoundCue legacyEnemyHit;
        [SerializeField, HideInInspector] private SoundCue sharedVaultAndWarp;
        [SerializeField, HideInInspector] private WorldCueBinding[] worldCues = Array.Empty<WorldCueBinding>();

        private ScarletGameAudioAdapter compatibilityAdapter;

        partial void EnableLegacyCompatibility()
        {
            if (compatibilityAdapter != null) { compatibilityAdapter.enabled = true; return; }
            bool hasOldAssignments = hoodOn != null || hoodOff != null || playerDeath != null || loreOpen != null
                || loreClose != null || flameHeartPickup != null || dialogueOpen != null || dialogueClose != null
                || legacyEnemyHit != null || sharedVaultAndWarp != null || (worldCues != null && worldCues.Length > 0);
            if (!hasOldAssignments) return;
            // A manually configured adapter takes precedence over automatic compatibility.
            if (GetComponent<ScarletGameAudioAdapter>() != null) return;
            compatibilityAdapter = gameObject.AddComponent<ScarletGameAudioAdapter>();
            compatibilityAdapter.enabled = false;
            var converted = new ScarletGameAudioAdapter.LegacyWorldCueBinding[worldCues == null ? 0 : worldCues.Length];
            for (int i = 0; i < converted.Length; i++)
                if (worldCues[i] != null)
                    converted[i] = new ScarletGameAudioAdapter.LegacyWorldCueBinding { action = worldCues[i].action, cue = worldCues[i].cue };
            compatibilityAdapter.ConfigureLegacy(new ScarletGameAudioAdapter.LegacyCueSettings
            {
                hoodOn = hoodOn, hoodOff = hoodOff, playerDeath = playerDeath, loreOpen = loreOpen, loreClose = loreClose,
                flameHeartPickup = flameHeartPickup, dialogueOpen = dialogueOpen, dialogueClose = dialogueClose,
                playLegacyEnemyHit = playLegacyEnemyHit, legacyEnemyHit = legacyEnemyHit,
                sharedVaultAndWarp = sharedVaultAndWarp, worldCues = converted
            });
            compatibilityAdapter.enabled = true;
            Debug.Log("Existing Bridge assignments are being handled by a runtime SCARLET Game Audio Adapter. For new setups, configure that adapter directly.", this);
        }

        partial void DisableLegacyCompatibility()
        {
            if (compatibilityAdapter != null) compatibilityAdapter.enabled = false;
        }
    }
}
