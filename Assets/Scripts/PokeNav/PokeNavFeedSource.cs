using System.Collections.Generic;
using UnityEngine;

public class PokeNavFeedSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Feed")]
    [Tooltip("Feed items unlocked or pushed by this NPC, board, terminal, sign or event object.")]
    [SerializeField] List<PokeNavFeedItemDefinition> feedItems = new List<PokeNavFeedItemDefinition>();
    [Tooltip("Short source id written into feed logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "pokenav-feed-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to unlock all assigned feed items.")]
    [SerializeField] bool unlockOnPlayerTrigger = true;
    [Tooltip("If enabled, linked knowledge, posts, markers and events are applied when feed items unlock.")]
    [SerializeField] bool applyLinkedData = true;
    [Tooltip("If enabled, unlocking feed items publishes events/notifications.")]
    [SerializeField] bool publishOnUnlock = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked feed unlocks are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful feed unlocks are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public IReadOnlyList<PokeNavFeedItemDefinition> FeedItems => feedItems;

    public void OnPlayerTriggered(PlayerController player) {
        if(!unlockOnPlayerTrigger) {
            return;
        }

        UnlockAll(player);
    }

    public int UnlockAll(PlayerController player) {
        int unlocked = 0;
        foreach(var item in feedItems) {
            if(item == null) {
                LogBlocked(player, "PokeNav feed source has a null feed item slot.");
                continue;
            }

            if(item.TryUnlock(player, ResolveSourceId(), applyLinkedData, publishOnUnlock, out _, out var failureMessage)) {
                unlocked++;
                if(logSuccessfulAttempts) {
                    GameDebug.Success($"{item.Title} pushed to PokeNav.", GameDebugCategory.PokeNav, this, "PokeNavFeedSource");
                }
            } else {
                LogBlocked(player, failureMessage);
            }
        }

        return unlocked;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(PlayerController player, string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.PokeNav, player != null ? player : this, "PokeNavFeedSource");
    }
}
