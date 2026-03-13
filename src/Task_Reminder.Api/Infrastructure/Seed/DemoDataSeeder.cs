using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Entities;
using Task_Reminder.Shared;
using TaskStatus = Task_Reminder.Shared.TaskStatus;

namespace Task_Reminder.Api.Infrastructure.Seed;

public static class DemoDataSeeder
{
    public static async Task<bool> SeedAsync(TaskReminderDbContext dbContext, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var todayLocal = DateOnly.FromDateTime(DateTime.Today);
        var seededAny = false;

        var users = await EnsureUsersAsync(dbContext, nowUtc, cancellationToken);
        seededAny |= users.seededAny;

        var frontDeskUsers = users.users.Where(x => x.Role == UserRole.FrontDesk).ToArray();
        var managerUser = users.users.First(x => x.Role == UserRole.Manager);
        var adminUser = users.users.First(x => x.Role == UserRole.Admin);

        if (!await dbContext.OfficeSettings.AnyAsync(cancellationToken))
        {
            dbContext.OfficeSettings.Add(new OfficeSettings
            {
                Id = Guid.NewGuid(),
                OfficeName = "Bright Smile Dental Center",
                BusinessHoursSummary = "Mon-Thu 7:30 AM - 5:00 PM, Fri 7:30 AM - 2:00 PM",
                ConfirmationLeadHours = 24,
                InsuranceVerificationLeadDays = 2,
                OverdueEscalationMinutes = 45,
                NoShowFollowUpDelayHours = 4,
                ManagerEscalationUserId = managerUser.Id,
                DefaultReminderIntervalMinutes = 30,
                TimeZoneId = "Eastern Standard Time",
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });
            seededAny = true;
        }

        if (!await dbContext.RecurringTaskDefinitions.AnyAsync(cancellationToken))
        {
            dbContext.RecurringTaskDefinitions.AddRange(CreateRecurringDefinitions(todayLocal, nowUtc, frontDeskUsers, managerUser, adminUser));
            seededAny = true;
        }

        if (!await dbContext.AppointmentWorkItems.AnyAsync(cancellationToken))
        {
            var appointments = CreateAppointments(todayLocal, nowUtc);
            dbContext.AppointmentWorkItems.AddRange(appointments);
            await dbContext.SaveChangesAsync(cancellationToken);

            var insuranceItems = CreateInsuranceItems(appointments, nowUtc, managerUser, adminUser);
            var balanceItems = CreateBalanceItems(appointments, nowUtc);
            dbContext.InsuranceWorkItems.AddRange(insuranceItems);
            dbContext.BalanceFollowUpWorkItems.AddRange(balanceItems);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.ContactLogs.AddRange(CreateWorkflowContactLogs(appointments, insuranceItems, balanceItems, frontDeskUsers, managerUser, adminUser, nowUtc));
            seededAny = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Tasks.AnyAsync(cancellationToken))
        {
            var appointments = await dbContext.AppointmentWorkItems.AsNoTracking().OrderBy(x => x.AppointmentDateLocal).ThenBy(x => x.AppointmentTimeLocal).ToListAsync(cancellationToken);
            var insuranceItems = await dbContext.InsuranceWorkItems.AsNoTracking().OrderBy(x => x.PatientName).ToListAsync(cancellationToken);
            var balanceItems = await dbContext.BalanceFollowUpWorkItems.AsNoTracking().OrderBy(x => x.PatientName).ToListAsync(cancellationToken);
            var tasks = CreateTasks(nowUtc, appointments, insuranceItems, balanceItems, frontDeskUsers, managerUser, adminUser);

            dbContext.Tasks.AddRange(tasks);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.TaskHistory.AddRange(CreateTaskHistory(tasks, frontDeskUsers, managerUser, adminUser));
            dbContext.ContactLogs.AddRange(CreateTaskContactLogs(tasks, frontDeskUsers, managerUser, nowUtc));
            seededAny = true;
        }

        if (!await dbContext.ExternalIntegrationProviderConfigs.AnyAsync(cancellationToken))
        {
            var configs = CreateIntegrationConfigs(nowUtc);
            dbContext.ExternalIntegrationProviderConfigs.AddRange(configs);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ExternalIntegrationRuns.AddRange(CreateIntegrationRuns(configs, nowUtc));
            seededAny = true;
        }

        if (!await dbContext.AuditEntries.AnyAsync(cancellationToken))
        {
            var sampleTasks = await dbContext.Tasks.AsNoTracking().Take(6).ToListAsync(cancellationToken);
            var sampleAppointments = await dbContext.AppointmentWorkItems.AsNoTracking().Take(4).ToListAsync(cancellationToken);
            dbContext.AuditEntries.AddRange(CreateAuditEntries(sampleTasks, sampleAppointments, managerUser, adminUser, nowUtc));
            seededAny = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return seededAny;
    }

    private static async Task<(User[] users, bool seededAny)> EnsureUsersAsync(TaskReminderDbContext dbContext, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var seededAny = false;
        var desiredUsers = new[]
        {
            new SeedUser("mia", "Mia Front Desk", UserRole.FrontDesk),
            new SeedUser("noah", "Noah Front Desk", UserRole.FrontDesk),
            new SeedUser("emma", "Emma Insurance Manager", UserRole.Manager),
            new SeedUser("ava", "Ava Office Admin", UserRole.Admin)
        };

        foreach (var desiredUser in desiredUsers)
        {
            var existing = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == desiredUser.Username, cancellationToken);
            if (existing is null)
            {
                existing = new User
                {
                    Id = Guid.NewGuid(),
                    DisplayName = desiredUser.DisplayName,
                    Username = desiredUser.Username,
                    IsActive = true,
                    Role = desiredUser.Role,
                    CreatedAtUtc = nowUtc
                };
                dbContext.Users.Add(existing);
                seededAny = true;
            }
            else
            {
                existing.DisplayName = desiredUser.DisplayName;
                existing.Role = desiredUser.Role;
                existing.IsActive = true;
            }

            if (!await dbContext.UserNotificationPreferences.AnyAsync(x => x.UserId == existing.Id, cancellationToken))
            {
                dbContext.UserNotificationPreferences.Add(new UserNotificationPreference
                {
                    UserId = existing.Id,
                    ReceiveAssignedTaskReminders = true,
                    ReceiveUnassignedTaskReminders = true,
                    ReceiveOverdueEscalationAlerts = existing.Role is UserRole.Manager or UserRole.Admin,
                    ReceiveRecurringTaskGenerationAlerts = true,
                    EnableSoundForUrgentReminders = existing.Role is UserRole.Manager or UserRole.Admin
                });
                seededAny = true;
            }
        }

        if (seededAny)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return (await dbContext.Users.OrderBy(x => x.DisplayName).ToArrayAsync(cancellationToken), seededAny);
    }

