using System.Collections;
using UnityEngine;

public class PhoneContactSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Contact")]
    [Tooltip("Contact granted by this source.")]
    [SerializeField] PhoneContactDefinition contact = null;
    [Tooltip("Source id written into PlayerPhoneLog. Empty uses the GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Temporary duration in in-game hours. 0 means the contact is permanent.")]
    [Min(0)]
    [SerializeField] int durationHours = 0;

    [Header("Trigger")]
    [Tooltip("If enabled, interacting with this object grants the contact.")]
    [SerializeField] bool grantOnInteract = true;
    [Tooltip("If enabled, entering this trigger grants the contact.")]
    [SerializeField] bool grantOnTrigger = false;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Feedback")]
    [Tooltip("Dialog shown when the contact is learned for the first time.")]
    [SerializeField] Dialog learnedDialog = null;
    [Tooltip("Text shown when Learned Dialog is empty.")]
    [TextArea]
    [SerializeField] string learnedFallbackText = "Contact registered.";
    [Tooltip("Text shown when the player already knows this contact.")]
    [TextArea]
    [SerializeField] string alreadyKnownText = "This contact is already registered.";
    [Tooltip("If enabled, feedback is shown through DialogManager.")]
    [SerializeField] bool showDialogFeedback = true;
    [Tooltip("If enabled, grants are written to GameDebug.")]
    [SerializeField] bool writeToDebug = true;

    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(grantOnTrigger && player != null) {
            StartCoroutine(Grant(player));
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!grantOnInteract) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        yield return Grant(player);
    }

    public IEnumerator Grant(PlayerController player) {
        if(player == null) {
            yield return ShowText("A player is required to register a phone contact.");
            yield break;
        }

        if(contact == null) {
            yield return ShowText("No phone contact is assigned.");
            yield break;
        }

        var phoneLog = player.GetComponent<PlayerPhoneLog>() ?? player.gameObject.AddComponent<PlayerPhoneLog>();
        bool learned = phoneLog.LearnContact(contact, ResolveSourceId(), durationHours);
        if(writeToDebug) {
            var message = learned ? $"{contact.DisplayName} contact registered." : $"{contact.DisplayName} contact refreshed.";
            GameDebug.Step(message, GameDebugCategory.PokeNav, this, "PhoneContactSource");
        }

        if(!showDialogFeedback || DialogManager.i == null) {
            yield break;
        }

        if(learned && learnedDialog != null) {
            yield return DialogManager.i.ShowDialog(learnedDialog);
        } else {
            yield return DialogManager.i.ShowDialogText(learned ? BuildLearnedText() : alreadyKnownText);
        }
    }

    string BuildLearnedText() {
        if(!string.IsNullOrWhiteSpace(learnedFallbackText)) {
            return learnedFallbackText;
        }

        return contact != null ? $"{contact.DisplayName} registered." : "Contact registered.";
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    IEnumerator ShowText(string message) {
        if(writeToDebug) {
            GameDebug.Warning(message, GameDebugCategory.PokeNav, this, "PhoneContactSource");
        }

        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }
}
