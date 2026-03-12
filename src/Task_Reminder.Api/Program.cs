using Microsoft.EntityFrameworkCore;
using Task_Reminder.Api.BackgroundServices;
using Task_Reminder.Api.Data;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Hubs;
using Task_Reminder.Api.Infrastructure.Seed;
using Task_Reminder.Api.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddDbContext<TaskReminderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TaskReminder")));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<TaskBroadcastService>();
builder.Services.AddHostedService<OverdueTaskMonitorService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopClient", policy =>
    {
        policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("DesktopClient");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TaskUpdatesHub>(TaskUpdatesHub.HubPath);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskReminderDbContext>();
    await DemoDataSeeder.SeedAsync(dbContext, CancellationToken.None);
}

app.Run();

public partial class Program;