    private static IReadOnlyList<RecurringTaskDefinition> CreateRecurringDefinitions(DateOnly todayLocal, DateTime nowUtc, IReadOnlyList<User> frontDeskUsers, User managerUser, User adminUser)
    {
        var templates = new[]
        {
            ("Daily opening checklist", TaskCategory.OpeningChecklist, TaskPriority.High, RecurrenceType.Weekdays, new TimeSpan(7, 30, 0)),
            ("Daily closing checklist", TaskCategory.ClosingChecklist, TaskPriority.High, RecurrenceType.Weekdays, new TimeSpan(16, 15, 0)),
            ("Confirm tomorrow hygiene schedule", TaskCategory.AppointmentConfirmation, TaskPriority.High, RecurrenceType.Weekdays, new TimeSpan(15, 0, 0)),
            ("Confirm tomorrow doctor columns", TaskCategory.AppointmentConfirmation, TaskPriority.High, RecurrenceType.Weekdays, new TimeSpan(15, 30, 0)),
            ("Verify tomorrow crowns and implant cases", TaskCategory.InsuranceVerification, TaskPriority.Urgent, RecurrenceType.Weekdays, new TimeSpan(8, 0, 0)),
            ("Check pre-op follow-up list", TaskCategory.TreatmentFollowUp, TaskPriority.Medium, RecurrenceType.Weekdays, new TimeSpan(11, 0, 0)),
            ("Review same-day balances", TaskCategory.BalanceCollection, TaskPriority.Medium, RecurrenceType.Weekdays, new TimeSpan(9, 30, 0)),
            ("Recall list review", TaskCategory.Recall, TaskPriority.Medium, RecurrenceType.Weekly, new TimeSpan(13, 30, 0)),
            ("Insurance aging cleanup", TaskCategory.InsuranceVerification, TaskPriority.High, RecurrenceType.Weekly, new TimeSpan(14, 0, 0)),
            ("Manager queue review", TaskCategory.General, TaskPriority.High, RecurrenceType.Weekdays, new TimeSpan(12, 0, 0)),
            ("Friday reschedule sweep", TaskCategory.TreatmentFollowUp, TaskPriority.Medium, RecurrenceType.Weekly, new TimeSpan(15, 45, 0)),
            ("Collections callback block", TaskCategory.BalanceCollection, TaskPriority.Medium, RecurrenceType.Weekdays, new TimeSpan(10, 15, 0))
        };

        var definitions = new List<RecurringTaskDefinition>();
        for (var index = 0; index < templates.Length; index++)
        {
            var template = templates[index];
            definitions.Add(new RecurringTaskDefinition
            {
                Id = Guid.NewGuid(),
                Title = template.Item1,
                Description = $"Auto-generated recurring office workflow: {template.Item1}.",
                Category = template.Item2,
                Priority = template.Item3,
                AssignedUserId = frontDeskUsers[index % frontDeskUsers.Count].Id,
                CreatedByUserId = managerUser.Id,
                PatientReference = index % 3 == 0 ? $"RECUR-{index + 1:000}" : null,
                Notes = "Use checklist-style notes to verify steps before marking complete.",
                ReminderRepeatMinutes = template.Item3 == TaskPriority.Urgent ? 20 : 30,
                EscalateAfterMinutes = template.Item3 is TaskPriority.High or TaskPriority.Urgent ? 45 : 60,
                EscalateToUserId = index % 4 == 0 ? adminUser.Id : managerUser.Id,
                RecurrenceType = template.Item4,
                RecurrenceInterval = 1,
                DaysOfWeek = template.Item4 == RecurrenceType.Weekly ? "Monday,Wednesday,Friday" : null,
                TimeOfDayLocal = template.Item5,
                StartDateLocal = todayLocal.AddDays(-14),
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });
        }

        return definitions;
    }

