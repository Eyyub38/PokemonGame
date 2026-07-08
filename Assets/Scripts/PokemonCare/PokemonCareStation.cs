using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokemonCareStation : MonoBehaviour, Interactable, IOverworldInteractionInfoProvider {
    [Tooltip("Care action performed when the player interacts with this station.")]
    [SerializeField] PokemonCareActionDefinition careAction;
    [Tooltip("If enabled, the care action affects every party member. If disabled, only the first healthy Pokemon is affected.")]
    [SerializeField] bool applyToWholeParty = false;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator.GetComponent<PlayerController>();
        var party = initiator.GetComponent<PokemonParty>();

        if(careAction == null || party == null) {
            yield return DialogManager.i.ShowDialogText("This care station is not ready.");
            yield break;
        }

        var activity = careAction.Activity;
        if(activity == null) {
            yield return DialogManager.i.ShowDialogText($"{careAction.DisplayName} has no activity configured.");
            yield break;
        }

        if(!activity.CanPerform(player, out var failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        var targets = GetTargetPokemon(party);
        if(targets.Count == 0) {
            yield return DialogManager.i.ShowDialogText("No Pokemon can receive care right now.");
            yield break;
        }

        var eligibleTargets = targets.Where(pokemon => careAction.CanApply(pokemon, out _)).ToList();
        if(eligibleTargets.Count == 0) {
            careAction.CanApply(targets.FirstOrDefault(), out var careFailureMessage);
            yield return DialogManager.i.ShowDialogText(string.IsNullOrWhiteSpace(careFailureMessage) ? "No Pokemon can receive this care right now." : careFailureMessage);
            yield break;
        }

        if(!activity.TryPayCosts(player, out failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        int bonus = GetCareBonus(player);
        int affectedCount = 0;
        foreach(var pokemon in eligibleTargets) {
            if(careAction.TryApply(pokemon, bonus, StationId, out _)) {
                affectedCount++;
            }
        }

        int experienceReward = PlayerActivityContext.ModifyExperience(activity, activity?.BaseExperience ?? 10);
        if(WorldEventManager.i != null) {
            experienceReward = WorldEventManager.i.ModifyExperience(activity, experienceReward);
            WorldEventManager.i.ApplyActivityReputation(player, activity);
        } else {
            player?.GetComponent<PlayerReputation>()?.ApplyChanges(activity?.ReputationChanges);
        }

        player?.GetComponent<PlayerProgression>()?.AddExperience(
            experienceReward,
            activity?.ExperienceSource ?? PlayerExperienceSource.Companion);
        activity?.ApplyRelationshipRewards(player);
        activity?.RecordCompletion(player);
        activity?.CompleteMilestones(player);
        activity?.ApplyLifePathRewards(player);
        activity?.ApplyOutcomes(player);
        PublishCareEvent(player, experienceReward, affectedCount);

        yield return DialogManager.i.ShowDialogText($"{careAction.DisplayName} complete.");
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        var activity = careAction != null ? careAction.Activity : null;
        bool canInteract = careAction != null && party != null && activity != null;
        string blockedMessage = null;

        if(careAction == null || party == null) {
            blockedMessage = "This care station is not ready.";
        } else if(activity == null) {
            blockedMessage = $"{careAction.DisplayName} has no activity configured.";
        } else if(!activity.CanPerform(player, out blockedMessage)) {
            canInteract = false;
        } else {
            var targets = GetTargetPokemon(party);
            if(targets.Count == 0) {
                canInteract = false;
                blockedMessage = "No Pokemon can receive care right now.";
            } else if(!targets.Any(pokemon => careAction.CanApply(pokemon, out _))) {
                canInteract = false;
                careAction.CanApply(targets.FirstOrDefault(), out blockedMessage);
                if(string.IsNullOrWhiteSpace(blockedMessage)) {
                    blockedMessage = "No Pokemon can receive this care right now.";
                }
            }
        }

        info = new OverworldInteractionInfo {
            TargetName = careAction != null ? careAction.DisplayName : name,
            ActionName = "Care",
            Description = careAction != null ? careAction.Description : "Care station is not configured.",
            PermissionHint = PlayerActivityContext.CurrentZone != null ? PlayerActivityContext.CurrentZone.DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }

    int GetCareBonus(PlayerController player) {
        var skill = careAction?.Activity?.BonusSkill;
        int areaBonus = PlayerActivityContext.GetPokemonCareBonus(careAction?.Activity);
        if(player == null || skill == null) {
            return areaBonus;
        }

        return (player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0) + areaBonus;
    }

    void PublishCareEvent(PlayerController player, int experienceReward, int affectedCount) {
        GameEventPublishing.PublishOptional(
            careAction.CompletedEvent,
            $"pokemon-care.completed.{careAction.Id}",
            $"{careAction.DisplayName} complete.",
            GameEventCategory.PokemonCare,
            GameEventImportance.Success,
            player,
            "PokemonCareStation",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("careActionId", careAction.Id),
            GameEventPublishing.Value("careActionName", careAction.DisplayName),
            GameEventPublishing.Value("activityId", careAction.Activity != null ? careAction.Activity.Id : null),
            GameEventPublishing.Value("experience", experienceReward),
            GameEventPublishing.Value("affectedPokemon", affectedCount));
    }

    string StationId => $"{name}:{careAction?.Id}";

    List<Pokemon> GetTargetPokemon(PokemonParty party) {
        if(party == null || party.Pokemons == null) {
            return new List<Pokemon>();
        }

        if(applyToWholeParty) {
            return party.Pokemons.Where(pokemon => pokemon != null && pokemon.HP > 0).ToList();
        }

        var pokemon = party.GetHealthyPokemon();
        return pokemon != null ? new List<Pokemon> { pokemon } : new List<Pokemon>();
    }
}
