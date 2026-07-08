using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PhoneCallManager : MonoBehaviour {
    [Header("Runtime")]
    [Tooltip("If enabled, missing PlayerPhoneLog is added to the player when a call starts.")]
    [SerializeField] bool createMissingPlayerLog = true;
    [Tooltip("If enabled, phone calls use DialogManager for connected, busy and blocked feedback.")]
    [SerializeField] bool showDialogFeedback = true;
    [Tooltip("If enabled, successful phone calls are written to GameDebug.")]
    [SerializeField] bool logSuccessfulCalls = true;
    [Tooltip("If enabled, blocked or failed phone calls are written to GameDebug.")]
    [SerializeField] bool logFailedCalls = true;

    public static PhoneCallManager i { get; private set; }
    public PhoneCallResult LastResult { get; private set; }
    public event Action<PhoneCallResult> OnCallCompleted;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
    }

    public static PhoneCallManager Ensure() {
        if(i != null) {
            return i;
        }

        var manager = FindAnyObjectByType<PhoneCallManager>();
        if(manager != null) {
            return manager;
        }

        return new GameObject("PhoneCallManager").AddComponent<PhoneCallManager>();
    }

    public IEnumerator CallContact(PhoneContactDefinition contact, PlayerController player, PhoneTerminal terminal = null, Action<PhoneCallResult> onCompleted = null) {
        var result = CreateResult(contact, terminal);

        if(player == null) {
            result.status = PhoneCallStatus.Blocked;
            result.message = "A player is required to make phone calls.";
            yield return FinishCall(null, contact, result, onCompleted, null);
            yield break;
        }

        var phoneLog = ResolvePhoneLog(player);
        if(contact == null) {
            result.status = PhoneCallStatus.Blocked;
            result.message = "No phone contact was selected.";
            yield return FinishCall(phoneLog, contact, result, onCompleted, null);
            yield break;
        }

        if(contact.RequiresKnownContact && (phoneLog == null || !phoneLog.HasContact(contact))) {
            result.status = PhoneCallStatus.Blocked;
            result.message = $"{contact.DisplayName}'s number is not known.";
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.UnavailableDialog);
            yield break;
        }

        if(!contact.AllowsTerminal(terminal)) {
            result.status = PhoneCallStatus.Blocked;
            result.message = $"{contact.DisplayName} cannot be called from this phone.";
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.UnavailableDialog);
            yield break;
        }

        if(!contact.IsAvailableAtHour(result.hour)) {
            result.status = PhoneCallStatus.Unavailable;
            result.message = $"{contact.DisplayName} is unavailable at this hour.";
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.UnavailableDialog);
            yield break;
        }

        if(IsOnCooldown(contact, phoneLog, result.absoluteHour, out var cooldownMessage)) {
            result.status = PhoneCallStatus.Busy;
            result.message = cooldownMessage;
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.BusyDialog);
            yield break;
        }

        if(HasReachedDailyLimit(contact, phoneLog, result.day, out var dailyLimitMessage)) {
            result.status = PhoneCallStatus.Busy;
            result.message = dailyLimitMessage;
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.BusyDialog);
            yield break;
        }

        if(UnityEngine.Random.value > contact.AnswerChance) {
            result.status = PhoneCallStatus.Busy;
            result.message = string.IsNullOrWhiteSpace(contact.BusyFallbackText) ? $"{contact.DisplayName} did not answer." : contact.BusyFallbackText;
            yield return FinishCall(phoneLog, contact, result, onCompleted, contact.BusyDialog);
            yield break;
        }

        result.status = PhoneCallStatus.Connected;
        result.message = string.IsNullOrWhiteSpace(contact.ConnectedFallbackText) ? $"{contact.DisplayName} answered." : contact.ConnectedFallbackText;

        if(contact.RememberOnSuccessfulCall && phoneLog != null) {
            phoneLog.LearnContact(contact, terminal != null ? terminal.TerminalId : "phone-call");
        }

        yield return ShowDialogOrText(contact.ConnectedDialog, result.message);
        yield return ExecuteActions(contact, player, result);

        if(result.status == PhoneCallStatus.Failed || result.status == PhoneCallStatus.Blocked) {
            yield return ShowDialogOrText(null, result.message);
            yield return FinishCall(phoneLog, contact, result, onCompleted, null, skipDialog: true);
            yield break;
        }

        result.success = result.status == PhoneCallStatus.Connected;
        if(result.success) {
            var successText = BuildSuccessText(contact, result);
            if(!string.IsNullOrWhiteSpace(successText) || contact.ActionSuccessDialog != null) {
                yield return ShowDialogOrText(contact.ActionSuccessDialog, successText);
            }
        }

        yield return FinishCall(phoneLog, contact, result, onCompleted, null, skipDialog: true);
    }

    PhoneCallResult CreateResult(PhoneContactDefinition contact, PhoneTerminal terminal) {
        int day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        int hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
        return new PhoneCallResult {
            contactId = contact != null ? contact.Id : string.Empty,
            contactName = contact != null ? contact.DisplayName : string.Empty,
            terminalId = terminal != null ? terminal.TerminalId : string.Empty,
            day = day,
            hour = hour,
            absoluteHour = day * 24 + hour
        };
    }

    PlayerPhoneLog ResolvePhoneLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var phoneLog = player.GetComponent<PlayerPhoneLog>();
        if(phoneLog == null && createMissingPlayerLog) {
            phoneLog = player.gameObject.AddComponent<PlayerPhoneLog>();
        }

        return phoneLog;
    }

    bool IsOnCooldown(PhoneContactDefinition contact, PlayerPhoneLog phoneLog, int currentAbsoluteHour, out string message) {
        message = string.Empty;
        if(contact == null || phoneLog == null || contact.CooldownHours <= 0) {
            return false;
        }

        int lastCall = phoneLog.GetLastCallAbsoluteHour(contact.Id);
        if(lastCall < 0) {
            return false;
        }

        int elapsed = currentAbsoluteHour - lastCall;
        if(elapsed >= contact.CooldownHours) {
            return false;
        }

        message = $"{contact.DisplayName} can be called again in {contact.CooldownHours - elapsed} hour(s).";
        return true;
    }

    bool HasReachedDailyLimit(PhoneContactDefinition contact, PlayerPhoneLog phoneLog, int day, out string message) {
        message = string.Empty;
        if(contact == null || phoneLog == null || contact.MaxCallsPerDay <= 0) {
            return false;
        }

        int count = phoneLog.CountCallsOnDay(contact.Id, day);
        if(count < contact.MaxCallsPerDay) {
            return false;
        }

        message = $"{contact.DisplayName} has already been called today.";
        return true;
    }

    IEnumerator ExecuteActions(PhoneContactDefinition contact, PlayerController player, PhoneCallResult result) {
        var actions = contact.Actions;
        if(actions == null || actions.Count == 0) {
            yield break;
        }

        for(int i = 0; i < actions.Count; i++) {
            var action = actions[i];
            if(action == null || action.ActionType == PhoneCallActionType.None) {
                continue;
            }

            switch(action.ActionType) {
                case PhoneCallActionType.OpenPokemonStorage:
                    ExecuteOpenStorage(action, result);
                    break;
                case PhoneCallActionType.StartQuest:
                    yield return ExecuteStartQuest(action, result);
                    break;
                case PhoneCallActionType.CompleteQuest:
                    yield return ExecuteCompleteQuest(action, player, result);
                    break;
            }

            if(result.status == PhoneCallStatus.Failed || result.status == PhoneCallStatus.Blocked) {
                yield break;
            }
        }
    }

    void ExecuteOpenStorage(PhoneContactAction action, PhoneCallResult result) {
        if(GameController.i == null || GameController.i.StateMachine == null || StorageState.i == null) {
            FailAction(action, result, "Pokemon storage is not ready.");
            return;
        }

        GameController.i.StateMachine.Push(StorageState.i);
        result.openedPokemonStorage = true;
        result.actionMessages.Add(string.IsNullOrWhiteSpace(action.SuccessMessage) ? "Pokemon storage opened." : action.SuccessMessage);
    }

    IEnumerator ExecuteStartQuest(PhoneContactAction action, PhoneCallResult result) {
        if(action.Quest == null) {
            FailAction(action, result, "No quest is assigned to this phone action.");
            yield break;
        }

        var questList = ResolveQuestList();
        if(questList != null && questList.IsStarted(action.Quest.Name)) {
            result.actionMessages.Add(string.IsNullOrWhiteSpace(action.SuccessMessage) ? $"{action.Quest.Name} is already started." : action.SuccessMessage);
            yield break;
        }

        var quest = new Quest(action.Quest);
        yield return quest.StartQuest();
        result.actionMessages.Add(string.IsNullOrWhiteSpace(action.SuccessMessage) ? $"{action.Quest.Name} started." : action.SuccessMessage);
    }

    IEnumerator ExecuteCompleteQuest(PhoneContactAction action, PlayerController player, PhoneCallResult result) {
        if(action.Quest == null) {
            FailAction(action, result, "No quest is assigned to this phone action.");
            yield break;
        }

        var questList = ResolveQuestList();
        bool isStarted = questList != null && questList.IsStarted(action.Quest.Name);
        if(action.RequireQuestStarted && !isStarted) {
            FailAction(action, result, string.IsNullOrWhiteSpace(action.FailureMessage) ? $"{action.Quest.Name} has not been started." : action.FailureMessage);
            yield break;
        }

        var quest = questList != null ? questList.GetQuest(action.Quest.Name) : null;
        quest ??= new Quest(action.Quest);
        if(!quest.CanBeCompleted()) {
            FailAction(action, result, string.IsNullOrWhiteSpace(action.FailureMessage) ? $"{action.Quest.Name} cannot be completed yet." : action.FailureMessage);
            yield break;
        }

        yield return quest.CompleteQuest(player.transform);
        result.actionMessages.Add(string.IsNullOrWhiteSpace(action.SuccessMessage) ? $"{action.Quest.Name} completed." : action.SuccessMessage);
    }

    QuestList ResolveQuestList() {
        if(PlayerController.i != null) {
            var list = PlayerController.i.GetComponent<QuestList>();
            if(list != null) {
                return list;
            }
        }

        return FindAnyObjectByType<QuestList>();
    }

    void FailAction(PhoneContactAction action, PhoneCallResult result, string fallbackMessage) {
        result.status = PhoneCallStatus.Failed;
        result.success = false;
        string message = action != null && !string.IsNullOrWhiteSpace(action.FailureMessage) ? action.FailureMessage : fallbackMessage;
        result.message = message;
        result.actionMessages.Add(message);
    }

    string BuildSuccessText(PhoneContactDefinition contact, PhoneCallResult result) {
        if(contact != null && !string.IsNullOrWhiteSpace(contact.ActionSuccessFallbackText)) {
            return contact.ActionSuccessFallbackText;
        }

        if(result.actionMessages == null || result.actionMessages.Count == 0) {
            return string.Empty;
        }

        return string.Join("\n", result.actionMessages.Where(message => !string.IsNullOrWhiteSpace(message)));
    }

    IEnumerator FinishCall(PlayerPhoneLog phoneLog, PhoneContactDefinition contact, PhoneCallResult result, Action<PhoneCallResult> onCompleted, Dialog dialog, bool skipDialog = false) {
        if(!skipDialog) {
            var fallback = ResolveFallbackText(contact, result);
            yield return ShowDialogOrText(dialog, fallback);
        }

        LastResult = result;
        phoneLog?.RecordCall(result);
        WriteDebug(result);
        OnCallCompleted?.Invoke(result);
        onCompleted?.Invoke(result);
    }

    string ResolveFallbackText(PhoneContactDefinition contact, PhoneCallResult result) {
        if(result == null) {
            return string.Empty;
        }

        if(!string.IsNullOrWhiteSpace(result.message)) {
            return result.message;
        }

        if(contact == null) {
            return "Phone call failed.";
        }

        return result.status switch {
            PhoneCallStatus.Busy => contact.BusyFallbackText,
            PhoneCallStatus.Unavailable => contact.UnavailableFallbackText,
            PhoneCallStatus.Blocked => contact.UnavailableFallbackText,
            PhoneCallStatus.Connected => contact.ConnectedFallbackText,
            _ => "Phone call failed."
        };
    }

    IEnumerator ShowDialogOrText(Dialog dialog, string fallbackText) {
        if(!showDialogFeedback || DialogManager.i == null) {
            yield break;
        }

        if(dialog != null) {
            yield return DialogManager.i.ShowDialog(dialog);
        } else if(!string.IsNullOrWhiteSpace(fallbackText)) {
            yield return DialogManager.i.ShowDialogText(fallbackText);
        }
    }

    void WriteDebug(PhoneCallResult result) {
        if(result == null) {
            return;
        }

        string source = "PhoneCallManager";
        if(result.success) {
            if(logSuccessfulCalls) {
                GameDebug.Success(result.message, GameDebugCategory.PokeNav, this, source);
            }
        } else if(logFailedCalls) {
            GameDebug.Warning(result.message, GameDebugCategory.PokeNav, this, source);
        }
    }
}
