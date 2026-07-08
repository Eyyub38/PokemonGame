using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new battle held item")]
public class BattleHeldItem : ItemBase {
    [Header("Damage")]
    [Tooltip("Percent boost applied to all damaging moves.")]
    [SerializeField] int damageBoostPercentage;
    [Tooltip("Percent boost applied only to physical moves.")]
    [SerializeField] int physicalDamageBoostPercentage;
    [Tooltip("Percent boost applied only to special moves.")]
    [SerializeField] int specialDamageBoostPercentage;
    [Tooltip("Move type that receives the type-specific damage boost. None disables this rule.")]
    [SerializeField] PokemonType boostedMoveType;
    [Tooltip("Percent boost applied when the move type matches Boosted Move Type.")]
    [SerializeField] int typeBoostPercentage;
    [Tooltip("Percent of max HP lost by the holder after dealing damage.")]
    [SerializeField] int recoilAfterHitPercentage;

    [Header("Stats")]
    [Tooltip("Percent boost applied to holder speed.")]
    [SerializeField] int speedBoostPercentage;

    [Header("Move Lock")]
    [Tooltip("If enabled, battle logic can lock the holder into the first move used.")]
    [SerializeField] bool lockMoveOnUse;

    [Header("Defense")]
    [Tooltip("If enabled, holder survives a full-HP knockout hit with 1 HP.")]
    [SerializeField] bool focusSash;
    [Tooltip("If enabled, focus sash removes itself after triggering.")]
    [SerializeField] bool consumeFocusSash = true;
    [Tooltip("Percent of attacker's max HP dealt back after contact.")]
    [SerializeField] int contactDamageToAttackerPercentage;

    [Header("End Turn")]
    [Tooltip("Percent of max HP restored to the holder at the end of each turn.")]
    [SerializeField] int endTurnHealPercentage;

    public override bool CanUseInBattle => false;
    public override bool CanUseInOutsideBattle => false;
    public override bool IsConsumable => false;
    public bool HasAnyConfiguredEffect => damageBoostPercentage != 0
        || physicalDamageBoostPercentage != 0
        || specialDamageBoostPercentage != 0
        || boostedMoveType != PokemonType.None
        || typeBoostPercentage != 0
        || recoilAfterHitPercentage != 0
        || speedBoostPercentage != 0
        || lockMoveOnUse
        || focusSash
        || contactDamageToAttackerPercentage != 0
        || endTurnHealPercentage != 0;

    public float ModifyMoveBasePower(float basePower, Pokemon owner, Pokemon defender, Move move) {
        var modifiedPower = basePower;
        var moveType = owner.GetMoveType(move, defender);

        if(damageBoostPercentage > 0) {
            modifiedPower *= 1f + damageBoostPercentage / 100f;
        }

        if(move.Base.Category == MoveCategory.Physical && physicalDamageBoostPercentage > 0) {
            modifiedPower *= 1f + physicalDamageBoostPercentage / 100f;
        }

        if(move.Base.Category == MoveCategory.Special && specialDamageBoostPercentage > 0) {
            modifiedPower *= 1f + specialDamageBoostPercentage / 100f;
        }

        if(boostedMoveType != PokemonType.None && moveType == boostedMoveType && typeBoostPercentage > 0) {
            modifiedPower *= 1f + typeBoostPercentage / 100f;
        }

        return modifiedPower;
    }

    public float ModifySpeed(float speed, Pokemon owner) {
        if(speedBoostPercentage > 0) {
            speed *= 1f + speedBoostPercentage / 100f;
        }

        return speed;
    }

    public bool ShouldLockMoveOnUse => lockMoveOnUse;

    public int ModifyIncomingDamage(int damage, Pokemon owner) {
        if(focusSash && owner.HP == owner.MaxHp && damage >= owner.HP) {
            owner.AddStatusEvent($"{owner.NickName} hung on using its {Name}!");

            if(consumeFocusSash) {
                owner.HeldItem = null;
            }

            return owner.HP - 1;
        }

        return damage;
    }

    public void OnAfterContact(Pokemon attacker, Pokemon owner, Move move) {
        if(contactDamageToAttackerPercentage <= 0 || attacker.HP <= 0) {
            return;
        }

        int damage = Mathf.Max(1, Mathf.FloorToInt(attacker.MaxHp * contactDamageToAttackerPercentage / 100f));
        attacker.DecreaseHP(damage, true);
        attacker.AddStatusEvent(StatusEventType.Damage, $"{attacker.NickName} was hurt by {owner.NickName}'s {Name}!");
    }

    public void OnAfterDamagingHit(int damage, Pokemon owner) {
        if(recoilAfterHitPercentage <= 0 || damage <= 0 || owner.HP <= 0) {
            return;
        }

        int recoilDamage = Mathf.Max(1, Mathf.FloorToInt(owner.MaxHp * recoilAfterHitPercentage / 100f));
        owner.DecreaseHP(recoilDamage, true);
        owner.AddStatusEvent(StatusEventType.Damage, $"{owner.NickName} was hurt by its {Name}!");
    }

    public void OnAfterTurn(Pokemon owner) {
        if(endTurnHealPercentage <= 0 || owner.HP <= 0 || owner.HP >= owner.MaxHp) {
            return;
        }

        int healAmount = Mathf.Max(1, Mathf.FloorToInt(owner.MaxHp * endTurnHealPercentage / 100f));
        owner.IncreaseHP(healAmount);
        owner.AddStatusEvent(StatusEventType.Heal, $"{owner.NickName} restored HP with its {Name}!");
    }
}
