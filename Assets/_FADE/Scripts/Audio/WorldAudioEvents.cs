using System;
using UnityEngine;

namespace Scarlet.Audio
{
    // Compatibility for the first SCARLET implementation. New actions should use AudioSignal assets.
    // This enum is not the reusable system's list of allowed sounds.
    public enum WorldSoundAction
    {
        EnemyAlert = 0,
        EnemyDamaged = 1,
        EnemyDied = 2,
        PlayerDamaged = 3,
        PlayerJumped = 4,
        PlayerDashed = 5,
        ParrySucceeded = 6,
        SwordThrown = 7,
        SwordLanded = 8,
        PlayerLanded = 9
    }

    public static class WorldAudioEvents
    {
        public static event Action<WorldSoundAction, Vector3> AtPosition;
        public static event Action<WorldSoundAction, Transform> FollowingObject;

        public static void Report(WorldSoundAction action, Vector3 position)
        {
            AtPosition?.Invoke(action, position);
        }

        public static void ReportFollowing(WorldSoundAction action, Transform source)
        {
            if (source != null) FollowingObject?.Invoke(action, source);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetListeners()
        {
            AtPosition = null;
            FollowingObject = null;
        }
    }
}
