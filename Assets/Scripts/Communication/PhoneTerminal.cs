using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhoneTerminal : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Identity")]
    [Tooltip("Stable terminal id used by logs and contact rules. Empty falls back to the GameObject name.")]
    [SerializeField] string terminalId = string.Empty;
    [Tooltip("Name shown in dialog/UI for this terminal.")]
    [SerializeField] string displayName = "Phone";
    [Tooltip("If enabled, this is treated as a public phone for contact access rules.")]
    [SerializeField] bool isPublicTerminal = true;
    [Tooltip("Free-form tags used by Phone Contact required terminal tags, such as professor, police, lab, station or home.")]
    [SerializeField] List<string> terminalTags = new List<string>();

    [Header("Contacts")]
    [Tooltip("Contacts available from this terminal. UI can read this list and let the player choose.")]
    [SerializeField] List<PhoneContactDefinition> availableContacts = new List<PhoneContactDefinition>();
    [Tooltip("Contact called when Auto Call Default Contact is enabled. Empty uses the first available contact.")]
    [SerializeField] PhoneContactDefinition defaultContact = null;
    [Tooltip("If enabled, interacting with this phone immediately calls the default contact until a dedicated UI is added.")]
    [SerializeField] bool autoCallDefaultContact = true;
    [Tooltip("If enabled, interacting with this terminal adds all available contacts to PlayerPhoneLog.")]
    [SerializeField] bool learnAvailableContactsOnInteract = false;
    [Tooltip("Temporary duration in in-game hours for contacts learned from this terminal. 0 means permanent.")]
    [Min(0)]
    [SerializeField] int learnedContactDurationHours = 0;

    [Header("Trigger")]
    [Tooltip("If enabled, touching this trigger starts the phone interaction.")]
    [SerializeField] bool triggerOnEnter = false;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Feedback")]
    [Tooltip("If enabled, blocked terminal interactions use DialogManager feedback.")]
    [SerializeField] bool showDialogFeedback = true;
    [Tooltip("Text shown when this terminal has no contact to call.")]
    [TextArea]
    [SerializeField] string noContactMessage = "There is no saved contact on this phone.";
    [Tooltip("If enabled, terminal attempts are written to GameDebug.")]
    [SerializeField] bool writeToDebug = true;

    public string TerminalId => string.IsNullOrWhiteSpace(terminalId) ? name : terminalId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool IsPublicTerminal => isPublicTerminal;
    public IReadOnlyList<string> TerminalTags => terminalTags;
    public IReadOnlyList<PhoneContactDefinition> AvailableContacts => availableContacts;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerOnEnter && player != null) {
            StartCoroutine(Interact(player.transform));
        }
    }

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        LearnContacts(player);

        if(!autoCallDefaultContact) {
            yield return ShowFeedback("Phone UI selection is not connected yet.");
            yield break;
        }

        var contact = ResolveDefaultContact();
        if(contact == null) {
            yield return ShowFeedback(noContactMessage);
            yield break;
        }

        yield return CallContact(contact, player);
    }

    public IEnumerator CallContact(PhoneContactDefinition contact, PlayerController player) {
        if(contact == null) {
            yield return ShowFeedback(noContactMessage);
            yield break;
        }

        if(player == null) {
            yield return ShowFeedback("A player is required to use this phone.");
            yield break;
        }

        if(writeToDebug) {
            GameDebug.Step($"{DisplayName} calling {contact.DisplayName}.", GameDebugCategory.PokeNav, this, "PhoneTerminal");
        }

        yield return PhoneCallManager.Ensure().CallContact(contact, player, this);
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || terminalTags == null) {
            return false;
        }

        return terminalTags.Any(value => string.Equals(value, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<PhoneContactDefinition> GetCallableContacts(PlayerController player) {
        var phoneLog = player != null ? player.GetComponent<PlayerPhoneLog>() : null;
        return availableContacts
            .Where(contact => contact != null)
            .Where(contact => !contact.RequiresKnownContact || (phoneLog != null && phoneLog.HasContact(contact)))
            .Where(contact => contact.AllowsTerminal(this))
            .ToList();
    }

    void LearnContacts(PlayerController player) {
        if(!learnAvailableContactsOnInteract || player == null || availableContacts == null) {
            return;
        }

        var phoneLog = player.GetComponent<PlayerPhoneLog>() ?? player.gameObject.AddComponent<PlayerPhoneLog>();
        for(int i = 0; i < availableContacts.Count; i++) {
            var contact = availableContacts[i];
            if(contact != null) {
                phoneLog.LearnContact(contact, TerminalId, learnedContactDurationHours);
            }
        }
    }

    PhoneContactDefinition ResolveDefaultContact() {
        if(defaultContact != null) {
            return defaultContact;
        }

        return availableContacts != null ? availableContacts.FirstOrDefault(contact => contact != null) : null;
    }

    IEnumerator ShowFeedback(string message) {
        if(writeToDebug) {
            GameDebug.Warning(message, GameDebugCategory.PokeNav, this, "PhoneTerminal");
        }

        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }
}
