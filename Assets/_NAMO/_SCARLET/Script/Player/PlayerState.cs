using UnityEngine;

/// <summary>
/// <<MonoBehaviour>> Explicit state container for Player.
/// Replaces scattered bool flags with typed enums for clarity and extensibility.
/// </summary>
public class PlayerState : MonoBehaviour
{
    public enum Hood { OFF, ON }
    public enum Mode { OFF, ON }

    [Header("State")]
    [SerializeField] private Hood currentHood = Hood.OFF;
    [SerializeField] private Mode currentMode = Mode.OFF;

    public Hood CurrentHood => currentHood;
    public Mode CurrentMode => currentMode;
    public bool IsHoodOn => currentHood == Hood.ON;
    public bool IsWOFMode => currentMode == Mode.ON;

    public void HoodOn() => currentHood = Hood.ON;
    public void HoodOff() => currentHood = Hood.OFF;
    public void ToggleHood() => currentHood = currentHood == Hood.ON ? Hood.OFF : Hood.ON;

    public void WOFModeOn() => currentMode = Mode.ON;
    public void WOFModeOff() => currentMode = Mode.OFF;
    public void ToggleWOFMode() => currentMode = currentMode == Mode.ON ? Mode.OFF : Mode.ON;
}
