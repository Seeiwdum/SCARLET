# Adding new sounds without changing the audio core

## The simple idea

**AudioSignal says what happened. SoundCue says what to play. ScarletAudioBridge connects the two. SoundManager plays it.**

The Bridge now has an Inspector list instead of a built-in list of hood, enemy, quest, or future sound names.

```text
Any gameplay object raises its assigned signal
    -> Bridge finds Signal -> Cue assignment
    -> SoundManager plays the cue and handles volume/distance
```

## Add a completely new action

1. Create > SCARLET > Audio > Audio Signal. Name it GrandmaGreeting, CarEngineStarted, DoorOpened, or any action you need.
2. Create a Sound Cue and assign its recordings.
3. Add an entry to ScarletAudioBridge's Bindings. Drag in the signal and cue.
4. The gameplay script raises that signal when the action really happens. It does not need to know the recording, AudioSource, mixer, or pooling rules.

```csharp
[SerializeField] private Scarlet.Audio.AudioSignal greetingSignal;

// Inside the existing gameplay method that confirms the greeting:
if (greetingSignal != null)
    greetingSignal.RaiseAt(transform.position, this);
```

For a moving source use RaiseFollowing(transform, this). For a sound without distance use RaiseFrom(this). If the cue is a loop, StopFrom(this) stops that owner's loop. Repeated starts from the same owner keep one loop; other owners still get their own playback.

New actions do not require a new Bridge method, SoundManager method, or WorldSoundAction enum entry. Different actor prefabs can assign different signal assets to the same reusable gameplay script field.

The signal asset must match on both sides. Two assets named DoorOpened are still two different signals. Subscribe/unsubscribe cleanup remains necessary and is done automatically for configured bindings.

## Current NAMO events

ScarletGameAudioAdapter is an optional connector for NAMO's existing events. Its explicit subscriptions are intentionally project-specific; the reusable Bridge no longer knows those event names. Old Bridge cue assignments remain readable through compatibility code and create a configured runtime adapter.

A new C# event does not automatically tell audio which cue it should play. Its publisher must raise an assigned AudioSignal, or an adapter must connect the event once. This is a real connection requirement, not a limit on the number or names of sounds.

## What stays deliberately bounded

- The four volume groups remain BGM, SFX, UI and Ambience, as agreed. A new recording/action fits an existing group. A fifth volume group is a separate settings/mixer feature.
- The pools limit sounds playing at the SAME TIME. They do not limit how many SoundCue assets you create.
- One music controller selects the current BGM. Music zones can contain any BGM cue.
- Distance volume requires the position/following-object information. The Bridge cannot guess the sender's location.

## OOP explanation

The gameplay object owns action decisions. The signal carries the announcement. The Bridge owns the editable sound choice. The cue owns recording settings. The manager owns playback decisions. The SCARLET adapter contains knowledge of the existing game's event names.

The part that varies most often—new game actions—is now asset configuration instead of a growing switch or enum in the audio core.

See UNITY-SETUP.md for the full manual setup and checks. Actual Unity Play Mode verification is still required.