    private static IReadOnlyList<AppointmentWorkItem> CreateAppointments(DateOnly todayLocal, DateTime nowUtc)
    {
        var patients = new[]
        {
            "Sophia Carter", "Liam Peterson", "Mason Rivera", "Olivia Brooks", "Aiden Hall", "Charlotte Reed",
            "Isabella Kim", "Ethan Howard", "Amelia Foster", "James Bennett", "Harper Collins", "Benjamin Price"
        };
        var providers = new[] { "Dr. Lee", "Dr. Shah", "Dr. Patel", "Dr. Nguyen" };
        var appointmentTypes = new[] { "Hygiene Recall", "Crown Prep", "Crown Delivery", "Implant Consult", "Emergency Exam", "New Patient", "Filling", "Limited Exam" };
        var appointments = new List<AppointmentWorkItem>();

        for (var index = 0; index < 60; index++)
        {
            var appointmentDate = todayLocal.AddDays((index % 14) - 4);
            var status = index % 17 == 0
                ? AppointmentStatus.NoShow
                : index % 13 == 0
                    ? AppointmentStatus.Cancelled
                    : index % 11 == 0
                        ? AppointmentStatus.Rescheduled
                        : appointmentDate < todayLocal && index % 7 == 0
                            ? AppointmentStatus.Completed
                            : AppointmentStatus.Scheduled;

            appointments.Add(new AppointmentWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = patients[index % patients.Length],
                PatientReference = $"PT-{5000 + index}",
                AppointmentDateLocal = appointmentDate,
                AppointmentTimeLocal = new TimeSpan(8, 0, 0).Add(TimeSpan.FromMinutes(30 * (index % 16))),
                ProviderName = providers[index % providers.Length],
                AppointmentType = appointmentTypes[index % appointmentTypes.Length],
                Status = status,
                ConfirmationStatus = status switch
                {
                    AppointmentStatus.Completed or AppointmentStatus.Confirmed => AppointmentConfirmationStatus.Confirmed,
                    AppointmentStatus.Cancelled => AppointmentConfirmationStatus.Declined,
                    _ when index % 5 == 0 => AppointmentConfirmationStatus.LeftVoicemail,
                    _ when index % 4 == 0 => AppointmentConfirmationStatus.TextSent,
                    _ when index % 6 == 0 => AppointmentConfirmationStatus.NoAnswer,
                    _ => AppointmentConfirmationStatus.NotStarted
                },
                InsuranceStatus = (index % 6) switch
                {
                    0 => AppointmentInsuranceStatus.PendingVerification,
                    1 => AppointmentInsuranceStatus.Verified,
                    2 => AppointmentInsuranceStatus.IssueFound,
                    3 => AppointmentInsuranceStatus.NotNeeded,
                    4 => AppointmentInsuranceStatus.PendingVerification,
                    _ => AppointmentInsuranceStatus.Ineligible
                },
                BalanceStatus = (index % 5) switch
                {
                    0 => AppointmentBalanceStatus.BalanceDue,
                    1 => AppointmentBalanceStatus.NoBalance,
                    2 => AppointmentBalanceStatus.PaymentArranged,
                    3 => AppointmentBalanceStatus.Collected,
                    _ => AppointmentBalanceStatus.Unknown
                },
                Notes = BuildAppointmentNotes(index, status),
                SourceSystem = "Demo",
                SourceReference = $"APT-DEMO-{index + 1:000}",
                CreatedAtUtc = nowUtc.AddDays(-10).AddMinutes(index * 7),
                UpdatedAtUtc = nowUtc.AddDays(-1).AddMinutes(index * 2)
            });
        }

