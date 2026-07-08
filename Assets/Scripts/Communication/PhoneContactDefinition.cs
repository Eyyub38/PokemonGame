using System;
using System.Collections.Generic;
using UnityEngine;

public enum PhoneContactType {
    Professor,
    QuestContact,
    Police,
    Companion,
    Merchant,
    Transport,
    Friend,
    Custom
}

public enum PhoneTerminalAccess {
    Any,
    PublicOnly,
    PrivateOnly
}

public enum PhoneCallStatus {
    None,
    Connected,
    Busy,
    Unavailable,
    Blocked,
    Failed
}

public enum PhoneCallActionType {
    None,
    OpenPokemonStorage,
    StartQuest,
    CompleteQuest
}

[CreateAssetMenu(menuName = "Communication/Phone Contact")]
public class PhoneContactDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used for saves, logs and UI. Empty falls back to the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in phone/contact UI.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Optional phone number or short contact code shown in UI.")]
    [SerializeField] string phoneNumber = string.Empty;
    [Tooltip("High-level contact category used by filters and availability rules.")]
    [SerializeField] PhoneContactType contactType = PhoneContactType.Custom;

    [Header("Access")]
    [Tooltip("If enabled, the player must know this contact before calling it from normal UI.")]
    [SerializeField] bool requiresKnownContact = true;
    [Tooltip("Controls whether this contact can be called from public phones, private phones, or either.")]
    [SerializeField] PhoneTerminalAccess terminalAccess = PhoneTerminalAccess.Any;
    [Tooltip("If not empty, the phone terminal must have at least one of these tags.")]
    [SerializeField] List<string> requiredTerminalTags = new List<string>();
    [Tooltip("If enabled, a successful call writes this contact into PlayerPhoneLog.")]
    [SerializeField] bool rememberOnSuccessfulCall = true;

    [Header("Availability")]
    [Tooltip("If enabled, Available Start Hour and Available End Hour restrict when this contact can answer.")]
    [SerializeField] bool useHourAvailability = false;
    [Tooltip("First in-game hour when this contact can answer.")]
    [Range(0, 23)]
    [SerializeField] int availableStartHour = 8;
    [Tooltip("Exclusive in-game hour when this contact stops answering. Use 24 for end of day.")]
    [Range(1, 24)]
    [SerializeField] int availableEndHour = 22;
    [Tooltip("Chance that this contact answers after access and time checks pass.")]
    [Range(0f, 1f)]
    [SerializeField] float answerChance = 1f;
    [Tooltip("Minimum in-game hours between successful or failed calls to this contact. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours = 0;
    [Tooltip("Maximum calls allowed to this contact per in-game day. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxCallsPerDay = 0;

    [Header("Dialog")]
    [Tooltip("Dialog shown when the contact answers. Empty uses Connected Fallback Text.")]
    [SerializeField] Dialog connectedDialog = null;
    [Tooltip("Dialog shown when the contact is busy or misses the call. Empty uses Busy Fallback Text.")]
    [SerializeField] Dialog busyDialog = null;
    [Tooltip("Dialog shown when the contact is unavailable because of time or terminal rules. Empty uses Unavailable Fallback Text.")]
    [SerializeField] Dialog unavailableDialog = null;
    [Tooltip("Text shown when Connected Dialog is empty.")]
    [TextArea]
    [SerializeField] string connectedFallbackText = "The call connected.";
    [Tooltip("Text shown when Busy Dialog is empty.")]
    [TextArea]
    [SerializeField] string busyFallbackText = "No one answered.";
    [Tooltip("Text shown when Unavailable Dialog is empty.")]
    [TextArea]
    [SerializeField] string unavailableFallbackText = "This contact is unavailable right now.";

    [Header("Actions")]
    [Tooltip("Actions performed after the contact answers. Keep this list small; complex behavior should be moved into a dedicated source script.")]
    [SerializeField] List<PhoneContactAction> actions = new List<PhoneContactAction>();
    [Tooltip("Dialog shown after all actions succeed. Empty uses Action Success Fallback Text.")]
    [SerializeField] Dialog actionSuccessDialog = null;
    [Tooltip("Text shown after all actions succeed when Action Success Dialog is empty.")]
    [TextArea]
    [SerializeField] string actionSuccessFallbackText = string.Empty;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string PhoneNumber => phoneNumber;
    public PhoneContactType ContactType => contactType;
    public bool RequiresKnownContact => requiresKnownContact;
    public PhoneTerminalAccess TerminalAccess => terminalAccess;
    public IReadOnlyList<string> RequiredTerminalTags => requiredTerminalTags;
    public bool RememberOnSuccessfulCall => rememberOnSuccessfulCall;
    public bool UseHourAvailability => useHourAvailability;
    public int AvailableStartHour => Mathf.Clamp(availableStartHour, 0, 23);
    public int AvailableEndHour => Mathf.Clamp(availableEndHour, 1, 24);
    public float AnswerChance => Mathf.Clamp01(answerChance);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxCallsPerDay => Mathf.Max(0, maxCallsPerDay);
    public Dialog ConnectedDialog => connectedDialog;
    public Dialog BusyDialog => busyDialog;
    public Dialog UnavailableDialog => unavailableDialog;
    public string ConnectedFallbackText => connectedFallbackText;
    public string BusyFallbackText => busyFallbackText;
    public string UnavailableFallbackText => unavailableFallbackText;
    public IReadOnlyList<PhoneContactAction> Actions => actions;
    public Dialog ActionSuccessDialog => actionSuccessDialog;
    public string ActionSuccessFallbackText => actionSuccessFallbackText;

    public bool IsAvailableAtHour(int hour) {
        if(!useHourAvailability) {
            return true;
        }

        hour = Mathf.Clamp(hour, 0, 23);
        int start = AvailableStartHour;
        int end = AvailableEndHour;

        if(start == end) {
            return true;
        }

        if(start < end) {
            return hour >= start && hour < end;
        }

        return hour >= start || hour < end;
    }

    public bool AllowsTerminal(PhoneTerminal terminal) {
        if(terminalAccess == PhoneTerminalAccess.PublicOnly && (terminal == null || !terminal.IsPublicTerminal)) {
            return false;
        }

        if(terminalAccess == PhoneTerminalAccess.PrivateOnly && (terminal == null || terminal.IsPublicTerminal)) {
            return false;
        }

        if(requiredTerminalTags == null || requiredTerminalTags.Count == 0) {
            return true;
        }

        if(terminal == null) {
            return false;
        }

        for(int i = 0; i < requiredTerminalTags.Count; i++) {
            if(terminal.HasTag(requiredTerminalTags[i])) {
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class PhoneContactAction {
    [Tooltip("Action performed after the phone contact answers.")]
    [SerializeField] PhoneCallActionType actionType = PhoneCallActionType.None;
    [Tooltip("Quest used by Start Quest or Complete Quest actions.")]
    [SerializeField] QuestBase quest = null;
    [Tooltip("If enabled, Complete Quest requires the quest to already be started in QuestList.")]
    [SerializeField] bool requireQuestStarted = true;
    [Tooltip("Feedback text appended when this action succeeds.")]
    [TextArea]
    [SerializeField] string successMessage = string.Empty;
    [Tooltip("Feedback text used when this action fails.")]
    [TextArea]
    [SerializeField] string failureMessage = string.Empty;

    public PhoneCallActionType ActionType => actionType;
    public QuestBase Quest => quest;
    public bool RequireQuestStarted => requireQuestStarted;
    public string SuccessMessage => successMessage;
    public string FailureMessage => failureMessage;
}

[Serializable]
public class PhoneCallResult {
    [Tooltip("If enabled, the call reached the contact and all configured actions completed.")]
    public bool success;
    [Tooltip("Final call status.")]
    public PhoneCallStatus status;
    [Tooltip("Contact id used by the call.")]
    public string contactId;
    [Tooltip("Contact display name used by the call.")]
    public string contactName;
    [Tooltip("Terminal id used by the call.")]
    public string terminalId;
    [Tooltip("Readable result text for UI and debug logs.")]
    public string message;
    [Tooltip("If enabled, this call opened Pokemon storage.")]
    public bool openedPokemonStorage;
    [Tooltip("In-game day when the call happened.")]
    public int day;
    [Tooltip("In-game hour when the call happened.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the call happened.")]
    public int absoluteHour;
    [Tooltip("Action-level feedback messages.")]
    public List<string> actionMessages = new List<string>();
}
