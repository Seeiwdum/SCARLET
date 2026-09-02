using UnityEngine;

public interface IFormSwappable
{
    /// <summary>
    /// Swaps appearance/behavior between Monster Form and Infected Villager Form
    /// </summary>
    /// <param name="isHoodWorn">True = Monster Form (hood on, brainwashing active), 
    /// False = Infected Villager Form (hood off, true identities visible)</param>
    void SwapForm(bool isHoodWorn);
}