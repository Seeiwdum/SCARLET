# SCARLET audio: manual Unity setup

The audio system now uses reusable AudioSignal assets for new actions. Existing SCARLET GameEvents connect through an optional ScarletGameAudioAdapter. These scripts are the audio code. You still create and connect the recordings, mixer, cue assets, scene objects, and menu controls in Unity. Start with a scene saved under Assets/_FADE, such as Rabob. All new code is in Assets/_FADE/Scripts/Audio; NAMO scripts were read only.

## 1. What the pieces do

| Piece | Plain meaning |
| --- | --- |
| AudioClip | A recording you import, such as a WAV file. |
| SoundCue | An instruction card holding recordings and settings. |
| SoundManager | Finds a reusable sound player and starts playback. |
| AudioSource | Unity's actual sound player. The manager creates these automatically. |
| AudioMixer | Separate volume controls for music, effects, UI, and ambience. |
| ScarletAudioBridge | Maps any AudioSignal asset to an assigned SoundCue; it has no fixed game-action list. |
| AudioSignal | An announcement asset such as DoorOpened or EnemyAlert. Create another asset for each new action. |
| ScarletGameAudioAdapter | Optional SCARLET-only connector for existing NAMO GameEvents and the first WorldAudioEvents API. |
| SceneAudio | Chooses scene music, starts background ambience, and identifies the player used for hearing distance. |
| MusicZone | A marked area that can replace scene music while the player is inside. |
| AudioSettingsUI | Connects your menu sliders and mute toggles. |
| WorldAudioEvents | Compatibility for the first action-enum API. Use AudioSignal for new actions instead. |
| SoundHandle | A ticket used to stop one particular repeating sound safely. |
| AudioTestPlayer | Optional manual testing tool; remove its scene object after testing. |

The world sounds use 2D playback with volume calculated from XY distance to the player. They do not use automatic 3D direction, walls, or camera visibility. UI and music do not fade with distance.

## 2. Let Unity import the code

1. Open Unity and let the script compilation finish.
2. Open Window > General > Console.
3. Resolve any red compilation errors before adding components. Existing project errors can prevent all scripts from being attached.
4. In the Project window, check Assets/_FADE/Scripts/Audio.

The code uses the namespace Scarlet.Audio. Unity can still attach the MonoBehaviour scripts normally. SoundCategory, SoundHandle, and WorldAudioEvents are supporting types; do not attach them to GameObjects. SoundCue and AudioSignal become assets rather than components. Do not attach ScarletAudioBridge.Legacy.cs; it only preserves old serialized assignments.

## 3. Create folders and import a few recordings

Create these folders yourself in the Project window:

```text
Assets/_FADE/Audio/
    Mixer/
    Prefabs/
    SoundCues/
    Signals/
    Clips/
        BGM/
        SFX/
        UI/
        Ambience/
```

Start with one music recording, one cloth/hood recording, one UI click or page-open recording, and one roar for testing. Drag the recording files into the appropriate Clips folders. Select a recording and use the Inspector preview to confirm you can hear it.

## 4. Build the AudioMixer

1. Right-click the Mixer folder > Create > Audio Mixer. Name it SCARLET_AudioMixer.
2. Double-click it to open the Audio Mixer window.
3. Add four child groups under Master: BGM, SFX, UI, Ambience. They are siblings, not children of SFX.
4. Select each group. In its Inspector, right-click its volume control and expose that volume to scripts.
5. Find the exposed parameters list in the Audio Mixer window and rename the five parameters exactly as follows. The code needs matching spelling and capitals.

| Mixer group | Exposed parameter name |
| --- | --- |
| Master | MasterVolume |
| BGM | BGMVolume |
| SFX | SFXVolume |
| UI | UIVolume |
| Ambience | AmbienceVolume |

6. Leave group levels at 0 dB initially. The menu code supplies the player's chosen levels during Play Mode.

The code converts slider values to Unity's decibel values. You do not need to do this yourself. Mixer settings are applied after startup initialization; see Unity's AudioMixer.SetFloat documentation: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html

