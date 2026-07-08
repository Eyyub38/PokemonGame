using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ServiceAppointmentPayloadMode {
    None,
    Service,
    ServicePackage
}

public enum ServiceAppointmentScheduleMode {
    ManualOnly,
    Daily,
    EveryNDays,
    Weekly,
    CalendarEventScheduled
}

public enum ServiceAppointmentCompletionMode {
    AutoCompleteWhenDue,
    ClaimAtProvider
}

public enum ServiceAppointmentSourceAction {
    PreviewNextSlot,
    BookAppointment,
    CompleteDueAppointments,
    CancelLatestPending
}

[CreateAssetMenu(menuName = "Services/Appointment Definition")]
public class ServiceAppointmentDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this appointment. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future appointment UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of what this appointment reserves.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad service category used by future UI filters and appointment logs.")]
    [SerializeField] PlayerServiceCategory category = PlayerServiceCategory.General;
    [Tooltip("Optional icon used by future appointment/calendar UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as clinic, daycare, inn, grooming, professor, police, premium or region name.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Payload")]
    [Tooltip("What runs when this appointment is completed.")]
    [SerializeField] ServiceAppointmentPayloadMode payloadMode = ServiceAppointmentPayloadMode.ServicePackage;
    [Tooltip("Service used when Payload Mode is Service.")]
    [SerializeField] ServiceDefinition service;
    [Tooltip("Service package used when Payload Mode is Service Package.")]
    [SerializeField] ServicePackageDefinition servicePackage;
    [Tooltip("Optional shop context used for package price multipliers and sponsor discounts.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("If enabled, appointment completion can run even when no payload service/package is assigned.")]
    [SerializeField] bool allowEmptyCompletion;

    [Header("Schedule")]
    [Tooltip("How this appointment decides which days can be booked.")]
    [SerializeField] ServiceAppointmentScheduleMode scheduleMode = ServiceAppointmentScheduleMode.Daily;
    [Tooltip("First in-game day this appointment can be booked.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Interval in days when Schedule Mode is Every N Days.")]
    [Min(1)]
    [SerializeField] int repeatEveryDays = 1;
    [Tooltip("Weekdays used by Weekly schedule mode. Empty means every weekday is valid.")]
    [SerializeField] List<WeekDay> activeWeekDays = new List<WeekDay>();
    [Tooltip("Calendar event that must be scheduled on the appointment day when Schedule Mode is Calendar Event Scheduled.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Calendar event revealed/seen when an appointment is booked and completed when the appointment completes.")]
    [SerializeField] CalendarEventDefinition linkedCalendarEvent;
    [Tooltip("Earliest in-game hour that can be booked.")]
    [Range(0, 23)]
    [SerializeField] int earliestHour = 8;
    [Tooltip("Latest in-game hour that can be booked.")]
    [Range(0, 23)]
    [SerializeField] int latestHour = 18;
    [Tooltip("Slot spacing in in-game hours. 1 means every hour between earliest and latest.")]
    [Min(1)]
    [SerializeField] int slotIntervalHours = 1;
    [Tooltip("Minimum in-game hours between booking time and appointment time.")]
    [Min(0)]
    [SerializeField] int minimumLeadTimeHours = 1;
    [Tooltip("How many in-game hours the appointment blocks in logs/UI.")]
    [Min(0)]
    [SerializeField] int durationHours = 1;
    [Tooltip("How many in-game days ahead automatic next-slot search can look.")]
    [Min(0)]
    [SerializeField] int maxLookAheadDays = 14;
    [Tooltip("Maximum appointments allowed in the same provider/source slot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBookingsPerSlot = 1;
    [Tooltip("Maximum pending appointments this player can have for this appointment definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPendingPerPlayer = 1;
    [Tooltip("If enabled, this appointment can be booked only once per provider/source per in-game day.")]
    [SerializeField] bool oncePerProviderPerDay = true;

    [Header("Completion")]
    [Tooltip("How this appointment is completed once due.")]
    [SerializeField] ServiceAppointmentCompletionMode completionMode = ServiceAppointmentCompletionMode.ClaimAtProvider;
    [Tooltip("If enabled, completing the appointment requires the same provider/source id that booked it.")]
    [SerializeField] bool requireSameProviderForClaim = true;
    [Tooltip("If enabled, appointment records are marked late when completed after Grace Hours.")]
    [SerializeField] bool trackLateCompletion = true;
    [Tooltip("Hours after appointment time before completion is considered late. 0 means immediately late after due hour.")]
    [Min(0)]
    [SerializeField] int graceHours = 2;

    [Header("Booking Cost")]
    [Tooltip("Money paid when the appointment is booked. The service/package can still charge its own cost when completed.")]
    [Min(0f)]
    [SerializeField] float bookingFee;
    [Tooltip("If enabled, the Shop Context catalog multiplier is applied to Booking Fee.")]
    [SerializeField] bool useShopPriceMultiplier = true;
    [Tooltip("If enabled, active sponsor shop-buy multipliers can discount Booking Fee when Shop Context is assigned.")]
    [SerializeField] bool useSponsorDiscounts = true;
    [Tooltip("If enabled, Wallet is checked and charged when booking.")]
    [SerializeField] bool chargeWalletOnBooking = true;

    [Header("Cancellation")]
    [Tooltip("If enabled, pending appointments can be cancelled before they are due.")]
    [SerializeField] bool allowCancellation = true;
    [Tooltip("In-game hours after booking during which cancellation is allowed. 0 means until appointment time.")]
    [Min(0)]
    [SerializeField] int cancellationWindowHours = 1;
    [Tooltip("Percentage of booking fee refunded on cancellation. 1.0 means full booking-fee refund.")]
    [Range(0f, 1f)]
    [SerializeField] float cancellationRefundPercent = 0.75f;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this appointment can be booked.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this appointment can be booked.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this appointment.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this appointment.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Provider/source tags required before this appointment can be booked.")]
    [SerializeField] List<string> requiredProviderTags = new List<string>();
    [Tooltip("How required provider tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode providerTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this appointment can be booked.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this appointment is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This appointment is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when an appointment is booked.")]
    [SerializeField] GameEventDefinition bookedEvent;
    [Tooltip("Optional event published when an appointment is completed.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when an appointment is cancelled.")]
    [SerializeField] GameEventDefinition cancelledEvent;
    [Tooltip("Optional event published when appointment booking/completion is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, appointment events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, appointment events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PlayerServiceCategory Category => category;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ServiceAppointmentPayloadMode PayloadMode => payloadMode;
    public ServiceDefinition Service => service;
    public ServicePackageDefinition ServicePackage => servicePackage;
    public ShopCatalog ShopContext => shopContext;
    public bool AllowEmptyCompletion => allowEmptyCompletion;
    public ServiceAppointmentScheduleMode ScheduleMode => scheduleMode;
    public int StartDay => Mathf.Max(1, startDay);
    public int RepeatEveryDays => Mathf.Max(1, repeatEveryDays);
    public IReadOnlyList<WeekDay> ActiveWeekDays => activeWeekDays != null ? (IReadOnlyList<WeekDay>)activeWeekDays : Array.Empty<WeekDay>();
    public CalendarEventDefinition CalendarEvent => calendarEvent;
    public CalendarEventDefinition LinkedCalendarEvent => linkedCalendarEvent;
    public int EarliestHour => Mathf.Clamp(earliestHour, 0, 23);
    public int LatestHour => Mathf.Clamp(latestHour, 0, 23);
    public int SlotIntervalHours => Mathf.Max(1, slotIntervalHours);
    public int MinimumLeadTimeHours => Mathf.Max(0, minimumLeadTimeHours);
    public int DurationHours => Mathf.Max(0, durationHours);
    public int MaxLookAheadDays => Mathf.Max(0, maxLookAheadDays);
    public int MaxBookingsPerSlot => Mathf.Max(0, maxBookingsPerSlot);
    public int MaxPendingPerPlayer => Mathf.Max(0, maxPendingPerPlayer);
    public bool OncePerProviderPerDay => oncePerProviderPerDay;
    public ServiceAppointmentCompletionMode CompletionMode => completionMode;
    public bool RequireSameProviderForClaim => requireSameProviderForClaim;
    public bool TrackLateCompletion => trackLateCompletion;
    public int GraceHours => Mathf.Max(0, graceHours);
    public float BookingFee => Mathf.Max(0f, bookingFee);
    public bool ChargeWalletOnBooking => chargeWalletOnBooking;
    public bool AllowCancellation => allowCancellation;
    public int CancellationWindowHours => Mathf.Max(0, cancellationWindowHours);
    public float CancellationRefundPercent => Mathf.Clamp01(cancellationRefundPercent);
    public IReadOnlyList<string> RequiredProviderTags => requiredProviderTags != null ? (IReadOnlyList<string>)requiredProviderTags : Array.Empty<string>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryBuildQuote(
        PlayerController player,
        PlayerServiceAppointmentLog appointmentLog,
        ServiceAppointmentContext context,
        int requestedDay,
        int requestedHour,
        out ServiceAppointmentQuote quote,
        out string failureMessage
    ) {
        quote = null;
        if(!CanBook(player, appointmentLog, context, out failureMessage)) {
            PublishAppointmentEvent(blockedEvent, "blocked", (ServiceAppointmentQuote)null, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(!ResolveSlot(appointmentLog, context, requestedDay, requestedHour, out int scheduledDay, out int scheduledHour, out failureMessage)) {
            PublishAppointmentEvent(blockedEvent, "blocked", (ServiceAppointmentQuote)null, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float fee = GetBookingFee(player);
        float expectedCompletionCost = GetExpectedCompletionCost(player);
        int scheduledAbsoluteHour = scheduledDay * 24 + scheduledHour;
        int currentHour = GetCurrentAbsoluteHour();
        quote = new ServiceAppointmentQuote {
            appointmentId = Id,
            appointmentName = DisplayName,
            category = category,
            payloadMode = payloadMode,
            payloadId = GetPayloadId(),
            payloadName = GetPayloadName(),
            sourceId = context != null ? context.SourceId : Id,
            sourceName = context != null ? context.SourceName : DisplayName,
            shopId = context != null && context.ShopContext != null ? context.ShopContext.ShopId : string.Empty,
            scheduledDay = scheduledDay,
            scheduledHour = scheduledHour,
            scheduledAbsoluteHour = scheduledAbsoluteHour,
            durationHours = DurationHours,
            completionMode = completionMode,
            bookingFee = fee,
            expectedCompletionCost = expectedCompletionCost,
            totalDueNow = chargeWalletOnBooking ? fee : 0f,
            allowCancellation = allowCancellation,
            cancellationDeadlineAbsoluteHour = GetCancellationDeadline(currentHour, scheduledAbsoluteHour),
            cancellationRefundAmount = Mathf.Floor(fee * CancellationRefundPercent),
            graceHours = GraceHours
        };

        if(chargeWalletOnBooking && fee > 0f && (Wallet.i == null || !Wallet.i.HasMoney(fee))) {
            failureMessage = $"You need {fee:0} money to book {DisplayName}.";
            PublishAppointmentEvent(blockedEvent, "blocked", quote, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryBook(
        PlayerController player,
        PlayerServiceAppointmentLog appointmentLog,
        ServiceAppointmentContext context,
        int requestedDay,
        int requestedHour,
        out ServiceAppointmentRecord record,
        out string failureMessage
    ) {
        record = null;
        appointmentLog ??= player != null ? player.gameObject.AddComponent<PlayerServiceAppointmentLog>() : null;
        if(!TryBuildQuote(player, appointmentLog, context, requestedDay, requestedHour, out var quote, out failureMessage)) {
            return false;
        }

        if(chargeWalletOnBooking && quote.bookingFee > 0f) {
            Wallet.i.TakeMoney(quote.bookingFee);
        }

        record = appointmentLog.RecordBooking(this, quote, context);
        linkedCalendarEvent?.Reveal(player, quote.sourceId, quote.sourceName);
        PublishAppointmentEvent(bookedEvent, "booked", quote, player, context, GameEventImportance.Success, null);
        failureMessage = null;
        return true;
    }

    public bool TryComplete(
        PlayerController player,
        ServiceAppointmentRecord record,
        ServiceAppointmentContext context,
        out ServiceAppointmentCompletionResult result,
        out string failureMessage
    ) {
        result = new ServiceAppointmentCompletionResult(record);
        if(player == null) {
            failureMessage = "A player is required to complete appointments.";
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(record == null) {
            failureMessage = "No appointment record selected.";
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(!record.IsPending) {
            failureMessage = "Appointment is not pending.";
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        if(!record.IsDue(currentHour)) {
            failureMessage = "Appointment is not due yet.";
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(completionMode == ServiceAppointmentCompletionMode.ClaimAtProvider
            && requireSameProviderForClaim
            && context != null
            && !record.MatchesProvider(context.SourceId)) {
            failureMessage = "Appointment belongs to a different provider.";
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(!ApplyPayload(player, record, context, result, out failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAppointmentEvent(blockedEvent, "blocked", record, player, context, GameEventImportance.Warning, failureMessage);
            return false;
        }

        result.completed = true;
        result.completedLate = trackLateCompletion && currentHour > record.scheduledAbsoluteHour + GraceHours;
        result.completedAbsoluteHour = currentHour;
        result.completedDay = GetCurrentDay();
        player.GetComponent<PlayerCalendarLog>()?.CompleteEvent(linkedCalendarEvent, record.sourceId);
        PublishAppointmentEvent(completedEvent, "completed", record, player, context, GameEventImportance.Success, null);
        failureMessage = null;
        return true;
    }

    internal void PublishCancelled(ServiceAppointmentRecord record, PlayerController player, UnityEngine.Object eventContext) {
        PublishAppointmentEvent(
            cancelledEvent,
            "cancelled",
            record != null ? record.ToEventSnapshot() : null,
            player,
            null,
            GameEventImportance.Warning,
            null,
            eventContext != null ? eventContext : this);
    }

    public bool CanBook(PlayerController player, PlayerServiceAppointmentLog appointmentLog, ServiceAppointmentContext context, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to book appointments.";
            return false;
        }

        if(!PayloadAvailable()) {
            failureMessage = $"{DisplayName} has no service or service package assigned.";
            return false;
        }

        if(!MatchesTags(requiredProviderTags, providerTagMatchMode, tag => context != null && context.HasProviderTag(tag))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not offered by this provider." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(MaxPendingPerPlayer > 0 && appointmentLog != null && appointmentLog.GetPendingCount(this) >= MaxPendingPerPlayer) {
            failureMessage = $"{DisplayName} already has the maximum pending appointment count.";
            return false;
        }

        if(oncePerProviderPerDay && appointmentLog != null && appointmentLog.HasBookedOnDay(this, context != null ? context.SourceId : null, GetCurrentDay())) {
            failureMessage = $"{DisplayName} can only be booked once per day from this provider.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool ApplyPayload(PlayerController player, ServiceAppointmentRecord record, ServiceAppointmentContext context, ServiceAppointmentCompletionResult result, out string failureMessage) {
        failureMessage = null;
        string providerId = context != null ? context.SourceId : record.sourceId;
        string providerName = context != null ? context.SourceName : record.sourceName;
        if(payloadMode == ServiceAppointmentPayloadMode.None || allowEmptyCompletion && !PayloadAvailable()) {
            result.payloadApplied = false;
            return true;
        }

        if(payloadMode == ServiceAppointmentPayloadMode.Service) {
            if(service == null) {
                failureMessage = "Appointment service is missing.";
                return false;
            }

            var serviceResult = service.Use(player, providerId, providerName, this);
            result.serviceId = service.Id;
            result.serviceName = service.DisplayName;
            result.payloadApplied = serviceResult != null && !serviceResult.blocked;
            result.moneyPaidAtCompletion = serviceResult != null ? serviceResult.moneyPaid : 0f;
            result.failureMessage = serviceResult != null ? serviceResult.failureMessage : null;
            if(serviceResult == null || serviceResult.blocked) {
                failureMessage = result.failureMessage ?? "Service could not be completed.";
                return false;
            }

            return true;
        }

        if(servicePackage == null) {
            failureMessage = "Appointment service package is missing.";
            return false;
        }

        var packageResult = servicePackage.Use(player, providerId, providerName, ResolveShopCatalog(context), this);
        result.serviceId = servicePackage.Id;
        result.serviceName = servicePackage.DisplayName;
        result.payloadApplied = packageResult != null && !packageResult.blocked;
        result.moneyPaidAtCompletion = packageResult != null ? packageResult.packagePricePaid + packageResult.individualPricePaid : 0f;
        result.failureMessage = packageResult != null ? packageResult.failureMessage : null;
        if(packageResult == null || packageResult.blocked) {
            failureMessage = result.failureMessage ?? "Service package could not be completed.";
            return false;
        }

        return true;
    }

    bool ResolveSlot(PlayerServiceAppointmentLog appointmentLog, ServiceAppointmentContext context, int requestedDay, int requestedHour, out int scheduledDay, out int scheduledHour, out string failureMessage) {
        scheduledDay = 0;
        scheduledHour = 0;
        int earliestAbsoluteHour = GetCurrentAbsoluteHour() + MinimumLeadTimeHours;
        if(requestedDay > 0) {
            int hour = requestedHour >= 0 ? requestedHour : EarliestHour;
            if(!IsValidSlot(requestedDay, hour, earliestAbsoluteHour, out failureMessage)) {
                return false;
            }

            if(!HasSlotCapacity(appointmentLog, context, requestedDay, hour)) {
                failureMessage = $"{DisplayName} slot is full.";
                return false;
            }

            scheduledDay = requestedDay;
            scheduledHour = hour;
            failureMessage = null;
            return true;
        }

        int currentDay = GetCurrentDay();
        for(int day = currentDay; day <= currentDay + MaxLookAheadDays; day++) {
            for(int hour = EarliestHour; hour <= LatestHour; hour += SlotIntervalHours) {
                if(!IsValidSlot(day, hour, earliestAbsoluteHour, out _)) {
                    continue;
                }

                if(!HasSlotCapacity(appointmentLog, context, day, hour)) {
                    continue;
                }

                scheduledDay = day;
                scheduledHour = hour;
                failureMessage = null;
                return true;
            }
        }

        failureMessage = $"No available {DisplayName} slot was found.";
        return false;
    }

    bool IsValidSlot(int day, int hour, int earliestAbsoluteHour, out string failureMessage) {
        day = Mathf.Max(1, day);
        hour = Mathf.Clamp(hour, 0, 23);
        if(scheduleMode == ServiceAppointmentScheduleMode.ManualOnly) {
            failureMessage = $"{DisplayName} must be booked manually.";
            return false;
        }

        if(day < StartDay) {
            failureMessage = $"{DisplayName} starts on day {StartDay}.";
            return false;
        }

        if(hour < EarliestHour || hour > LatestHour) {
            failureMessage = $"{DisplayName} is available between {EarliestHour:00}:00 and {LatestHour:00}:00.";
            return false;
        }

        if((hour - EarliestHour) % SlotIntervalHours != 0) {
            failureMessage = $"{DisplayName} is not available at that slot interval.";
            return false;
        }

        if(day * 24 + hour < earliestAbsoluteHour) {
            failureMessage = $"{DisplayName} requires {MinimumLeadTimeHours} hour(s) lead time.";
            return false;
        }

        bool validDay = scheduleMode switch {
            ServiceAppointmentScheduleMode.Daily => true,
            ServiceAppointmentScheduleMode.EveryNDays => (day - StartDay) % RepeatEveryDays == 0,
            ServiceAppointmentScheduleMode.Weekly => activeWeekDays == null || activeWeekDays.Count == 0 || activeWeekDays.Contains(GetWeekDay(day)),
            ServiceAppointmentScheduleMode.CalendarEventScheduled => calendarEvent != null && calendarEvent.IsScheduledForDay(day),
            _ => false
        };

        failureMessage = validDay ? null : $"{DisplayName} is not available that day.";
        return validDay;
    }

    bool HasSlotCapacity(PlayerServiceAppointmentLog appointmentLog, ServiceAppointmentContext context, int day, int hour) {
        if(MaxBookingsPerSlot <= 0 || appointmentLog == null) {
            return true;
        }

        string sourceId = context != null ? context.SourceId : null;
        return appointmentLog.GetBookedCountAtSlot(this, sourceId, day, hour) < MaxBookingsPerSlot;
    }

    bool PayloadAvailable() {
        return payloadMode == ServiceAppointmentPayloadMode.None
            || payloadMode == ServiceAppointmentPayloadMode.Service && service != null
            || payloadMode == ServiceAppointmentPayloadMode.ServicePackage && servicePackage != null
            || allowEmptyCompletion;
    }

    string GetPayloadId() {
        if(payloadMode == ServiceAppointmentPayloadMode.Service && service != null) {
            return service.Id;
        }

        if(payloadMode == ServiceAppointmentPayloadMode.ServicePackage && servicePackage != null) {
            return servicePackage.Id;
        }

        return string.Empty;
    }

    string GetPayloadName() {
        if(payloadMode == ServiceAppointmentPayloadMode.Service && service != null) {
            return service.DisplayName;
        }

        if(payloadMode == ServiceAppointmentPayloadMode.ServicePackage && servicePackage != null) {
            return servicePackage.DisplayName;
        }

        return DisplayName;
    }

    float GetBookingFee(PlayerController player) {
        float fee = BookingFee;
        var catalog = shopContext != null ? shopContext.Catalog : null;
        if(useShopPriceMultiplier && catalog != null) {
            fee *= catalog.BuyPriceMultiplier;
        }

        if(useSponsorDiscounts && catalog != null) {
            fee *= player != null ? player.GetComponent<PlayerSponsorLog>()?.GetBestBuyPriceMultiplier(catalog, null) ?? 1f : 1f;
        }

        return Mathf.Max(0f, Mathf.Ceil(fee));
    }

    float GetExpectedCompletionCost(PlayerController player) {
        if(payloadMode == ServiceAppointmentPayloadMode.Service && service != null) {
            return service.MoneyCost;
        }

        if(payloadMode == ServiceAppointmentPayloadMode.ServicePackage && servicePackage != null) {
            return servicePackage.GetExpectedTotalPrice(player, shopContext != null ? shopContext.Catalog : null);
        }

        return 0f;
    }

    int GetCancellationDeadline(int bookedAbsoluteHour, int scheduledAbsoluteHour) {
        if(!allowCancellation) {
            return bookedAbsoluteHour;
        }

        if(cancellationWindowHours <= 0) {
            return scheduledAbsoluteHour;
        }

        return Mathf.Min(scheduledAbsoluteHour, bookedAbsoluteHour + cancellationWindowHours);
    }

    ShopCatalogDefinition ResolveShopCatalog(ServiceAppointmentContext context) {
        if(context != null && context.ShopContext != null) {
            return context.ShopContext.Catalog;
        }

        return shopContext != null ? shopContext.Catalog : null;
    }

    bool MatchesTags(List<string> requiredTags, ConsequenceRequirementMatchMode matchMode, Func<string, bool> hasTag) {
        var activeTags = requiredTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(activeTags.Count == 0) {
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            return activeTags.Any(hasTag);
        }

        return activeTags.All(hasTag);
    }

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(day - 1) % 7;
        return (WeekDay)index;
    }

    void PublishAppointmentEvent(GameEventDefinition eventDefinition, string phase, ServiceAppointmentQuote quote, PlayerController player, ServiceAppointmentContext context, GameEventImportance importance, string failureMessage) {
        PublishAppointmentEvent(eventDefinition, phase, quote != null ? quote.ToEventSnapshot() : null, player, context, importance, failureMessage, this);
    }

    void PublishAppointmentEvent(GameEventDefinition eventDefinition, string phase, ServiceAppointmentRecord record, PlayerController player, ServiceAppointmentContext context, GameEventImportance importance, string failureMessage) {
        PublishAppointmentEvent(eventDefinition, phase, record != null ? record.ToEventSnapshot() : null, player, context, importance, failureMessage, this);
    }

    void PublishAppointmentEvent(GameEventDefinition eventDefinition, string phase, ServiceAppointmentEventSnapshot snapshot, PlayerController player, ServiceAppointmentContext context, GameEventImportance importance, string failureMessage, UnityEngine.Object eventContext) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"service.appointment.{phase}.{Id}.{snapshot?.sourceId ?? context?.SourceId ?? "source"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : $"{DisplayName} appointment {phase}.",
            GameEventCategory.Calendar,
            importance,
            eventContext != null ? eventContext : player != null ? player : this,
            "ServiceAppointmentDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("appointmentId", Id),
            GameEventPublishing.Value("appointmentName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", snapshot?.sourceId ?? context?.SourceId ?? string.Empty),
            GameEventPublishing.Value("scheduledDay", snapshot?.scheduledDay ?? 0),
            GameEventPublishing.Value("scheduledHour", snapshot?.scheduledHour ?? 0),
            GameEventPublishing.Value("scheduledAbsoluteHour", snapshot?.scheduledAbsoluteHour ?? 0),
            GameEventPublishing.Value("bookingFee", snapshot?.bookingFee ?? 0f),
            GameEventPublishing.Value("payloadMode", payloadMode));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

public class ServiceAppointmentSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source/provider id written into appointment records. Empty uses shop id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source/provider name written into appointment records. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Appointment")]
    [Tooltip("Appointment definition offered by this source.")]
    [SerializeField] ServiceAppointmentDefinition appointment;
    [Tooltip("Optional shop context used for appointment/package pricing.")]
    [SerializeField] ShopCatalog shopContext;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Requested in-game day for booking. 0 means automatically find the next available slot.")]
    [Min(0)]
    [SerializeField] int requestedDay;
    [Tooltip("Requested in-game hour for booking. -1 means appointment default earliest hour.")]
    [Range(-1, 23)]
    [SerializeField] int requestedHour = -1;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ServiceAppointmentSourceAction triggerAction = ServiceAppointmentSourceAction.BookAppointment;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ServiceAppointmentSourceAction interactAction = ServiceAppointmentSourceAction.BookAppointment;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Provider Filters")]
    [Tooltip("Free-form tags used by appointment provider-tag requirements.")]
    [SerializeField] List<string> providerTags = new List<string>();

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked appointment actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful appointment actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public ServiceAppointmentDefinition Appointment => appointment;
    public ShopCatalog ShopContext => shopContext;
    public ServiceAppointmentSourceAction TriggerAction => triggerAction;
    public ServiceAppointmentSourceAction InteractAction => interactAction;
    public IReadOnlyList<string> ProviderTags => providerTags != null ? (IReadOnlyList<string>)providerTags : Array.Empty<string>();

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public bool TryPreview(PlayerController player, out ServiceAppointmentQuote quote, out string failureMessage) {
        player = ResolvePlayer(player);
        quote = null;
        if(!ResolveContext(player, out var log, out var context, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!appointment.TryBuildQuote(player, log, context, requestedDay, requestedHour, out quote, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        return true;
    }

    public bool TryBook(PlayerController player, out ServiceAppointmentRecord record, out string failureMessage) {
        player = ResolvePlayer(player);
        record = null;
        if(!ResolveContext(player, out var log, out var context, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!appointment.TryBook(player, log, context, requestedDay, requestedHour, out record, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{appointment.DisplayName} booked for day {record.scheduledDay} at {record.scheduledHour:00}:00.", GameDebugCategory.Activity, this, "ServiceAppointmentSource");
        }

        return true;
    }

    public bool TryCompleteDue(PlayerController player, out List<ServiceAppointmentRecord> completed, out string failureMessage) {
        player = ResolvePlayer(player);
        completed = new List<ServiceAppointmentRecord>();
        if(!ResolveContext(player, out var log, out var context, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!log.TryCompleteDueAppointments(player, SourceId, allowAutoAppointments: false, allowClaimAppointments: true, out completed, out failureMessage, context, this)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} completed {completed.Count} appointment(s).", GameDebugCategory.Activity, this, "ServiceAppointmentSource");
        }

        return true;
    }

    public bool TryCancelLatestPending(PlayerController player, out ServiceAppointmentRecord cancelled, out string failureMessage) {
        player = ResolvePlayer(player);
        cancelled = null;
        if(player == null) {
            failureMessage = "A player is required for appointment cancellation.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var log = player.GetComponent<PlayerServiceAppointmentLog>();
        var latest = log != null ? log.GetLatestPendingAppointment(appointment, SourceId) : null;
        if(latest == null) {
            failureMessage = "No pending appointment was found.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(!log.TryCancelAppointment(player, latest.recordId, out cancelled, out failureMessage, this)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} cancelled appointment.", GameDebugCategory.Activity, this, "ServiceAppointmentSource");
        }

        return true;
    }

    bool ApplyAction(ServiceAppointmentSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ServiceAppointmentSourceAction.PreviewNextSlot:
                if(TryPreview(player, out var quote, out feedback)) {
                    feedback = quote != null ? quote.BuildSummary() : "Appointment quote ready.";
                    return true;
                }
                return false;
            case ServiceAppointmentSourceAction.CompleteDueAppointments:
                if(TryCompleteDue(player, out var completed, out feedback)) {
                    feedback = $"Completed {completed.Count} appointment(s).";
                    return true;
                }
                return false;
            case ServiceAppointmentSourceAction.CancelLatestPending:
                if(TryCancelLatestPending(player, out var cancelled, out feedback)) {
                    feedback = cancelled != null ? $"Appointment cancelled. Refunded {cancelled.refundAmount:0}." : "Appointment cancelled.";
                    return true;
                }
                return false;
            default:
                if(TryBook(player, out var record, out feedback)) {
                    feedback = record != null ? $"Appointment booked for day {record.scheduledDay} at {record.scheduledHour:00}:00." : "Appointment booked.";
                    return true;
                }
                return false;
        }
    }

    bool ResolveContext(PlayerController player, out PlayerServiceAppointmentLog log, out ServiceAppointmentContext context, out string failureMessage) {
        log = null;
        context = null;
        if(player == null) {
            failureMessage = "A player is required for appointments.";
            return false;
        }

        if(appointment == null) {
            failureMessage = "Appointment source has no appointment assigned.";
            return false;
        }

        log = player.GetComponent<PlayerServiceAppointmentLog>() ?? player.gameObject.AddComponent<PlayerServiceAppointmentLog>();
        context = new ServiceAppointmentContext(SourceId, DisplayName, shopContext, providerTags);
        failureMessage = null;
        return true;
    }

    PlayerController ResolvePlayer(PlayerController player) {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(player != null) {
            return player;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        if(shopContext != null) {
            return $"shop:{shopContext.ShopId}";
        }

        return gameObject.name;
    }

    void RecordBlocked(PlayerController player, string failureMessage) {
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Activity, player != null ? player : this, "ServiceAppointmentSource");
        }
    }
}

public class PlayerServiceAppointmentLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum appointment records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecords = 200;
    [Tooltip("If enabled, due Auto Complete appointments are completed when time changes.")]
    [SerializeField] bool autoCompleteDueAppointments = true;
    [Tooltip("Runtime/save history of service appointments.")]
    [SerializeField] List<ServiceAppointmentRecord> records = new List<ServiceAppointmentRecord>();

    public IReadOnlyList<ServiceAppointmentRecord> Records => records;
    public event Action<ServiceAppointmentRecord> OnAppointmentBooked;
    public event Action<ServiceAppointmentRecord> OnAppointmentCompleted;
    public event Action<ServiceAppointmentRecord> OnAppointmentCancelled;
    public event Action OnAppointmentsChanged;

    void OnEnable() {
        SubscribeToTime();
    }

    void OnDisable() {
        UnsubscribeFromTime();
    }

    public ServiceAppointmentRecord RecordBooking(ServiceAppointmentDefinition appointment, ServiceAppointmentQuote quote, ServiceAppointmentContext context) {
        if(appointment == null || quote == null) {
            return null;
        }

        var record = new ServiceAppointmentRecord(quote) {
            recordId = Guid.NewGuid().ToString("N"),
            appointmentId = appointment.Id,
            appointmentName = appointment.DisplayName,
            category = appointment.Category,
            sourceId = context != null ? context.SourceId : quote.sourceId,
            sourceName = context != null ? context.SourceName : quote.sourceName
        };

        records.Add(record);
        TrimHistory();
        OnAppointmentBooked?.Invoke(record);
        OnAppointmentsChanged?.Invoke();
        return record;
    }

    public bool TryCompleteDueAppointments(
        PlayerController player,
        string sourceId,
        bool allowAutoAppointments,
        bool allowClaimAppointments,
        out List<ServiceAppointmentRecord> completed,
        out string failureMessage,
        ServiceAppointmentContext context = null,
        UnityEngine.Object unityContext = null
    ) {
        completed = new List<ServiceAppointmentRecord>();
        player ??= GetComponent<PlayerController>();
        if(player == null) {
            failureMessage = "A player is required to complete appointments.";
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        var dueRecords = records
            .Where(record => record != null && record.IsPending && record.IsDue(currentHour))
            .Where(record => (allowAutoAppointments && record.completionMode == ServiceAppointmentCompletionMode.AutoCompleteWhenDue)
                || (allowClaimAppointments && record.completionMode == ServiceAppointmentCompletionMode.ClaimAtProvider && record.MatchesProvider(sourceId)))
            .OrderBy(record => record.scheduledAbsoluteHour)
            .ToList();

        foreach(var record in dueRecords) {
            if(TryCompleteAppointment(player, record.recordId, out var completedRecord, out _, context, unityContext)) {
                completed.Add(completedRecord);
            }
        }

        if(completed.Count == 0) {
            failureMessage = "No due appointments were found.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryCompleteAppointment(
        PlayerController player,
        string recordId,
        out ServiceAppointmentRecord completedRecord,
        out string failureMessage,
        ServiceAppointmentContext context = null,
        UnityEngine.Object unityContext = null
    ) {
        completedRecord = null;
        player ??= GetComponent<PlayerController>();
        var record = FindRecord(recordId);
        if(record == null) {
            failureMessage = "Appointment record was not found.";
            return false;
        }

        var appointment = record.ResolveDefinition();
        if(appointment == null) {
            failureMessage = "Appointment definition could not be resolved.";
            return false;
        }

        var completionContext = context ?? new ServiceAppointmentContext(record.sourceId, record.sourceName, null, null);
        if(!appointment.TryComplete(player, record, completionContext, out var result, out failureMessage)) {
            record.lastFailureMessage = failureMessage;
            OnAppointmentsChanged?.Invoke();
            return false;
        }

        record.completed = true;
        record.completedDay = result.completedDay;
        record.completedAbsoluteHour = result.completedAbsoluteHour;
        record.completedLate = result.completedLate;
        record.payloadApplied = result.payloadApplied;
        record.moneyPaidAtCompletion = result.moneyPaidAtCompletion;
        record.lastFailureMessage = null;
        completedRecord = record;
        OnAppointmentCompleted?.Invoke(record);
        OnAppointmentsChanged?.Invoke();
        return true;
    }

    public bool TryCancelAppointment(PlayerController player, string recordId, out ServiceAppointmentRecord cancelledRecord, out string failureMessage, UnityEngine.Object context = null) {
        cancelledRecord = null;
        player ??= GetComponent<PlayerController>();
        var record = FindRecord(recordId);
        if(record == null) {
            failureMessage = "Appointment record was not found.";
            return false;
        }

        if(!record.IsPending) {
            failureMessage = "Only pending appointments can be cancelled.";
            return false;
        }

        if(!record.allowCancellation) {
            failureMessage = "This appointment cannot be cancelled.";
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        if(currentHour > record.cancellationDeadlineAbsoluteHour) {
            failureMessage = "The cancellation window has closed.";
            return false;
        }

        if(record.refundAmount > 0f) {
            Wallet.i?.AddMoney(record.refundAmount);
        }

        record.cancelled = true;
        record.cancelledDay = GetCurrentDay();
        record.cancelledAbsoluteHour = currentHour;
        cancelledRecord = record;
        OnAppointmentCancelled?.Invoke(record);
        OnAppointmentsChanged?.Invoke();
        record.ResolveDefinition()?.PublishCancelled(record, player, context != null ? context : this);
        failureMessage = null;
        return true;
    }

    public int GetPendingCount(ServiceAppointmentDefinition appointment = null, string sourceId = null) {
        return records.Count(record => record != null
            && record.IsPending
            && (appointment == null || record.appointmentId == appointment.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId));
    }

    public int GetBookedCountAtSlot(ServiceAppointmentDefinition appointment, string sourceId, int day, int hour) {
        return records.Count(record => record != null
            && record.IsPending
            && (appointment == null || record.appointmentId == appointment.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && record.scheduledDay == day
            && record.scheduledHour == hour);
    }

    public bool HasBookedOnDay(ServiceAppointmentDefinition appointment, string sourceId, int day) {
        return records.Any(record => record != null
            && record.appointmentId == (appointment != null ? appointment.Id : record.appointmentId)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && record.bookedDay == day
            && !record.cancelled);
    }

    public ServiceAppointmentRecord GetLatestPendingAppointment(ServiceAppointmentDefinition appointment = null, string sourceId = null) {
        return records
            .Where(record => record != null && record.IsPending)
            .Where(record => appointment == null || record.appointmentId == appointment.Id)
            .Where(record => string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            .OrderByDescending(record => record.bookedAbsoluteHour)
            .FirstOrDefault();
    }

    public ServiceAppointmentRecord FindRecord(string recordId) {
        if(string.IsNullOrWhiteSpace(recordId)) {
            return null;
        }

        return records.FirstOrDefault(record => record != null && record.recordId == recordId);
    }

    void HandleTimeChanged() {
        if(!autoCompleteDueAppointments) {
            return;
        }

        TryCompleteDueAppointments(GetComponent<PlayerController>(), null, allowAutoAppointments: true, allowClaimAppointments: false, out _, out _, null, this);
    }

    void SubscribeToTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleTimeChanged;
    }

    void UnsubscribeFromTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
    }

    void TrimHistory() {
        if(maxRecords <= 0 || records.Count <= maxRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.bookedAbsoluteHour)
            .Take(maxRecords)
            .OrderBy(record => record.bookedAbsoluteHour)
            .ToList();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerServiceAppointmentLogSaveData {
            records = records.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerServiceAppointmentLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ServiceAppointmentRecord>();
        TrimHistory();
        OnAppointmentsChanged?.Invoke();
    }
}

public class ServiceAppointmentContext {
    public string SourceId { get; }
    public string SourceName { get; }
    public ShopCatalog ShopContext { get; }
    public IReadOnlyList<string> ProviderTags => providerTags;

    readonly List<string> providerTags = new List<string>();

    public ServiceAppointmentContext(string sourceId, string sourceName, ShopCatalog shopContext, IEnumerable<string> providerTags) {
        SourceId = string.IsNullOrWhiteSpace(sourceId) ? "appointment" : sourceId;
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? SourceId : sourceName;
        ShopContext = shopContext;
        AddProviderTags(providerTags);
    }

    public bool HasProviderTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && providerTags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void AddProviderTags(IEnumerable<string> tags) {
        if(tags == null) {
            return;
        }

        foreach(var tag in tags) {
            if(!string.IsNullOrWhiteSpace(tag) && !providerTags.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase))) {
                providerTags.Add(tag);
            }
        }
    }
}

[Serializable]
public class ServiceAppointmentQuote {
    [Tooltip("Appointment definition id that produced this quote.")]
    public string appointmentId;
    [Tooltip("Appointment display name copied for fallback/debug output.")]
    public string appointmentName;
    [Tooltip("Appointment category copied for sorting/filtering.")]
    public PlayerServiceCategory category;
    [Tooltip("Payload mode that will run at completion.")]
    public ServiceAppointmentPayloadMode payloadMode;
    [Tooltip("Service or package id that will run at completion.")]
    public string payloadId;
    [Tooltip("Service or package display name that will run at completion.")]
    public string payloadName;
    [Tooltip("Provider/source id used by this appointment.")]
    public string sourceId;
    [Tooltip("Provider/source display name used by this appointment.")]
    public string sourceName;
    [Tooltip("Optional shop id used for pricing context.")]
    public string shopId;
    [Tooltip("Scheduled in-game day.")]
    public int scheduledDay;
    [Tooltip("Scheduled in-game hour.")]
    public int scheduledHour;
    [Tooltip("Absolute scheduled in-game hour.")]
    public int scheduledAbsoluteHour;
    [Tooltip("Appointment duration in hours.")]
    public int durationHours;
    [Tooltip("How the appointment can be completed once due.")]
    public ServiceAppointmentCompletionMode completionMode;
    [Tooltip("Money paid immediately when booking.")]
    public float bookingFee;
    [Tooltip("Estimated service/package money cost due at completion.")]
    public float expectedCompletionCost;
    [Tooltip("Money due now. Usually equals Booking Fee when Charge Wallet On Booking is enabled.")]
    public float totalDueNow;
    [Tooltip("If enabled, this appointment can be cancelled while pending and inside the cancellation window.")]
    public bool allowCancellation;
    [Tooltip("Absolute hour after which cancellation is blocked.")]
    public int cancellationDeadlineAbsoluteHour;
    [Tooltip("Money refunded if cancelled inside the cancellation window.")]
    public float cancellationRefundAmount;
    [Tooltip("Grace hours before late completion is marked.")]
    public int graceHours;

    public string BuildSummary() {
        return $"{appointmentName}: day {scheduledDay} {scheduledHour:00}:00, booking {bookingFee:0}, completion estimate {expectedCompletionCost:0}.";
    }

    public ServiceAppointmentEventSnapshot ToEventSnapshot() {
        return new ServiceAppointmentEventSnapshot {
            sourceId = sourceId,
            scheduledDay = scheduledDay,
            scheduledHour = scheduledHour,
            scheduledAbsoluteHour = scheduledAbsoluteHour,
            bookingFee = bookingFee
        };
    }
}

[Serializable]
public class ServiceAppointmentRecord {
    [Tooltip("Unique runtime/save id for this appointment.")]
    public string recordId;
    [Tooltip("Appointment definition id.")]
    public string appointmentId;
    [Tooltip("Appointment display name copied for fallback/debug output.")]
    public string appointmentName;
    [Tooltip("Appointment category copied for sorting/filtering.")]
    public PlayerServiceCategory category;
    [Tooltip("Payload mode that runs at completion.")]
    public ServiceAppointmentPayloadMode payloadMode;
    [Tooltip("Service or package id that runs at completion.")]
    public string payloadId;
    [Tooltip("Service or package display name that runs at completion.")]
    public string payloadName;
    [Tooltip("Provider/source id used by this appointment.")]
    public string sourceId;
    [Tooltip("Provider/source display name used by this appointment.")]
    public string sourceName;
    [Tooltip("Optional shop id used for pricing context.")]
    public string shopId;
    [Tooltip("Scheduled in-game day.")]
    public int scheduledDay;
    [Tooltip("Scheduled in-game hour.")]
    public int scheduledHour;
    [Tooltip("Absolute scheduled in-game hour.")]
    public int scheduledAbsoluteHour;
    [Tooltip("Appointment duration in hours.")]
    public int durationHours;
    [Tooltip("How the appointment can be completed once due.")]
    public ServiceAppointmentCompletionMode completionMode;
    [Tooltip("Money paid immediately when booking.")]
    public float bookingFee;
    [Tooltip("Estimated service/package money cost due at completion.")]
    public float expectedCompletionCost;
    [Tooltip("Money refunded if cancelled.")]
    public float refundAmount;
    [Tooltip("If enabled, this appointment can be cancelled while pending and inside the cancellation window.")]
    public bool allowCancellation;
    [Tooltip("Absolute hour after which cancellation is blocked.")]
    public int cancellationDeadlineAbsoluteHour;
    [Tooltip("Grace hours before late completion is marked.")]
    public int graceHours;
    [Tooltip("In-game day when this appointment was booked.")]
    public int bookedDay;
    [Tooltip("Absolute in-game hour when this appointment was booked.")]
    public int bookedAbsoluteHour;
    [Tooltip("If enabled, this appointment was completed.")]
    public bool completed;
    [Tooltip("In-game day when this appointment was completed.")]
    public int completedDay = -1;
    [Tooltip("Absolute in-game hour when this appointment was completed.")]
    public int completedAbsoluteHour = -1;
    [Tooltip("If enabled, this appointment was completed after grace period.")]
    public bool completedLate;
    [Tooltip("If enabled, the payload service/package was applied.")]
    public bool payloadApplied;
    [Tooltip("Money paid by the service/package at completion.")]
    public float moneyPaidAtCompletion;
    [Tooltip("If enabled, this appointment was cancelled.")]
    public bool cancelled;
    [Tooltip("In-game day when this appointment was cancelled.")]
    public int cancelledDay = -1;
    [Tooltip("Absolute in-game hour when this appointment was cancelled.")]
    public int cancelledAbsoluteHour = -1;
    [Tooltip("Most recent failure message when completion failed.")]
    public string lastFailureMessage;

    public bool IsPending => !completed && !cancelled;

    public ServiceAppointmentRecord() {
    }

    public ServiceAppointmentRecord(ServiceAppointmentQuote quote) {
        if(quote == null) {
            return;
        }

        appointmentId = quote.appointmentId;
        appointmentName = quote.appointmentName;
        category = quote.category;
        payloadMode = quote.payloadMode;
        payloadId = quote.payloadId;
        payloadName = quote.payloadName;
        sourceId = quote.sourceId;
        sourceName = quote.sourceName;
        shopId = quote.shopId;
        scheduledDay = quote.scheduledDay;
        scheduledHour = quote.scheduledHour;
        scheduledAbsoluteHour = quote.scheduledAbsoluteHour;
        durationHours = quote.durationHours;
        completionMode = quote.completionMode;
        bookingFee = quote.bookingFee;
        expectedCompletionCost = quote.expectedCompletionCost;
        refundAmount = quote.cancellationRefundAmount;
        allowCancellation = quote.allowCancellation;
        cancellationDeadlineAbsoluteHour = quote.cancellationDeadlineAbsoluteHour;
        graceHours = quote.graceHours;
        bookedDay = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        bookedAbsoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public bool IsDue(int currentAbsoluteHour) {
        return currentAbsoluteHour >= scheduledAbsoluteHour;
    }

    public bool MatchesProvider(string providerId) {
        return string.IsNullOrWhiteSpace(providerId)
            || string.Equals(sourceId, providerId, StringComparison.OrdinalIgnoreCase);
    }

    public ServiceAppointmentDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(appointmentId)) {
            return null;
        }

        return Resources.LoadAll<ServiceAppointmentDefinition>("").FirstOrDefault(definition => definition != null && definition.Id == appointmentId);
    }

    public ServiceAppointmentEventSnapshot ToEventSnapshot() {
        return new ServiceAppointmentEventSnapshot {
            sourceId = sourceId,
            scheduledDay = scheduledDay,
            scheduledHour = scheduledHour,
            scheduledAbsoluteHour = scheduledAbsoluteHour,
            bookingFee = bookingFee
        };
    }

    public ServiceAppointmentRecord Clone() {
        return new ServiceAppointmentRecord {
            recordId = recordId,
            appointmentId = appointmentId,
            appointmentName = appointmentName,
            category = category,
            payloadMode = payloadMode,
            payloadId = payloadId,
            payloadName = payloadName,
            sourceId = sourceId,
            sourceName = sourceName,
            shopId = shopId,
            scheduledDay = scheduledDay,
            scheduledHour = scheduledHour,
            scheduledAbsoluteHour = scheduledAbsoluteHour,
            durationHours = durationHours,
            completionMode = completionMode,
            bookingFee = bookingFee,
            expectedCompletionCost = expectedCompletionCost,
            refundAmount = refundAmount,
            allowCancellation = allowCancellation,
            cancellationDeadlineAbsoluteHour = cancellationDeadlineAbsoluteHour,
            graceHours = graceHours,
            bookedDay = bookedDay,
            bookedAbsoluteHour = bookedAbsoluteHour,
            completed = completed,
            completedDay = completedDay,
            completedAbsoluteHour = completedAbsoluteHour,
            completedLate = completedLate,
            payloadApplied = payloadApplied,
            moneyPaidAtCompletion = moneyPaidAtCompletion,
            cancelled = cancelled,
            cancelledDay = cancelledDay,
            cancelledAbsoluteHour = cancelledAbsoluteHour,
            lastFailureMessage = lastFailureMessage
        };
    }
}

public class ServiceAppointmentCompletionResult {
    [Tooltip("Runtime/save id of the completed appointment record.")]
    public string recordId;
    [Tooltip("Appointment definition id.")]
    public string appointmentId;
    [Tooltip("If enabled, completion succeeded.")]
    public bool completed;
    [Tooltip("If enabled, completion was blocked.")]
    public bool blocked;
    [Tooltip("If enabled, the appointment was completed after its grace period.")]
    public bool completedLate;
    [Tooltip("If enabled, the service/package payload was applied.")]
    public bool payloadApplied;
    [Tooltip("Service/package id applied at completion.")]
    public string serviceId;
    [Tooltip("Service/package display name applied at completion.")]
    public string serviceName;
    [Tooltip("Money charged by the service/package at completion.")]
    public float moneyPaidAtCompletion;
    [Tooltip("Most recent failure reason when completion was blocked.")]
    public string failureMessage;
    [Tooltip("In-game day when the appointment completed.")]
    public int completedDay;
    [Tooltip("Absolute in-game hour when the appointment completed.")]
    public int completedAbsoluteHour;

    public ServiceAppointmentCompletionResult(ServiceAppointmentRecord record) {
        recordId = record != null ? record.recordId : string.Empty;
        appointmentId = record != null ? record.appointmentId : string.Empty;
    }
}

public class ServiceAppointmentEventSnapshot {
    [Tooltip("Provider/source id copied into appointment events.")]
    public string sourceId;
    [Tooltip("Scheduled in-game day copied into appointment events.")]
    public int scheduledDay;
    [Tooltip("Scheduled in-game hour copied into appointment events.")]
    public int scheduledHour;
    [Tooltip("Absolute scheduled in-game hour copied into appointment events.")]
    public int scheduledAbsoluteHour;
    [Tooltip("Booking fee copied into appointment events.")]
    public float bookingFee;
}

[Serializable]
public class PlayerServiceAppointmentLogSaveData {
    [Tooltip("Saved service appointment records.")]
    public List<ServiceAppointmentRecord> records;
}
