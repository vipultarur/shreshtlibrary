namespace WebApplication1.Utils
{
    public static class Permissions
    {
        public static class Dashboard
        {
            public const string View = "Dashboard.View";
            public const string Analytics = "Dashboard.Analytics";
            public const string Export = "Dashboard.Export";
        }

        public static class StudentManagement
        {
            public const string View = "StudentManagement.View";
            public const string Add = "StudentManagement.Add";
            public const string Edit = "StudentManagement.Edit";
            public const string Delete = "StudentManagement.Delete";
            public const string Suspend = "StudentManagement.Suspend";
            public const string Activate = "StudentManagement.Activate";
            public const string Import = "StudentManagement.Import";
            public const string Export = "StudentManagement.Export";
            public const string ResetPassword = "StudentManagement.ResetPassword";
        }

        public static class Attendance
        {
            public const string View = "Attendance.View";
            public const string Mark = "Attendance.Mark";
            public const string Edit = "Attendance.Edit";
            public const string Delete = "Attendance.Delete";
            public const string Export = "Attendance.Export";
            public const string Manage = "Attendance.Manage";
        }

        public static class QRAttendance
        {
            public const string View = "QRAttendance.View";
            public const string Generate = "QRAttendance.Generate";
            public const string Delete = "QRAttendance.Delete";
        }

        public static class LibraryManagement
        {
            public const string Settings = "LibraryManagement.Settings";
            public const string Timing = "LibraryManagement.Timing";
            public const string Holiday = "LibraryManagement.Holiday";
            public const string Seat = "LibraryManagement.Seat";
            public const string Floor = "LibraryManagement.Floor";
            public const string Room = "LibraryManagement.Room";
            public const string Capacity = "LibraryManagement.Capacity";
            public const string Info = "LibraryManagement.Info";
            public const string Gallery = "LibraryManagement.Gallery";
            public const string Facilities = "LibraryManagement.Facilities";
            public const string Slider = "LibraryManagement.Slider";
            public const string Review = "LibraryManagement.Review";
            public const string Achiever = "LibraryManagement.Achiever";
        }

        public static class NotificationManagement
        {
            public const string View = "NotificationManagement.View";
            public const string Create = "NotificationManagement.Create";
            public const string Edit = "NotificationManagement.Edit";
            public const string Delete = "NotificationManagement.Delete";
            public const string SendPush = "NotificationManagement.SendPush";
            public const string SendEmail = "NotificationManagement.SendEmail";
            public const string SendSMS = "NotificationManagement.SendSMS";
            public const string Send = "NotificationManagement.Send";
        }

        public static class UserManagement // Wait, is this separate from students? Admin Management? The prompt says User Management. We will assume Admins/Staff. Let's create AdminManagement separately.
        {
            public const string View = "UserManagement.View";
            public const string Add = "UserManagement.Add";
            public const string Edit = "UserManagement.Edit";
            public const string Delete = "UserManagement.Delete";
            public const string Activate = "UserManagement.Activate";
            public const string Suspend = "UserManagement.Suspend";
            public const string ResetPassword = "UserManagement.ResetPassword";
        }
        
        public static class AdminManagement
        {
            public const string View = "AdminManagement.View";
            public const string Create = "AdminManagement.Create";
            public const string Edit = "AdminManagement.Edit";
            public const string Delete = "AdminManagement.Delete";
            public const string Suspend = "AdminManagement.Suspend";
            public const string Activate = "AdminManagement.Activate";
            public const string ResetPassword = "AdminManagement.ResetPassword";
            public const string ChangePermissions = "AdminManagement.ChangePermissions";
            public const string ViewActivity = "AdminManagement.ViewActivity";
            public const string Export = "AdminManagement.Export";
            public const string ManageRoles = "AdminManagement.ManageRoles";
        }

        public static class Reports
        {
            public const string View = "Reports.View";
            public const string Attendance = "Reports.Attendance";
            public const string Student = "Reports.Student";
            public const string Revenue = "Reports.Revenue";
            public const string Export = "Reports.Export";
        }

        public static class Analytics
        {
            public const string View = "Analytics.View";
            public const string Attendance = "Analytics.Attendance";
            public const string Student = "Analytics.Student";
            public const string Revenue = "Analytics.Revenue";
        }

        public static class FeeManagement
        {
            public const string View = "FeeManagement.View";
            public const string Create = "FeeManagement.Create";
            public const string Edit = "FeeManagement.Edit";
            public const string Delete = "FeeManagement.Delete";
            public const string Collect = "FeeManagement.Collect";
            public const string Refund = "FeeManagement.Refund";
            public const string Export = "FeeManagement.Export";
        }

        public static class Payment
        {
            public const string View = "Payment.View";
            public const string Verify = "Payment.Verify";
            public const string Refund = "Payment.Refund";
            public const string Export = "Payment.Export";
        }

        public static class Membership
        {
            public const string View = "Membership.View";
            public const string Create = "Membership.Create";
            public const string Edit = "Membership.Edit";
            public const string Delete = "Membership.Delete";
            public const string Renew = "Membership.Renew";
            public const string ManagePlans = "Membership.ManagePlans";
        }

        public static class CourseManagement
        {
            public const string View = "CourseManagement.View";
            public const string Add = "CourseManagement.Add";
            public const string Edit = "CourseManagement.Edit";
            public const string Delete = "CourseManagement.Delete";
        }

        public static class BatchManagement
        {
            public const string View = "BatchManagement.View";
            public const string Create = "BatchManagement.Create";
            public const string Edit = "BatchManagement.Edit";
            public const string Delete = "BatchManagement.Delete";
        }

        public static class StaffManagement
        {
            public const string View = "StaffManagement.View";
            public const string Add = "StaffManagement.Add";
            public const string Edit = "StaffManagement.Edit";
            public const string Delete = "StaffManagement.Delete";
            public const string Salary = "StaffManagement.Salary";
        }

        public static class VisitorManagement
        {
            public const string View = "VisitorManagement.View";
            public const string Add = "VisitorManagement.Add";
            public const string Edit = "VisitorManagement.Edit";
            public const string Delete = "VisitorManagement.Delete";
        }

        public static class Feedback
        {
            public const string View = "Feedback.View";
            public const string Reply = "Feedback.Reply";
            public const string Delete = "Feedback.Delete";
        }

        public static class Announcement
        {
            public const string View = "Announcement.View";
            public const string Create = "Announcement.Create";
            public const string Edit = "Announcement.Edit";
            public const string Delete = "Announcement.Delete";
        }

        public static class Maintenance
        {
            public const string View = "Maintenance.View";
            public const string Enable = "Maintenance.Enable";
            public const string Disable = "Maintenance.Disable";
            public const string EditMessage = "Maintenance.EditMessage";
            public const string ManageSchedule = "Maintenance.ManageSchedule";
        }

        public static class SystemSettings
        {
            public const string View = "SystemSettings.View";
            public const string Edit = "SystemSettings.Edit";
            public const string Backup = "SystemSettings.Backup";
            public const string Restore = "SystemSettings.Restore";
            public const string Configuration = "SystemSettings.Configuration";
            public const string APIConfiguration = "SystemSettings.APIConfiguration";
            public const string SMTPConfiguration = "SystemSettings.SMTPConfiguration";
            public const string StorageConfiguration = "SystemSettings.StorageConfiguration";
        }

        public static class Security
        {
            public const string ViewLoginHistory = "Security.ViewLoginHistory";
            public const string ViewActivityLogs = "Security.ViewActivityLogs";
            public const string ClearLogs = "Security.ClearLogs";
            public const string ManageSessions = "Security.ManageSessions";
            public const string ForceLogout = "Security.ForceLogout";
            public const string BlockUsers = "Security.BlockUsers";
        }

        public static class Localization
        {
            public const string Manage = "Localization.Manage";
            public const string Add = "Localization.Add";
            public const string Edit = "Localization.Edit";
            public const string Delete = "Localization.Delete";
        }

        public static class Backup
        {
            public const string Create = "Backup.Create";
            public const string Download = "Backup.Download";
            public const string Restore = "Backup.Restore";
            public const string Delete = "Backup.Delete";
        }

        public static class AuditLogs
        {
            public const string View = "AuditLogs.View";
            public const string Export = "AuditLogs.Export";
            public const string Delete = "AuditLogs.Delete";
        }

        public static class AppSettings
        {
            public const string Manage = "AppSettings.Manage";
        }
    }
}
