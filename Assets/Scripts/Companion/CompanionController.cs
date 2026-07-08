using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class CompanionController : MonoBehaviour, Interactable, ISavable {
    static readonly List<CompanionController> followingCompanions = new List<CompanionController>();

    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion. Empty uses the GameObject name.")]
    [SerializeField] string companionId;
    [Tooltip("Display name for this companion. Empty uses the GameObject name.")]
    [SerializeField] string companionName;
    [Tooltip("Role definition that controls companion bonuses.")]
    [SerializeField] CompanionRoleDefinition roleDefinition;
    [Tooltip("Extra perks granted only to this companion instance.")]
    [SerializeField] List<CompanionPerkDefinition> personalPerks = new List<CompanionPerkDefinition>();

    [Header("Following")]
    [Tooltip("If enabled, this companion starts following the player when the scene starts.")]
    [SerializeField] bool followPlayerOnStart = true;
    [Tooltip("How many tile positions the companion stays behind the player.")]
    [Min(1)]
    [SerializeField] int followDistanceInTiles = 2;
    [Tooltip("If farther than this distance from the player, the companion snaps near them.")]
    [Min(1f)]
    [SerializeField] float teleportDistance = 8f;

    [Header("Dialog")]
    [Tooltip("Optional dialog played when interacting with the companion.")]
    [SerializeField] Dialog companionDialog;
    [Tooltip("Optional conditional dialog used before the fallback companion dialog/default line.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialog;

    [Header("Dialog Graph")]
    [Tooltip("Optional interactive dialog graph used before the fallback/conditional companion dialog. Leave empty to use the classic dialog fields.")]
    [SerializeField] DialogGraphDefinition dialogGraph = null;
    [Tooltip("If enabled, Dialog Graph uses this companion's dialog presentation setting instead of the graph default.")]
    [SerializeField] bool overrideGraphPresentationWithCompanionSetting = true;

    [Header("Dialog Presentation")]
    [Tooltip("How this companion presents interaction dialog. Classic keeps the old dialog box; Speech Bubble uses SpeechBubbleDialogManager when available.")]
    [SerializeField] DialogPresentationMode dialogPresentation = DialogPresentationMode.ClassicDialogBox;
    [Tooltip("Speech bubble style used when dialog presentation is Speech Bubble.")]
    [SerializeField] SpeechBubbleStyleDefinition speechBubbleStyle;
    [Tooltip("Optional transform used as the speech bubble anchor. Empty uses this companion transform plus the style offset.")]
    [SerializeField] Transform speechBubbleAnchor;

    Character character;
    PersonalityProfile personalityProfile;
    PlayerController followTarget;
    Queue<Vector3> trail = new Queue<Vector3>();
    bool isFollowing;
    bool isMoving;
    int bondPoints;

    public string CompanionId => string.IsNullOrWhiteSpace(companionId) ? name : companionId;
    public string CompanionName => string.IsNullOrWhiteSpace(companionName) ? name : companionName;
    public CompanionRoleDefinition RoleDefinition => roleDefinition;
    public IReadOnlyList<CompanionPerkDefinition> PersonalPerks => personalPerks != null ? (IReadOnlyList<CompanionPerkDefinition>)personalPerks : System.Array.Empty<CompanionPerkDefinition>();
    public CompanionBondLevel BondLevel => CompanionBondRules.GetBondLevel(bondPoints);
    public Personality Personality => personalityProfile != null ? personalityProfile.Personality : PersonalityDB.Personalities[PersonalityID.Balanced];
    public bool IsFollowing => isFollowing;
    public int BondPoints => bondPoints;

    public static IReadOnlyList<CompanionController> FollowingCompanions => followingCompanions;

    public static IEnumerable<CompanionController> GetFollowingCompanions(PlayerController player = null) {
        return followingCompanions.Where(companion => companion != null
            && companion.isActiveAndEnabled
            && companion.isFollowing
            && (player == null || companion.followTarget == player));
    }

    void Awake() {
        character = GetComponent<Character>();
        personalityProfile = GetComponent<PersonalityProfile>();
    }

    void Start() {
        if(followPlayerOnStart && PlayerController.i != null) {
            StartFollowing(PlayerController.i);
        }
    }

    void OnDisable() {
        StopFollowing();
    }

    void Update() {
        character.HandleUpdate();

        if(!isFollowing || followTarget == null || isMoving || character.IsMoving) {
            return;
        }

        if(Vector3.Distance(transform.position, followTarget.transform.position) > teleportDistance) {
            SnapNearTarget();
            return;
        }

        if(trail.Count < Mathf.Max(1, followDistanceInTiles)) {
            return;
        }

        var targetPos = trail.Dequeue();
        var moveVector = targetPos - transform.position;
        moveVector.x = Mathf.Round(moveVector.x);
        moveVector.y = Mathf.Round(moveVector.y);

        if(Mathf.Abs(moveVector.x) + Mathf.Abs(moveVector.y) != 1f) {
            return;
        }

        StartCoroutine(MoveAlongTrail(new Vector2(moveVector.x, moveVector.y)));
    }

    public void StartFollowing(PlayerController player) {
        if(player == null) {
            return;
        }

        StopFollowing();
        followTarget = player;
        isFollowing = true;
        trail.Clear();
        player.OnMovedTile += OnPlayerMovedTile;
        RegisterFollowing();
    }

    public void StopFollowing() {
        if(followTarget != null) {
            followTarget.OnMovedTile -= OnPlayerMovedTile;
        }

        followTarget = null;
        isFollowing = false;
        trail.Clear();
        followingCompanions.Remove(this);
    }

    public void AddBond(int amount) {
        if(amount <= 0) {
            return;
        }

        var personalityBonus = Personality.ModifyFriendshipGain(amount) - amount;
        var roleBonus = roleDefinition != null ? roleDefinition.FriendshipBonus : 0;
        var bondMultiplier = CompanionBondRules.GetBondMultiplier(BondLevel);
        int perkBonus = GetActivePerks(null).Sum(perk => perk.BondGainBonus);
        bondPoints = Mathf.Clamp(bondPoints + amount + personalityBonus + roleBonus + perkBonus + bondMultiplier - 1, 0, 9999);
    }

    public int GetStaminaSupport() {
        int roleBonus = roleDefinition != null ? roleDefinition.StaminaRegenBonus : 0;
        int perkBonus = GetActivePerks(null).Sum(perk => perk.StaminaSupportBonus);
        return roleBonus + perkBonus + CompanionBondRules.GetBondMultiplier(BondLevel) - 1;
    }

    public int GetSurvivalSupport() {
        int roleBonus = roleDefinition != null ? roleDefinition.SurvivalSupportBonus : 0;
        int perkBonus = GetActivePerks(null).Sum(perk => perk.SurvivalSupportBonus);
        return roleBonus + perkBonus + Mathf.Max(0, CompanionBondRules.GetBondMultiplier(BondLevel) - 2);
    }

    public IEnumerable<CompanionPerkDefinition> GetAllPerks() {
        if(roleDefinition != null) {
            foreach(var perk in roleDefinition.Perks) {
                if(perk != null) {
                    yield return perk;
                }
            }
        }

        foreach(var perk in PersonalPerks) {
            if(perk != null) {
                yield return perk;
            }
        }
    }

    public IEnumerable<CompanionPerkDefinition> GetActivePerks(ActivityDefinition activity) {
        return GetAllPerks()
            .Distinct()
            .Where(perk => perk.IsUnlockedBy(this))
            .Where(perk => activity == null || perk.Affects(activity));
    }

    public bool HasActivePerk(CompanionPerkDefinition perk) {
        return perk != null && GetActivePerks(null).Contains(perk);
    }

    public float GetExperienceMultiplier(ActivityDefinition activity) {
        float multiplier = 1f;
        foreach(var perk in GetActivePerks(activity)) {
            multiplier *= perk.ExperienceMultiplier;
        }
        return multiplier;
    }

    public int GetFlatExperienceBonus(ActivityDefinition activity) {
        return GetActivePerks(activity).Sum(perk => perk.FlatExperienceBonus);
    }

    public int GetYieldBonus(ActivityDefinition activity) {
        return GetActivePerks(activity).Sum(perk => perk.YieldBonus);
    }

    public int GetResearchPointBonus(ActivityDefinition activity) {
        return GetActivePerks(activity).Sum(perk => perk.ResearchPointBonus);
    }

    public int GetPokemonCareBonus(ActivityDefinition activity) {
        return GetActivePerks(activity).Sum(perk => perk.PokemonCareBonus);
    }

    public float GetItemCostMultiplier(ActivityDefinition activity) {
        return GetActivePerks(activity).Aggregate(1f, (current, perk) => current * perk.ItemCostMultiplier);
    }

    public float GetToolDurabilityCostMultiplier(ActivityDefinition activity) {
        return GetActivePerks(activity).Aggregate(1f, (current, perk) => current * perk.ToolDurabilityCostMultiplier);
    }

    public float GetNeedCostMultiplier(ActivityDefinition activity) {
        return GetActivePerks(activity).Aggregate(1f, (current, perk) => current * perk.NeedCostMultiplier);
    }

    public IEnumerator Interact(Transform initiator) {
        character.LookTowards(initiator.position);
        AddBond(2);

        if(dialogGraph != null) {
            var options = new DialogGraphPlaybackOptions {
                Player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i,
                Initiator = initiator,
                Source = this,
                SpeakerName = CompanionName,
                SpeakerId = CompanionId,
                OverridePresentation = overrideGraphPresentationWithCompanionSetting,
                Presentation = dialogPresentation,
                SpeechBubbleStyle = speechBubbleStyle,
                SpeechBubbleAnchor = speechBubbleAnchor
            };
            yield return DialogGraphPlayer.Ensure().Play(dialogGraph, options);
            yield break;
        }

        var selectedDialog = conditionalDialog != null
            ? conditionalDialog.SelectDialog(DialogContext.FromInteraction(this, initiator, CompanionName))
            : companionDialog;

        if(selectedDialog != null) {
            yield return DialogPresenter.ShowDialog(selectedDialog, dialogPresentation, this, initiator, CompanionName, speechBubbleStyle, speechBubbleAnchor);
        } else {
            yield return DialogPresenter.ShowText(GetDefaultDialogLine(), dialogPresentation, this, initiator, CompanionName, speechBubbleStyle, speechBubbleAnchor);
        }
    }

    void OnPlayerMovedTile(Vector3 playerPosition) {
        trail.Enqueue(playerPosition);
        AddBond(1);

        while(trail.Count > 16) {
            trail.Dequeue();
        }
    }

    void RegisterFollowing() {
        if(!followingCompanions.Contains(this)) {
            followingCompanions.Add(this);
        }
    }

    IEnumerator MoveAlongTrail(Vector2 moveVector) {
        isMoving = true;
        yield return character.Move(moveVector, null, false);
        isMoving = false;
    }

    void SnapNearTarget() {
        var fallbackOffset = -followTarget.GetLastFacingDirection();
        if(fallbackOffset == Vector3.zero) {
            fallbackOffset = Vector3.down;
        }

        transform.position = followTarget.transform.position + fallbackOffset;
        trail.Clear();
    }

    string GetDefaultDialogLine() {
        var roleName = roleDefinition != null ? roleDefinition.DisplayName : "None";
        return $"{CompanionName} is following you. Role: {roleName}, Bond: {BondLevel}.";
    }

    public object CaptureState() {
        return new CompanionSaveData() {
            position = new float[] { transform.position.x, transform.position.y },
            isFollowing = isFollowing,
            bondPoints = bondPoints,
            companionId = CompanionId,
            roleId = roleDefinition != null ? roleDefinition.Id : null
        };
    }

    public void RestoreState(object state) {
        var saveData = state as CompanionSaveData;
        if(saveData == null) {
            return;
        }

        if(saveData.position != null && saveData.position.Length >= 2) {
            transform.position = new Vector3(saveData.position[0], saveData.position[1], transform.position.z);
        }

        bondPoints = saveData.bondPoints;

        if(saveData.isFollowing && PlayerController.i != null) {
            StartFollowing(PlayerController.i);
        } else {
            StopFollowing();
        }
    }
}

[System.Serializable]
public class CompanionSaveData {
    public string companionId;
    public float[] position;
    public bool isFollowing;
    public int bondPoints;
    public string roleId;
}
