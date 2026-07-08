using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Companion/Role Definition")]
public class CompanionRoleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion role. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this role.")]
    [TextArea][SerializeField] string description;
    [Header("Bonuses")]
    [Tooltip("Extra bond/friendship points gained from companion interactions.")]
    [SerializeField] int friendshipBonus;
    [Tooltip("Bonus used by survival systems to reduce movement energy drain.")]
    [SerializeField] int staminaRegenBonus;
    [Tooltip("Bonus used by survival systems to reduce hourly need decay.")]
    [SerializeField] int survivalSupportBonus;
    [Tooltip("Perks granted to companions using this role.")]
    [SerializeField] List<CompanionPerkDefinition> perks = new List<CompanionPerkDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int FriendshipBonus => friendshipBonus;
    public int StaminaRegenBonus => staminaRegenBonus;
    public int SurvivalSupportBonus => survivalSupportBonus;
    public IReadOnlyList<CompanionPerkDefinition> Perks => perks != null ? (IReadOnlyList<CompanionPerkDefinition>)perks : System.Array.Empty<CompanionPerkDefinition>();
}