        return appointments;
    }

    private static IReadOnlyList<InsuranceWorkItem> CreateInsuranceItems(IReadOnlyList<AppointmentWorkItem> appointments, DateTime nowUtc, User managerUser, User adminUser)
    {
        var carriers = new[] { "Delta Dental", "Aetna", "MetLife", "Guardian", "Cigna", "Principal" };
        var plans = new[] { "Premier PPO", "Dental PPO", "Advantage", "Traditional", "Choice", "Elite" };
        var items = new List<InsuranceWorkItem>();

        foreach (var appointment in appointments.Take(36))
        {
            var index = items.Count;
            var status = (index % 5) switch
            {
                0 => InsuranceVerificationStatus.Pending,
                1 => InsuranceVerificationStatus.Verified,
                2 => InsuranceVerificationStatus.InProgress,
                3 => InsuranceVerificationStatus.NeedsManualReview,
                _ => InsuranceVerificationStatus.Failed
            };

            items.Add(new InsuranceWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = appointment.PatientName,
                PatientReference = appointment.PatientReference,
                CarrierName = carriers[index % carriers.Length],
                PlanName = plans[index % plans.Length],
                MemberId = $"MEM-{8000 + index}",
                GroupNumber = $"GRP-{200 + index}",
                PayerId = $"PAY-{100 + index}",
                AppointmentDateLocal = appointment.AppointmentDateLocal,
                VerificationStatus = status,
                EligibilityStatus = (index % 5) switch
                {
                    0 => InsuranceEligibilityStatus.Unknown,
                    1 => InsuranceEligibilityStatus.Active,
                    2 => InsuranceEligibilityStatus.Active,
                    3 => InsuranceEligibilityStatus.MissingSubscriberInfo,
                    _ => InsuranceEligibilityStatus.Inactive
                },
                VerificationMethod = (index % 4) switch
                {
                    0 => InsuranceVerificationMethod.ManualPortal,
                    1 => InsuranceVerificationMethod.Phone,
                    2 => InsuranceVerificationMethod.Clearinghouse,
                    _ => InsuranceVerificationMethod.Imported
                },
                VerificationRequestedAtUtc = nowUtc.AddDays(-3).AddMinutes(index * 9),
                VerificationCompletedAtUtc = status == InsuranceVerificationStatus.Verified ? nowUtc.AddDays(-1).AddMinutes(index) : null,
                VerifiedByUserId = status == InsuranceVerificationStatus.Verified ? (index % 2 == 0 ? managerUser.Id : adminUser.Id) : null,
                CopayAmount = 20 + index,
                DeductibleAmount = 50 + (index * 3),
                AnnualMaximum = 1500,
                RemainingMaximum = 1500 - (index * 25),
                FrequencyNotes = index % 4 == 0 ? "Perio maintenance every 3 months." : null,
                WaitingPeriodNotes = index % 6 == 0 ? "Major services waiting period may apply." : null,
                MissingInfoNotes = index % 5 == 3 ? "Need subscriber DOB and employer name." : null,
                IssueType = status is InsuranceVerificationStatus.Failed or InsuranceVerificationStatus.NeedsManualReview
                    ? ((index % 4) switch
                    {
                        0 => InsuranceIssueType.InactiveCoverage,
                        1 => InsuranceIssueType.MissingMemberId,
                        2 => InsuranceIssueType.WaitingPeriod,
                        _ => InsuranceIssueType.AnnualMaxConcern
                    })
                    : null,
                Notes = status == InsuranceVerificationStatus.Verified ? "Benefits confirmed and copied into patient notes." : "Verification still needs front desk follow-up.",
                SourceSystem = "Demo",
                SourceReference = $"INS-DEMO-{index + 1:000}",
                AppointmentWorkItemId = appointment.Id,
                CreatedAtUtc = nowUtc.AddDays(-5).AddMinutes(index * 4),
                UpdatedAtUtc = nowUtc.AddDays(-1).AddMinutes(index * 2)
            });
        }

        return items;
    }

    private static IReadOnlyList<BalanceFollowUpWorkItem> CreateBalanceItems(IReadOnlyList<AppointmentWorkItem> appointments, DateTime nowUtc)
    {
        var items = new List<BalanceFollowUpWorkItem>();
        foreach (var appointment in appointments.Where(x => x.BalanceStatus != AppointmentBalanceStatus.NoBalance).Take(22))
        {
            var index = items.Count;
            items.Add(new BalanceFollowUpWorkItem
            {
                Id = Guid.NewGuid(),
                PatientName = appointment.PatientName,
                PatientReference = appointment.PatientReference,
                AppointmentWorkItemId = appointment.Id,
                AmountDue = 35 + (index * 18.5m),
                DueReasonNote = index % 3 == 0 ? "Estimated patient portion." : "Outstanding balance follow-up.",
                Status = (index % 5) switch
                {
                    0 => BalanceFollowUpStatus.NotReviewed,
                    1 => BalanceFollowUpStatus.InformedPatient,
                    2 => BalanceFollowUpStatus.PaymentRequested,
                    3 => BalanceFollowUpStatus.PaymentArranged,
                    _ => BalanceFollowUpStatus.Collected
                },
                FollowUpDateLocal = appointment.AppointmentDateLocal.AddDays(index % 4),
                Notes = index % 4 == 0 ? "Patient asked for payment plan options." : "Standard collection follow-up.",
                CreatedAtUtc = nowUtc.AddDays(-6).AddMinutes(index * 5),
                UpdatedAtUtc = nowUtc.AddDays(-1).AddMinutes(index * 3)
            });
        }

        if (items.Count > 1)
        {
            items[0].Status = BalanceFollowUpStatus.DisputeNeedsManager;
            items[0].Notes = "Patient disputed EOB amount and requested manager callback.";
            items[1].Status = BalanceFollowUpStatus.PaymentArranged;
            items[1].Notes = "Payment plan approved for two monthly installments.";
        }

        return items;
    }

    private static IReadOnlyList<ContactLog> CreateWorkflowContactLogs(
        IReadOnlyList<AppointmentWorkItem> appointments,
        IReadOnlyList<InsuranceWorkItem> insuranceItems,
        IReadOnlyList<BalanceFollowUpWorkItem> balanceItems,
        IReadOnlyList<User> frontDeskUsers,
        User managerUser,
        User adminUser,
        DateTime nowUtc)
    {
        var logs = new List<ContactLog>();

        foreach (var appointment in appointments.Take(24))
        {
            logs.Add(new ContactLog
            {
                Id = Guid.NewGuid(),
                AppointmentWorkItemId = appointment.Id,
                ContactType = appointment.ConfirmationStatus switch
                {
                    AppointmentConfirmationStatus.LeftVoicemail => ContactType.Voicemail,
                    AppointmentConfirmationStatus.TextSent => ContactType.Text,
                    AppointmentConfirmationStatus.EmailSent => ContactType.Email,
                    _ => ContactType.Call
                },
                Outcome = appointment.ConfirmationStatus switch
                {
                    AppointmentConfirmationStatus.Confirmed => ContactOutcome.Reached,
                    AppointmentConfirmationStatus.LeftVoicemail => ContactOutcome.LeftVoicemail,
                    AppointmentConfirmationStatus.NoAnswer => ContactOutcome.NoAnswer,
                    AppointmentConfirmationStatus.Declined => ContactOutcome.Declined,
                    _ => ContactOutcome.Completed
                },
                Notes = $"Appointment outreach note for {appointment.PatientName}.",
                PerformedByUserId = frontDeskUsers[logs.Count % frontDeskUsers.Count].Id,
                PerformedAtUtc = nowUtc.AddHours(-(logs.Count + 1))
            });
        }

        foreach (var insurance in insuranceItems.Take(14))
        {
            logs.Add(new ContactLog
            {
                Id = Guid.NewGuid(),
                InsuranceWorkItemId = insurance.Id,
                ContactType = ContactType.Call,
                Outcome = insurance.VerificationStatus == InsuranceVerificationStatus.Verified ? ContactOutcome.Completed : ContactOutcome.CallbackRequested,
                Notes = insurance.VerificationStatus == InsuranceVerificationStatus.Verified ? "Benefits confirmed with payer representative." : "Waiting for subscriber info or manager review.",
                PerformedByUserId = insurance.VerificationStatus == InsuranceVerificationStatus.Verified ? managerUser.Id : adminUser.Id,
                PerformedAtUtc = nowUtc.AddHours(-(logs.Count + 2))
            });
        }

        foreach (var balance in balanceItems.Take(10))
        {
            logs.Add(new ContactLog
            {
                Id = Guid.NewGuid(),
                BalanceFollowUpWorkItemId = balance.Id,
                ContactType = balance.Status == BalanceFollowUpStatus.PaymentArranged ? ContactType.Call : ContactType.Text,
                Outcome = balance.Status == BalanceFollowUpStatus.Collected ? ContactOutcome.Completed : ContactOutcome.CallbackRequested,
                Notes = $"Balance follow-up for {balance.PatientName}.",
                PerformedByUserId = frontDeskUsers[(logs.Count + 1) % frontDeskUsers.Count].Id,
                PerformedAtUtc = nowUtc.AddHours(-(logs.Count + 3))
            });
        }

        return logs;
    }

    private static IReadOnlyList<TaskItem> CreateTasks(
        DateTime nowUtc,
        IReadOnlyList<AppointmentWorkItem> appointments,
        IReadOnlyList<InsuranceWorkItem> insuranceItems,
        IReadOnlyList<BalanceFollowUpWorkItem> balanceItems,
        IReadOnlyList<User> frontDeskUsers,
        User managerUser,
        User adminUser)
    {
        var templates = new[]
        {
            ("Confirm next-day hygiene appointments", TaskCategory.AppointmentConfirmation, TaskPriority.High),
            ("Verify insurance benefits before crown prep", TaskCategory.InsuranceVerification, TaskPriority.Urgent),
            ("Balance follow-up before afternoon check-in", TaskCategory.BalanceCollection, TaskPriority.Medium),
            ("Recall list outreach", TaskCategory.Recall, TaskPriority.Medium),
            ("No-show reschedule follow-up", TaskCategory.TreatmentFollowUp, TaskPriority.High),
            ("Open treatment estimate review", TaskCategory.General, TaskPriority.Low),
            ("Same-day emergency schedule sweep", TaskCategory.General, TaskPriority.High),
            ("Opening checklist items", TaskCategory.OpeningChecklist, TaskPriority.High),
            ("Closing checklist items", TaskCategory.ClosingChecklist, TaskPriority.High),
            ("Pending pre-op instruction call", TaskCategory.TreatmentFollowUp, TaskPriority.Medium),
            ("Manual insurance review escalation", TaskCategory.InsuranceVerification, TaskPriority.Urgent),
            ("Payment plan follow-up", TaskCategory.BalanceCollection, TaskPriority.Medium),
            ("Tomorrow prep board cleanup", TaskCategory.General, TaskPriority.Medium),
            ("Front desk voicemail callback list", TaskCategory.AppointmentConfirmation, TaskPriority.Medium)
        };

        var tasks = new List<TaskItem>();
        for (var index = 0; index < 42; index++)
        {
            var template = templates[index % templates.Length];
            var appointment = appointments[index % appointments.Count];
            var insurance = insuranceItems[index % insuranceItems.Count];
            var balance = balanceItems[index % balanceItems.Count];
            var assignedUser = frontDeskUsers[index % frontDeskUsers.Count];
            var status = (index % 8) switch
            {
                0 => TaskStatus.New,
                1 => TaskStatus.Assigned,
                2 => TaskStatus.InProgress,
                3 => TaskStatus.Snoozed,
                4 => TaskStatus.Overdue,
                5 => TaskStatus.Completed,
                6 => TaskStatus.Assigned,
                _ => TaskStatus.Cancelled
            };

            var dueAtUtc = nowUtc.AddHours((index % 12) - 5);
            var completedAtUtc = status == TaskStatus.Completed ? dueAtUtc.AddHours(1) : null as DateTime?;

            tasks.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"{template.Item1} #{index + 1}",
                Description = $"Seeded office workflow item tied to {appointment.PatientName}.",
                Category = template.Item2,
                Priority = template.Item3,
                Status = status,
                AssignedUserId = status is TaskStatus.Assigned or TaskStatus.InProgress or TaskStatus.Completed or TaskStatus.Snoozed ? assignedUser.Id : null,
                ClaimedByUserId = status is TaskStatus.InProgress or TaskStatus.Completed ? assignedUser.Id : null,
                CreatedByUserId = managerUser.Id,
                DueAtUtc = dueAtUtc,
                SnoozeUntilUtc = status == TaskStatus.Snoozed ? nowUtc.AddHours(3) : null,
                CompletedAtUtc = completedAtUtc,
                CreatedAtUtc = nowUtc.AddDays(-7).AddHours(index),
                UpdatedAtUtc = completedAtUtc ?? nowUtc.AddMinutes(-(index % 30)),
                PatientReference = appointment.PatientReference,
                Notes = index % 4 == 0 ? "Patient already called once. Review notes before final follow-up." : "Use front desk script and document outcome in comments/contact logs.",
                ReminderRepeatMinutes = template.Item3 == TaskPriority.Urgent ? 15 : 30,
                EscalateAfterMinutes = template.Item3 is TaskPriority.High or TaskPriority.Urgent ? 45 : 60,
                EscalateToUserId = index % 5 == 0 ? adminUser.Id : managerUser.Id,
                EscalatedAtUtc = status == TaskStatus.Overdue && index % 2 == 0 ? nowUtc.AddMinutes(-20) : null,
                AppointmentWorkItemId = index % 2 == 0 ? appointment.Id : null,
                InsuranceWorkItemId = template.Item2 == TaskCategory.InsuranceVerification ? insurance.Id : null,
                BalanceFollowUpWorkItemId = template.Item2 == TaskCategory.BalanceCollection ? balance.Id : null
            });
        }

        return tasks;
    }

    private static IReadOnlyList<TaskHistory> CreateTaskHistory(IReadOnlyList<TaskItem> tasks, IReadOnlyList<User> frontDeskUsers, User managerUser, User adminUser)
    {
        var historyEntries = new List<TaskHistory>();
        foreach (var task in tasks)
        {
            historyEntries.Add(new TaskHistory
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                ActionType = "Seeded",
                OldStatus = null,
                NewStatus = task.Status,
                PerformedByUserId = task.CreatedByUserId,
                PerformedAtUtc = task.CreatedAtUtc,
                Details = $"Seeded office task: {task.Title}"
            });

            if (task.AssignedUserId.HasValue)
            {
                historyEntries.Add(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    ActionType = "Assigned",
                    OldStatus = TaskStatus.New,
                    NewStatus = task.Status is TaskStatus.InProgress ? TaskStatus.Assigned : task.Status,
                    PerformedByUserId = managerUser.Id,
                    PerformedAtUtc = task.CreatedAtUtc.AddMinutes(10),
                    Details = $"Assigned to front desk user {task.AssignedUserId}."
                });
            }

            if (task.Status is TaskStatus.InProgress or TaskStatus.Completed or TaskStatus.Overdue)
            {
                historyEntries.Add(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    ActionType = "Comment",
                    OldStatus = task.Status,
                    NewStatus = task.Status,
                    PerformedByUserId = task.AssignedUserId ?? frontDeskUsers[0].Id,
                    PerformedAtUtc = task.UpdatedAtUtc.AddMinutes(-15),
                    Details = task.Status == TaskStatus.Overdue
                        ? "Called patient, no answer. Left message and flagged for manager review."
                        : "Patient contacted and update logged for the front desk team."
                });
            }

            if (task.EscalatedAtUtc.HasValue)
            {
                historyEntries.Add(new TaskHistory
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = task.Id,
                    ActionType = "Escalated",
                    OldStatus = TaskStatus.Overdue,
                    NewStatus = TaskStatus.Overdue,
                    PerformedByUserId = adminUser.Id,
                    PerformedAtUtc = task.EscalatedAtUtc.Value,
                    Details = "Escalated after follow-up window elapsed."
                });
            }
        }

        return historyEntries;
    }

    private static IReadOnlyList<ContactLog> CreateTaskContactLogs(IReadOnlyList<TaskItem> tasks, IReadOnlyList<User> frontDeskUsers, User managerUser, DateTime nowUtc)
    {
        var logs = new List<ContactLog>();
        foreach (var task in tasks.Where(x => x.Status is TaskStatus.Overdue or TaskStatus.InProgress or TaskStatus.Completed).Take(18))
        {
            logs.Add(new ContactLog
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                ContactType = task.Category == TaskCategory.BalanceCollection ? ContactType.Call : ContactType.Text,
                Outcome = task.Status == TaskStatus.Completed ? ContactOutcome.Completed : ContactOutcome.LeftVoicemail,
                Notes = $"Task-linked outreach for {task.Title}.",
                PerformedByUserId = task.Status == TaskStatus.Overdue ? managerUser.Id : frontDeskUsers[logs.Count % frontDeskUsers.Count].Id,
                PerformedAtUtc = nowUtc.AddHours(-(logs.Count + 1))
            });
        }

        return logs;
    }

    private static IReadOnlyList<ExternalIntegrationProviderConfig> CreateIntegrationConfigs(DateTime nowUtc) =>
    [
        new ExternalIntegrationProviderConfig
        {
            Id = Guid.NewGuid(),
            ProviderType = ExternalIntegrationProviderType.OpenDentalAppointments,
            DisplayName = "Open Dental Appointment Sync",
            IsEnabled = false,
            BaseUrl = "https://open-dental.example.local/api/",
            Notes = "Disabled scaffold for future scheduling sync.",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        },
        new ExternalIntegrationProviderConfig
        {
            Id = Guid.NewGuid(),
            ProviderType = ExternalIntegrationProviderType.PatientXpressInsurance,
            DisplayName = "PatientXpress Insurance Sync",
            IsEnabled = false,
            BaseUrl = "https://patientxpress.example.local/api/",
            Notes = "Disabled scaffold for future insurance verification sync.",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        },
        new ExternalIntegrationProviderConfig
        {
            Id = Guid.NewGuid(),
            ProviderType = ExternalIntegrationProviderType.CsvManualImport,
            DisplayName = "CSV / Manual Import Provider",
            IsEnabled = true,
            Notes = "Example enabled provider for local file-drop imports.",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        },
        new ExternalIntegrationProviderConfig
        {
            Id = Guid.NewGuid(),
            ProviderType = ExternalIntegrationProviderType.PatientCommunication,
            DisplayName = "Patient Communication Provider",
            IsEnabled = false,
            BaseUrl = "https://communications.example.local/api/",
            Notes = "Disabled scaffold for future text/email messaging.",
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        }
    ];

    private static IReadOnlyList<ExternalIntegrationRun> CreateIntegrationRuns(IReadOnlyList<ExternalIntegrationProviderConfig> configs, DateTime nowUtc) =>
        configs.Select(config => new ExternalIntegrationRun
        {
            Id = Guid.NewGuid(),
            ProviderConfigId = config.Id,
            Status = config.ProviderType == ExternalIntegrationProviderType.CsvManualImport ? ExternalIntegrationRunStatus.Success : ExternalIntegrationRunStatus.Disabled,
            StartedAtUtc = nowUtc.AddHours(-12),
            CompletedAtUtc = nowUtc.AddHours(-12).AddMinutes(2),
            Message = config.ProviderType == ExternalIntegrationProviderType.CsvManualImport
                ? "Sample file drop processed successfully."
                : "Provider is scaffolded and currently disabled."
        }).ToList();

    private static IReadOnlyList<AuditEntry> CreateAuditEntries(IReadOnlyList<TaskItem> tasks, IReadOnlyList<AppointmentWorkItem> appointments, User managerUser, User adminUser, DateTime nowUtc)
    {
        var auditEntries = new List<AuditEntry>();

        foreach (var task in tasks)
        {
            auditEntries.Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Task",
                EntityId = task.Id,
                ActionType = "SeededAudit",
                Summary = $"Task {task.Title} reviewed by manager.",
                Details = "Example audit trail entry for front desk workflow review.",
                PerformedByUserId = managerUser.Id,
                PerformedByDisplayName = managerUser.DisplayName,
                PerformedAtUtc = nowUtc.AddHours(-(auditEntries.Count + 1))
            });
        }

        foreach (var appointment in appointments)
        {
            auditEntries.Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "AppointmentWorkItem",
                EntityId = appointment.Id,
                ActionType = "SeededStatusReview",
                Summary = $"Appointment {appointment.PatientReference} reviewed for tomorrow prep.",
                Details = "Example audit entry for appointment workflow oversight.",
                PerformedByUserId = adminUser.Id,
                PerformedByDisplayName = adminUser.DisplayName,
                PerformedAtUtc = nowUtc.AddHours(-(auditEntries.Count + 2))
            });
        }

        return auditEntries;
    }

    private static string BuildAppointmentNotes(int index, AppointmentStatus status)
    {
        var notes = new[]
        {
            index % 3 == 0 ? "Needs pre-op instructions reviewed." : "Standard front desk follow-up.",
            index % 4 == 0 ? "Review balance estimate before check-in." : string.Empty,
            status == AppointmentStatus.NoShow ? "Create follow-up and reschedule outreach." : string.Empty
        };

        return string.Join(" ", notes.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private sealed record SeedUser(string Username, string DisplayName, UserRole Role);
}
