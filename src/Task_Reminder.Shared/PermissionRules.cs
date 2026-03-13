namespace Task_Reminder.Shared;

public static class PermissionRules
{
    public static bool HasPermission(UserRole role, OfficePermission permission) =>
        permission switch
        {
            OfficePermission.ViewOperationalData => role is UserRole.FrontDesk or UserRole.Manager or UserRole.Admin,
            OfficePermission.ManageTasks => role is UserRole.FrontDesk or UserRole.Manager or UserRole.Admin,
            OfficePermission.ViewManagerDashboard => role is UserRole.Manager or UserRole.Admin,
            OfficePermission.ManageOfficeSettings => role == UserRole.Admin,
            OfficePermission.ManageImports => role == UserRole.Admin,
            OfficePermission.ManageRecurringTasks => role is UserRole.Manager or UserRole.Admin,
            OfficePermission.ViewAudit => role is UserRole.Manager or UserRole.Admin,
            OfficePermission.ManageIntegrations => role == UserRole.Admin,
            _ => false
        };
}