## 5. Create SoundCue assets

Right-click SoundCues > Create > SCARLET > Audio > Sound Cue. Name the asset, set its Clips array size, and drag an imported recording into each element.

Suggested first assets:

| Cue | Category | Loop | Distance volume |
| --- | --- | --- | --- |
| ForestMusic | BGM | On | Off |
| ForestWind | Ambience | On | Off |
| HoodOn / HoodOff | SFX | Off | Off |
| PlayerDeath | SFX | Off | Off |
| LoreOpen / LoreClose | UI | Off | Off |
| ButtonClick | UI | Off | Off |
| EnemyRoarTest | SFX | Off | On |

Settings:

- Volume: start around 0.5 to 0.8 and tune by listening.
- Pitch Range: use 1 and 1 for music. Small variation such as 0.97 to 1.03 is useful for repeated effects.
- Loop: repeats the selected recording until stopped. It does not choose a new random clip on every repeat.
- Priority: larger numbers are more important. A full short-sound pool may replace a sound with lower priority.
- Max Concurrent: maximum active copies of this cue across the game. Use 4 for the roar test if you want four overlapping roars.
- Min Retrigger Seconds: delay before this same cue can be requested again anywhere. Leave 0 for the first tests.
- Use Distance Volume: enable only for world sounds whose requests include positions.
- Full Distance: normal volume inside this distance, for example 3 map units.
- Silent Distance: silence at or beyond this distance, for example 15 map units.

Between 3 and 15 units, the example roar becomes progressively quieter. The manager uses the player's position, not the camera's position. Keep distance disabled for legacy Bridge cues: those events do not say where they happened.

## 6. Create AudioSystem

1. Outside Play Mode, create an empty root GameObject named AudioSystem.
2. Add the SoundManager component.
3. Assign SCARLET_AudioMixer to its Mixer field.
4. Assign each matching AudioMixerGroup to the BGM, SFX, UI, and Ambience routing fields. You can expand the mixer asset in the Project window or use each field's object picker.
5. Start with the component's default pool counts and story-volume settings.
6. Drag AudioSystem into the Prefabs folder to save a reusable prefab.

The manager creates its music players and sound pools at runtime. Do not add a speaker for every cue. AudioSystem stays alive when changing scenes. Put a configured prefab in each scene you want to start directly from the editor; duplicates are removed when changing scenes normally.

Default pool sizes are 6 UI players, 16 short world-sound players, 8 looping players, plus 2 music players. Lore Music Gain = 0.4 and Lore Ambience Gain = 0.35 temporarily reduce those sounds. Dialogue gains default to 1 (unchanged); set them lower if desired. A gain of 0.4 means 40% of the previously chosen level, not an overwrite of the saved music slider.

Keep AudioSystem as its own root. Do not put SceneAudio, the Bridge, the scene Player, or the settings menu under it: those belong to the scene and should leave with it.

## 7. Connect SceneAudio, reusable signals, and the optional SCARLET adapter

1. Create another empty root GameObject named SceneAudio.
2. Attach SceneAudio.cs.
3. Drag the actual scene Player's Transform into Player. This is the hearing reference for distance volume.
4. Assign ForestMusic as Main Music and optionally ForestWind as Background Ambience.
5. Additional Ambience accepts any number of other ambience cue assignments, such as rain, wind and insects. Duplicate cue assignments play only once. Actual simultaneous playback remains bounded by the loop/source pool.
6. Set Music Fade Duration to 1 second initially.
7. Attach ScarletAudioBridge.cs when this scene needs reusable sound signals.
8. In the Signals folder, create an asset using Create > SCARLET > Audio > Audio Signal. Give it an action name, for example DoorOpened.
9. Expand the Bridge's Bindings list. Add an entry, assign the DoorOpened signal and its SoundCue. A new action needs a new asset/binding, not a new enum value or a new Bridge method.
10. The publisher must raise that same signal asset when its action happens. Section 11 shows the exact code.

