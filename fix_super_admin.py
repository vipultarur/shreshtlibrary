with open("Services/SuperAdminService.cs", "r", encoding="utf-8") as f:
    content = f.read()

content = content.replace(
    '"LibraryManagement.Capacity" }',
    '"LibraryManagement.Capacity", "LibraryManagement.Info", "LibraryManagement.Gallery", "LibraryManagement.Facilities", "LibraryManagement.Slider", "LibraryManagement.Review", "LibraryManagement.Achiever" }'
)

content = content.replace(
    '"NotificationManagement.SendSMS" }',
    '"NotificationManagement.SendSMS", "NotificationManagement.Send" }'
)

content = content.replace(
    '"Attendance.Export" }',
    '"Attendance.Export", "Attendance.Manage" }'
)

content = content.replace(
    '"Membership.Renew" }',
    '"Membership.Renew", "Membership.ManagePlans" }'
)

if '"AppSettings.Manage"' not in content:
    content = content.replace(
        'new { category = "Audit Logs", permissions = new[] { "AuditLogs.View", "AuditLogs.Export", "AuditLogs.Delete" } }',
        'new { category = "Audit Logs", permissions = new[] { "AuditLogs.View", "AuditLogs.Export", "AuditLogs.Delete" } },\n                new { category = "App Settings", permissions = new[] { "AppSettings.Manage" } }'
    )

with open("Services/SuperAdminService.cs", "w", encoding="utf-8") as f:
    f.write(content)
