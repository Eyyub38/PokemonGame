using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defines the visual sprites used for a particular battle environment / trigger type.
/// Add one entry per BattleTrigger in BattleSystem's Inspector list.
/// Adding a new trigger type requires zero code changes — just add a new list entry.
/// </summary>
[Serializable]
public class BattleEnvironmentVisuals {
    [Tooltip("The trigger that activates this environment. Used to match against the active BattleTrigger.")]
    public BattleTrigger trigger;

    [Tooltip("Background sprite shown behind both Pokemon.")]
    public Sprite background;

    [Tooltip("Sprite shown beneath the player's Pokemon.")]
    public Sprite playerSpot;

    [Tooltip("Sprite shown beneath the enemy's Pokemon.")]
    public Sprite enemySpot;
}