For the CURRENT NAMO events, add ScarletGameAudioAdapter.cs to the scene object and assign its hood, death, lore, pickup, and optional dialogue cues. This component is the only new connector that knows NAMO's classes. It is optional when reusing the core in another game.

Leave Play Legacy Enemy Hit disabled. The current warp slash can report that event without hitting an enemy. Shared Vault And Warp cannot distinguish those two actions because the current game sends one event for both.

If you already assigned cues on the OLD Bridge, those serialized references are retained in a separate compatibility file. In Play Mode, that Bridge adds/configures a runtime adapter from the old assignments. Do not add a manually configured adapter beside those old assignments unless you intend it to take precedence. New setups should configure the adapter directly. Empty old Bridges do not create an adapter automatically.

Use one configured binding for a given signal in a scene. Duplicate bindings on one Bridge are warned about and the first is used. Do not connect the same gameplay action through both the adapter and a new signal unless you deliberately want two sounds.

The reusable Bridge filters explicitly owned messages from other scenes by default. Send the actor/owner with world requests. An ownerless Raise is a global announcement heard by every enabled Bridge assigned to that signal; avoid overlapping copies for the same sound. Only Owners In This Scene can be disabled deliberately for a cross-scene/global listener.

Add SceneAudio to every scene you enter, including a silent scene: leave Main Music empty there so it fades the previous music out. A menu can omit the gameplay adapter. A menu without a player can leave Player empty; music/UI still work.

Keep the SCARLET adapter enabled throughout the scene, including while lore is open. It owns its lore/dialogue context; disabling it clears only its own changes. If the player is created later or replaced, call SceneAudio.Active.SetPlayer(newPlayerTransform).

## 8. Add a music zone

1. Create an empty GameObject named BossMusicZone in the scene.
2. Add a BoxCollider2D and enable Is Trigger. Do not use the 3D BoxCollider.
3. Resize and position the collider to cover the desired map area.
4. Add MusicZone.cs.
5. Assign a BGM cue to Music and set Priority, for example 10 for a boss area.
6. Save the scene and test walking into and out of it.

SceneAudio checks whether the player's root position is inside each zone. That handles spawning inside a zone, walking, and teleports without relying on one trigger-entry message. Only the point at the player's root is used; touching the boundary with a hand/child collider does not count yet. There is no need to add a Rigidbody2D to the zone for this position check.

Highest priority wins when zones overlap. When leaving it, another occupied zone wins or the scene music returns. Equal priorities in occupied overlapping zones produce a warning: the current zone is kept, otherwise one is selected consistently. Give overlapping zones different priorities.

An empty Music field deliberately requests silence while inside that zone. The same requested music cue continues without restarting. Requesting another cue starts a crossfade. Rapid requests replace the current fade rather than stacking competing routines.

## 9. Build the volume menu

1. Use your existing settings panel under a Canvas, or create one in your FADE scene.
2. Add AudioSettingsUI.cs to the panel.
3. Create five Unity UI Sliders and five UI Toggles: Master, Music, SFX, UI, Ambience.
4. For every slider, set Min Value = 0, Max Value = 1, and Whole Numbers = off. Your visual label may say 0 to 100%, but the actual slider uses 0 to 1.
5. Label every toggle Mute. Checked means muted, not enabled sound.
6. Assign Master Slider and Master Mute Toggle. Set Category Sliders and Category Mute Toggles to size 4. Assign their elements in this exact order: Element 0 = Music/BGM, Element 1 = SFX, Element 2 = UI, Element 3 = Ambience.
7. Assign the optional click/hover/back cue fields if you want menu feedback.
8. Keep the sliders' On Value Changed and toggles' On Value Changed lists empty for these volume actions: this script registers those listeners automatically.
9. Make the close button deactivate the settings panel. That saves the settings. AudioSettingsUI.Save is also available if you use an Apply button or animate the menu without disabling it.

The UI waits for SoundManager, reads the current values, and sets the controls without firing their callbacks. Sliders take effect immediately. Preferences are saved when the panel closes and when the application pauses/quits. Muting preserves the selected slider value.

