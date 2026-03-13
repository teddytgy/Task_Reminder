using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Domain.Entities;

namespace Task_Reminder.Api.Data;

public sealed class TaskReminderDbContext(DbContextOptions<TaskReminderDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskHistory> TaskHistory => Set<TaskHistory>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<RecurringTaskDefinition> RecurringTaskDefinitions => Set<RecurringTaskDefinition>();
    public DbSet<AppointmentWorkItem> AppointmentWorkItems => Set<AppointmentWorkItem>();
    public DbSet<InsuranceWorkItem> InsuranceWorkItems => Set<InsuranceWorkItem>();
    public DbSet<BalanceFollowUpWorkItem> BalanceFollowUpWorkItems => Set<BalanceFollowUpWorkItem>();
    public DbSet<ContactLog> ContactLogs => Set<ContactLog>();
    public DbSet<OfficeSettings> OfficeSettings => Set<OfficeSettings>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ExternalIntegrationProviderConfig> ExternalIntegrationProviderConfigs => Set<ExternalIntegrationProviderConfig>();
    public DbSet<ExternalIntegrationRun> ExternalIntegrationRuns => Set<ExternalIntegrationRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasOne(x => x.NotificationPreference)
                .WithOne(x => x.User)
                .HasForeignKey<UserNotificationPreference>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("task_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.PatientReference).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ReminderRepeatMinutes).HasDefaultValue(30);
            entity.Property(x => x.GeneratedForDateLocal);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DueAtUtc);
            entity.HasIndex(x => new { x.GeneratedFromRecurringTaskDefinitionId, x.GeneratedForDateLocal }).IsUnique();
            entity.HasOne(x => x.AssignedUser).WithMany(x => x.AssignedTasks).HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ClaimedByUser).WithMany(x => x.ClaimedTasks).HasForeignKey(x => x.ClaimedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedTasks).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EscalateToUser).WithMany(x => x.EscalatedTasks).HasForeignKey(x => x.EscalateToUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GeneratedFromRecurringTaskDefinition).WithMany(x => x.GeneratedTasks).HasForeignKey(x => x.GeneratedFromRecurringTaskDefinitionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AppointmentWorkItem).WithMany(x => x.Tasks).HasForeignKey(x => x.AppointmentWorkItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.InsuranceWorkItem).WithMany(x => x.Tasks).HasForeignKey(x => x.InsuranceWorkItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.BalanceFollowUpWorkItem).WithMany(x => x.Tasks).HasForeignKey(x => x.BalanceFollowUpWorkItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.ToTable("task_history");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(2000);
            entity.Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.TaskItemId);
            entity.HasOne(x => x.TaskItem).WithMany(x => x.HistoryEntries).HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PerformedByUser).WithMany(x => x.HistoryEntries).HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("user_notification_preferences");
            entity.HasKey(x => x.UserId);
        });

        modelBuilder.Entity<AppointmentWorkItem>(entity =>
        {
            entity.ToTable("appointment_work_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PatientReference).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProviderName).HasMaxLength(100);
            entity.Property(x => x.AppointmentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.SourceSystem).HasMaxLength(100);
            entity.Property(x => x.SourceReference).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ConfirmationStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.InsuranceStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.BalanceStatus).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.AppointmentDateLocal);
            entity.HasIndex(x => new { x.SourceSystem, x.SourceReference }).IsUnique();
        });

        modelBuilder.Entity<InsuranceWorkItem>(entity =>
        {
            entity.ToTable("insurance_work_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PatientReference).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CarrierName).HasMaxLength(100);
            entity.Property(x => x.PlanName).HasMaxLength(100);
            entity.Property(x => x.MemberId).HasMaxLength(100);
            entity.Property(x => x.GroupNumber).HasMaxLength(100);
            entity.Property(x => x.PayerId).HasMaxLength(100);
            entity.Property(x => x.FrequencyNotes).HasMaxLength(500);
            entity.Property(x => x.WaitingPeriodNotes).HasMaxLength(500);
            entity.Property(x => x.MissingInfoNotes).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.SourceSystem).HasMaxLength(100);
            entity.Property(x => x.SourceReference).HasMaxLength(100);
            entity.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.EligibilityStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.VerificationMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.IssueType).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.AppointmentDateLocal);
            entity.HasIndex(x => new { x.SourceSystem, x.SourceReference }).IsUnique();
            entity.HasOne(x => x.VerifiedByUser).WithMany(x => x.VerifiedInsuranceItems).HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AppointmentWorkItem).WithMany(x => x.InsuranceWorkItems).HasForeignKey(x => x.AppointmentWorkItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BalanceFollowUpWorkItem>(entity =>
        {
            entity.ToTable("balance_follow_up_work_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PatientReference).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DueReasonNote).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.FollowUpDateLocal);
            entity.HasOne(x => x.AppointmentWorkItem).WithMany(x => x.BalanceFollowUpWorkItems).HasForeignKey(x => x.AppointmentWorkItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ContactLog>(entity =>
        {
            entity.ToTable("contact_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContactType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.PerformedAtUtc);
            entity.HasOne(x => x.TaskItem).WithMany(x => x.ContactLogs).HasForeignKey(x => x.TaskItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AppointmentWorkItem).WithMany(x => x.ContactLogs).HasForeignKey(x => x.AppointmentWorkItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InsuranceWorkItem).WithMany(x => x.ContactLogs).HasForeignKey(x => x.InsuranceWorkItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.BalanceFollowUpWorkItem).WithMany(x => x.ContactLogs).HasForeignKey(x => x.BalanceFollowUpWorkItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PerformedByUser).WithMany(x => x.ContactLogs).HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OfficeSettings>(entity =>
        {
            entity.ToTable("office_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OfficeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BusinessHoursSummary).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.ManagerEscalationUser).WithMany(x => x.EscalationSettings).HasForeignKey(x => x.ManagerEscalationUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.Property(x => x.PerformedByDisplayName).HasMaxLength(100);
            entity.HasIndex(x => x.PerformedAtUtc);
            entity.HasIndex(x => new { x.EntityType, x.PerformedAtUtc });
            entity.HasOne(x => x.PerformedByUser)
                .WithMany(x => x.AuditEntries)
                .HasForeignKey(x => x.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExternalIntegrationProviderConfig>(entity =>
        {
            entity.ToTable("external_integration_provider_configs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(100);
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BaseUrl).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.ProviderType).IsUnique();
        });

        modelBuilder.Entity<ExternalIntegrationRun>(entity =>
        {
            entity.ToTable("external_integration_runs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.HasIndex(x => new { x.ProviderConfigId, x.StartedAtUtc });
            entity.HasOne(x => x.ProviderConfig)
                .WithMany(x => x.Runs)
                .HasForeignKey(x => x.ProviderConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringTaskDefinition>(entity =>
        {
            entity.ToTable("recurring_task_definitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.PatientReference).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.RecurrenceType).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.DaysOfWeek).HasMaxLength(100);
            entity.Property(x => x.ReminderRepeatMinutes).HasDefaultValue(30);
            entity.HasIndex(x => x.IsActive);
            entity.HasOne(x => x.AssignedUser).WithMany(x => x.AssignedRecurringTasks).HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedRecurringTasks).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EscalateToUser).WithMany(x => x.EscalatedRecurringTasks).HasForeignKey(x => x.EscalateToUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
