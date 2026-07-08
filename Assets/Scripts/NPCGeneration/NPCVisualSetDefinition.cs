using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC Generation/NPC Visual Set")]
public class NPCVisualSetDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this visual set. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note for this visual set, such as base body, outfit or trainer class.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by NPC pools and future filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Walking Sprites")]
    [Tooltip("Sprites used while walking down.")]
    [SerializeField] List<Sprite> walkDownSprites = new List<Sprite>();
    [Tooltip("Sprites used while walking up.")]
    [SerializeField] List<Sprite> walkUpSprites = new List<Sprite>();
    [Tooltip("Sprites used while walking left.")]
    [SerializeField] List<Sprite> walkLeftSprites = new List<Sprite>();
    [Tooltip("Sprites used while walking right.")]
    [SerializeField] List<Sprite> walkRightSprites = new List<Sprite>();

    [Header("Running Sprites")]
    [Tooltip("Sprites used while running down. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> runDownSprites = new List<Sprite>();
    [Tooltip("Sprites used while running up. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> runUpSprites = new List<Sprite>();
    [Tooltip("Sprites used while running left. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> runLeftSprites = new List<Sprite>();
    [Tooltip("Sprites used while running right. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> runRightSprites = new List<Sprite>();

    [Header("Jumping Sprites")]
    [Tooltip("Sprites used while jumping down. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> jumpDownSprites = new List<Sprite>();
    [Tooltip("Sprites used while jumping up. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> jumpUpSprites = new List<Sprite>();
    [Tooltip("Sprites used while jumping left. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> jumpLeftSprites = new List<Sprite>();
    [Tooltip("Sprites used while jumping right. Empty falls back to walking sprites.")]
    [SerializeField] List<Sprite> jumpRightSprites = new List<Sprite>();

    [Header("Surfing Sprites")]
    [Tooltip("Optional sprites used while surfing.")]
    [SerializeField] List<Sprite> surfSprites = new List<Sprite>();

    [Header("Battle")]
    [Tooltip("Optional battle image used if this visual set is assigned to a trainer.")]
    [SerializeField] Sprite trainerBattleImage;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags;
    public Sprite TrainerBattleImage => trainerBattleImage;

    public IReadOnlyList<Sprite> WalkDownSprites => walkDownSprites;
    public IReadOnlyList<Sprite> WalkUpSprites => walkUpSprites;
    public IReadOnlyList<Sprite> WalkLeftSprites => walkLeftSprites;
    public IReadOnlyList<Sprite> WalkRightSprites => walkRightSprites;
    public IReadOnlyList<Sprite> RunDownSprites => runDownSprites;
    public IReadOnlyList<Sprite> RunUpSprites => runUpSprites;
    public IReadOnlyList<Sprite> RunLeftSprites => runLeftSprites;
    public IReadOnlyList<Sprite> RunRightSprites => runRightSprites;
    public IReadOnlyList<Sprite> JumpDownSprites => jumpDownSprites;
    public IReadOnlyList<Sprite> JumpUpSprites => jumpUpSprites;
    public IReadOnlyList<Sprite> JumpLeftSprites => jumpLeftSprites;
    public IReadOnlyList<Sprite> JumpRightSprites => jumpRightSprites;
    public IReadOnlyList<Sprite> SurfSprites => surfSprites;

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    public List<Sprite> GetRunDownOrWalk() => GetFallback(runDownSprites, walkDownSprites);
    public List<Sprite> GetRunUpOrWalk() => GetFallback(runUpSprites, walkUpSprites);
    public List<Sprite> GetRunLeftOrWalk() => GetFallback(runLeftSprites, walkLeftSprites);
    public List<Sprite> GetRunRightOrWalk() => GetFallback(runRightSprites, walkRightSprites);
    public List<Sprite> GetJumpDownOrWalk() => GetFallback(jumpDownSprites, walkDownSprites);
    public List<Sprite> GetJumpUpOrWalk() => GetFallback(jumpUpSprites, walkUpSprites);
    public List<Sprite> GetJumpLeftOrWalk() => GetFallback(jumpLeftSprites, walkLeftSprites);
    public List<Sprite> GetJumpRightOrWalk() => GetFallback(jumpRightSprites, walkRightSprites);

    List<Sprite> GetFallback(List<Sprite> primary, List<Sprite> fallback) {
        return primary != null && primary.Count > 0 ? new List<Sprite>(primary) : new List<Sprite>(fallback ?? new List<Sprite>());
    }
}