Your UI also needs an EventSystem and the scene's working UI input module for clicks and slider dragging. Use the setup already working for your menu; the audio scripts do not change input-system settings.

For arbitrary button sounds, use a Button's On Click list, select AudioSettingsUI.PlayCue, and supply a SoundCue asset. You can create as many different button sounds as needed. For the convenience click sound, select AudioSettingsUI.PlayClickSound. For hover, add Unity's EventTrigger to the button, add Pointer Enter, and select AudioSettingsUI.PlayHoverSound. For a back button choose PlayBackSound. Put the audio callback before a callback that destroys/disables the menu.

## 10. Test hearing distance without editing enemy code

1. Make sure SceneAudio has the Player assigned.
2. Create an empty test GameObject named RoarTest near the player.
3. Add AudioTestPlayer.cs and assign EnemyRoarTest to Cue.
4. Sound Origin can stay empty; it then uses RoarTest's position.
5. Enter Play Mode. On the AudioTestPlayer component, open its context menu and select Play Test Sound (Play Mode).
6. Move RoarTest farther away and play again. With Full Distance = 3 and Silent Distance = 15, it should be quieter at 9 units and silent at 15 or more.
7. Enable Follow Origin for a longer recording if you want moving RoarTest to change loudness while it plays. Move the player too: hearing distance updates during playback.
8. To test loops, temporarily enable Loop on a test cue, play it, then use Stop Test Sound. Disable the test object and confirm its loop stops.
9. Remove the test GameObject when finished. It is a test tool, not a required emitter on every enemy.

Edits made to scene objects during Play Mode normally disappear when you stop. Asset edits such as changing a SoundCue may remain; restore test values afterward.

## 11. Add any new gameplay sound

Example: a future enemy alert, car engine, grandma greeting, or door opening.

1. Create a new AudioSignal asset named for the action.
2. Create the SoundCue containing its recordings and settings.
3. Add a Signal -> Cue entry to the scene Bridge's Bindings list.
4. Assign the signal asset to the gameplay script's serialized AudioSignal field.
5. At the confirmed action point, raise that signal.

The gameplay owner can use this pattern. No NAMO files were edited by this audio work:

```csharp
[SerializeField] private Scarlet.Audio.AudioSignal alertSignal;

// At the moment this enemy becomes alert, once per transition:
if (alertSignal != null)
    alertSignal.RaiseAt(transform.position, this);
```

For a sound that follows a moving actor:

```csharp
if (alertSignal != null)
    alertSignal.RaiseFollowing(transform, this);
```

For UI or general feedback with no distance setting:

```csharp
if (doorOpenedSignal != null)
    doorOpenedSignal.RaiseFrom(this);
```

For a repeating sound, raising the same signal again from the same owner keeps its existing loop instead of stacking another. Different owners can play the same cue separately. Stop one owner's loop with:

```csharp
engineSignal.StopFrom(this);
```

Stop all loops belonging to that signal on receiving Bridges with engineSignal.Stop(). Owner destruction/disable and scene unload also clean up loops. A sound with Loop unchecked plays once for each accepted request.

Buttons and other UnityEvent callbacks can use AudioSignal.Raise, RaiseAtObject(GameObject), RaiseFollowingObject(GameObject), or Stop directly with an assigned asset. RaiseAtObject/FollowingObject let you supply an actor in the Inspector without adding an emitter script. Animation events still need a callable method on the animated object to raise its assigned signal.

A completely new static NAMO C# event is not discovered automatically. Either its gameplay script raises an AudioSignal when the action occurs, or a project-specific adapter listens to that C# event and raises the assigned signal. This unavoidable connection defines WHEN and WHICH sound should play. SoundManager and the generic Bridge stay unchanged.

The old WorldSoundAction enum and WorldAudioEvents.Report calls remain available for compatibility through ScarletGameAudioAdapter. They are not the recommended route for new actions.

Do not connect health-changed directly to hurt audio: it also runs during healing and initial setup. Always report accepted damage at the correct point.

## 12. What to check before calling setup finished

