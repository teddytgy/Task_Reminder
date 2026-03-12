using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.Domain.Entities;

namespace Task_Reminder.Api.Data;

public sealed class TaskReminderDbContext(DbContextOptions<TaskReminderDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskHistory> TaskHistory => Set<TaskHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
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
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DueAtUtc);
            entity.HasOne(x => x.AssignedUser).WithMany(x => x.AssignedTasks).HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ClaimedByUser).WithMany(x => x.ClaimedTasks).HasForeignKey(x => x.ClaimedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedTasks).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
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
    }
}
