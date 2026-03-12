using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Task_Reminder.Api.Data;

#nullable disable

namespace Task_Reminder.Api.Migrations;

[DbContext(typeof(TaskReminderDbContext))]
public partial class TaskReminderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.14")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Task_Reminder.Api.Domain.Entities.User", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Username").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.HasKey("Id");
            b.HasIndex("Username").IsUnique();
            b.ToTable("users");
        });

        modelBuilder.Entity("Task_Reminder.Api.Domain.Entities.TaskItem", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<Guid?>("AssignedUserId").HasColumnType("uuid");
            b.Property<string>("Category").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<Guid?>("ClaimedByUserId").HasColumnType("uuid");
            b.Property<DateTime?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid");
            b.Property<string>("Description").HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.Property<DateTime?>("DueAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Notes").HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.Property<string>("PatientReference").HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Priority").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<int>("ReminderRepeatMinutes").ValueGeneratedOnAdd().HasColumnType("integer").HasDefaultValue(30);
            b.Property<DateTime?>("SnoozeUntilUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("AssignedUserId");
            b.HasIndex("ClaimedByUserId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("DueAtUtc");
            b.HasIndex("Status");
            b.ToTable("task_items");
        });

        modelBuilder.Entity("Task_Reminder.Api.Domain.Entities.TaskHistory", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
            b.Property<string>("ActionType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Details").HasMaxLength(2000).HasColumnType("character varying(2000)");
            b.Property<string>("NewStatus").HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("OldStatus").HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<DateTime>("PerformedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid?>("PerformedByUserId").HasColumnType("uuid");
            b.Property<Guid>("TaskItemId").HasColumnType("uuid");
            b.HasKey("Id");
            b.HasIndex("PerformedByUserId");
            b.HasIndex("TaskItemId");
            b.ToTable("task_history");
        });
#pragma warning restore 612, 618
    }
}