- Hood toggling plays the assigned cue once.
- Opening lore plays a UI cue, keeps UI audible, lowers music/ambience, and pauses SFX. Closing restores chosen volumes. Short combat hit-stop does not pause audio.
- Master and individual categories control the expected sounds; zero and mute silence them.
- Unmuting returns to the previous slider value. Reopening the menu and restarting preserves settings.
- Distance tests are normal nearby, quieter between limits, and silent beyond the far limit.
- UI still works when the world pool is busy. Music is unaffected by effect pool exhaustion.
- A paused sound is not reused as an empty source.
- Owned loops stop when disabled, destroyed, or unloaded. One-shots survive actor destruction but leave with their requesting scene.
- Starting inside a music zone chooses it immediately. Exiting restores scene music or silence. Overlapping priorities work.
- Reloading/changing scenes does not duplicate audio systems, subscriptions, or music.
- The scene has one intended active AudioListener, normally on Main Camera. AudioSystem does not need an additional listener.

## 13. Troubleshooting

| Problem | Check |
| --- | --- |
| No sound at all | A configured AudioSystem exists; cue has Clips; Master is not muted/zero; one active AudioListener; editor Game-view audio is not muted. |
| Volume slider has no effect | Correct mixer group assignment and exposed parameter spelling; do not wire slider listeners twice. |
| Distance cue rejected | Assign SceneAudio Player, and raise the signal with RaiseAt or RaiseFollowing. Direct PlayAt/PlayFollowing calls also work. Legacy parameterless events have no position. |
| Enemy still makes no roar | Its gameplay must raise the SAME signal asset assigned to the Bridge, at the real action point. An asset assignment does not automatically detect actions. |
| Music does not repeat | Enable Loop on its BGM cue. |
| Music zone does nothing | Use Collider2D, Is Trigger, a BGM cue, one SceneAudio, and its Player reference; root point must be inside. |
| Unexpected duplicate cue | Remove duplicate Inspector callbacks and duplicate Bridges; do not connect the same gameplay action through two paths. |
| Loop will not start | A repeating effect needs a live scene owner; SceneAudio supplies it for background ambience. |
| Missing components after import | Check Console for pre-existing project errors before assuming the new script is missing. |

The build's starting scene must also be configured by you. Earlier inspection found a build entry pointing at an absent SampleScene. Check File > Build Profiles > Scene List and select your intended FADE scenes when preparing a build; the audio code does not edit project settings.

## 14. Verification and future-project reuse

The reusable core can compile without NAMO's scripts. For another game, copy the audio scripts/assets with their .meta files, except these optional compatibility files:

- ScarletGameAudioAdapter.cs
- ScarletAudioBridge.Legacy.cs
- WorldAudioEvents.cs

Those files preserve current SCARLET connections and old serialized assignments. The generic Bridge's optional partial hooks disappear when the compatibility file is omitted. Update the scene references and assign that game's player; no enemy/player class name is built into the generic Bridge or manager.

BGM/SFX/UI/Ambience are the four volume categories we deliberately chose. They are not a list of permitted sound actions. Any number of new sound/signal assets can use those groups. Adding a brand-new VOLUME CATEGORY is a separate mixer/settings change; it does not need to happen for a new enemy, car, tree, voice recording, or effect.

Source pool sizes intentionally limit SIMULTANEOUS playback, not the number of cue assets. SceneAudio owns one music choice, while any number of zone assets/cues may be configured. This version still assumes one scene music controller at a time; additive-scene music ownership needs a specific future design.

Verification includes actual Unity-reference compilation for both the complete SCARLET integration and the reusable core without NAMO. Managed checks use simulated sound players and test newly invented signal actions, position forwarding, listener cleanup, separate loop owners, distance, settings, pool protection and overlapping contexts. These do not replace Unity Play Mode listening, UI, collider and scene-transition checks.

NAMO files, scenes, prefabs, mixer assets and project settings were not edited. The gameplay owner still connects the publisher signals. Temporary compiler output and test doubles are outside the Unity project.
