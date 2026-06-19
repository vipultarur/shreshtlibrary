from django.urls import path

from api.v1 import v2_admin as v2
from api.v1.admin.views import students as admin_students_views
from api.v1.admin.views import memberships as admin_memberships_views
from api.v1.admin.views import payments as admin_payments_views
from api.v1.admin.views import dashboard as admin_dashboard_views
from api.v1.admin.views import attendance as admin_attendance_views
from api.v1.attendance.views import StudentAttendanceLogsView
from api.v1.authentication.views import (
    ForgotPasswordView,
    LogoutView,
    RevokingTokenRefreshView,
    ResetPasswordView,
    SendOTPView,
    StudentLoginMobileView,
    StudentLoginView,
    StudentRegisterView,
    VerifyOTPView,
)
from api.v1.library.views import StudentSubmitReviewView
from api.v1.memberships.views import MembershipPlansListView, StudentMembershipHistoryView
from api.v1.notifications.views import NotificationReadView, RegisterDeviceTokenView, StudentNotificationsListView
from api.v1.payments.views import StudentInitiatePaymentView, StudentPaymentHistoryView
from api.v1.seats.views import SeatLayoutView, StudentSeatHistoryView
from api.v1.students.views import (
    ReferralApplyView,
    ReferralCodeView,
    ReferralHistoryView,
    StudentDashboardView,
    StudentIDCardView,
    StudentProfilePhotoView,
    StudentProfileUpdateView,
    StudentProfileView,
)
from api.v1.study.views import EndStudySessionView, StartStudySessionView, UpdateStudySessionView, CurrentStudySessionView, StudySessionHistoryView, LeaderboardView


urlpatterns = [
    # Authentication
    path('auth/register/', StudentRegisterView.as_view(), name='auth-register'),
    path('auth/send-otp/', SendOTPView.as_view(), name='auth-send-otp'),
    path('auth/verify-otp/', VerifyOTPView.as_view(), name='auth-verify-otp'),
    path('auth/login/email/', StudentLoginView.as_view(), name='auth-login-email'),
    path('auth/login/mobile/', StudentLoginMobileView.as_view(), name='auth-login-mobile'),
    path('auth/login/admin/', v2.AdminLoginView.as_view(), name='auth-login-admin-v2'),
    path('auth/admin/login/', v2.AdminLoginView.as_view(), name='auth-admin-login-legacy'),
    path('auth/me/', v2.AdminMeView.as_view(), name='auth-me'),
    path('admin/profile/', v2.AdminProfileView.as_view(), name='admin-profile'),
    path('auth/change-password/', v2.ChangePasswordAliasView.as_view(), name='auth-change-password-v2'),
    path('auth/admin/change-password/', v2.ChangePasswordAliasView.as_view(), name='auth-admin-change-password-legacy'),
    path('auth/fcm-token/update/', v2.FCMTokenUpdateView.as_view(), name='auth-fcm-token-update'),
    path('auth/forgot-password/', ForgotPasswordView.as_view(), name='auth-forgot-password'),
    path('auth/reset-password/', ResetPasswordView.as_view(), name='auth-reset-password'),
    path('auth/logout/', LogoutView.as_view(), name='auth-logout'),
    path('auth/token/refresh/', RevokingTokenRefreshView.as_view(), name='auth-token-refresh'),

    # Student Profile & Dashboard
    path('student/profile/', StudentProfileView.as_view(), name='student-profile'),
    path('student/profile/update/', StudentProfileUpdateView.as_view(), name='student-profile-update'),
    path('student/profile/photo/', StudentProfilePhotoView.as_view(), name='student-profile-photo'),
    path('student/dashboard/', StudentDashboardView.as_view(), name='student-dashboard'),
    path('student/id-card/', StudentIDCardView.as_view(), name='student-id-card'),
    path('student/referral/', ReferralCodeView.as_view(), name='student-referral'),
    path('student/referral/apply/', ReferralApplyView.as_view(), name='student-referral-apply'),
    path('student/referral/history/', ReferralHistoryView.as_view(), name='student-referral-history'),

    # Student Attendance, QR, Memberships, Payments, Seats
    path('qr/today/', v2.StudentQRTodayView.as_view(), name='qr-today'),
    path('qr/scan/', v2.StudentQRScanView.as_view(), name='qr-scan-v2'),
    path('attendance/scan/', v2.StudentQRScanView.as_view(), name='attendance-scan'),
    path('attendance/logs/', StudentAttendanceLogsView.as_view(), name='attendance-logs-legacy'),
    path('holidays/', v2.HolidayView.as_view(), name='holidays'),
    path('memberships/plans/', MembershipPlansListView.as_view(), name='memberships-plans'),
    path('memberships/history/', StudentMembershipHistoryView.as_view(), name='memberships-history'),
    path('payments/history/', StudentPaymentHistoryView.as_view(), name='payments-history'),
    path('payments/initiate/', StudentInitiatePaymentView.as_view(), name='payments-initiate'),
    path('seats/layout/', SeatLayoutView.as_view(), name='seats-layout'),
    path('seats/history/', StudentSeatHistoryView.as_view(), name='seats-history'),

    # Student Notifications
    path('notifications/list/', StudentNotificationsListView.as_view(), name='notifications-list'),
    path('notifications/read/<int:pk>/', NotificationReadView.as_view(), name='notifications-read'),
    path('notifications/register-device/', RegisterDeviceTokenView.as_view(), name='notifications-register-device'),

    # Public Library Content
    path('library/info/', v2.LibraryInfoAdminView.as_view(), name='library-info'),
    path('library/facilities/', v2.PublicFacilitiesView.as_view(), name='library-facilities'),
    path('library/achievers/', v2.AchieversPublicView.as_view(), name='library-achievers'),
    path('library/achievers/featured/', v2.AchieversPublicView.as_view(), {'featured': True}, name='library-achievers-featured'),
    path('library/reviews/', v2.PublicReviewsView.as_view(), name='library-reviews'),
    path('library/reviews/summary/', v2.ReviewsSummaryView.as_view(), name='library-reviews-summary'),
    path('library/reviews/submit/', StudentSubmitReviewView.as_view(), name='library-reviews-submit'),
    path('sliders/', v2.PublicSlidersView.as_view(), name='sliders-public'),


    # Study Features
    path('study/session/start/', StartStudySessionView.as_view(), name='study-session-start'),
    path('study/session/end/', EndStudySessionView.as_view(), name='study-session-end'),
    path('study/session/current/', CurrentStudySessionView.as_view(), name='study-session-current'),
    path('study/session/update/', UpdateStudySessionView.as_view(), name='study-session-update'),
    path('study/session/history/', StudySessionHistoryView.as_view(), name='study-session-history'),
    path('study/leaderboard/', LeaderboardView.as_view(), name='study-leaderboard'),

    # Dashboard Analytics
    path('dashboard/stats/', admin_dashboard_views.DashboardStatsView.as_view(), {'section': 'overview'}, name='dashboard-stats'),
    path('dashboard/stats/<path:section>/', admin_dashboard_views.DashboardStatsView.as_view(), name='dashboard-stats-section'),
    path('admin/dashboard/stats/', admin_dashboard_views.DashboardStatsView.as_view(), {'section': 'overview'}, name='admin-dashboard-stats-legacy'),
    path('dashboard/charts/<str:domain>/<str:chart>/', admin_dashboard_views.DashboardChartView.as_view(), name='dashboard-chart'),
    path('dashboard/activity/recent/', v2.DashboardActivityView.as_view(), name='dashboard-activity-recent'),
    path('dashboard/activity/log/', v2.DashboardActivityView.as_view(), name='dashboard-activity-log'),
    path('dashboard/activity/export/', v2.DashboardActivityView.as_view(), {'export': True}, name='dashboard-activity-export'),
    path('dashboard/alerts/', v2.DashboardAlertsView.as_view(), name='dashboard-alerts'),
    path('admin/search/', admin_dashboard_views.GlobalSearchView.as_view(), name='admin-global-search'),

    # Admin Students
    path('admin/students/counts/', admin_students_views.AdminStudentCountsView.as_view(), name='admin-students-counts'),
    path('admin/students/export/', admin_students_views.AdminStudentExportView.as_view(), name='admin-students-export'),
    path('admin/students/', admin_students_views.AdminStudentsView.as_view(), name='admin-students'),
    path('admin/students/<str:pk>/', admin_students_views.AdminStudentDetailView.as_view(), name='admin-student-detail'),
    path('admin/students/<str:pk>/photo/', admin_students_views.AdminStudentPhotoView.as_view(), name='admin-student-photo'),
    path('admin/students/<str:pk>/analytics/', admin_students_views.AdminStudentAnalyticsView.as_view(), name='admin-student-analytics'),
    path('admin/students/<str:pk>/suspend/', admin_students_views.AdminStudentStatusView.as_view(), {'action': 'suspend'}, name='admin-student-suspend'),
    path('admin/students/<str:pk>/activate/', admin_students_views.AdminStudentStatusView.as_view(), {'action': 'activate'}, name='admin-student-activate'),
    path('admin/students/<str:pk>/<str:kind>/', admin_students_views.AdminStudentRelatedView.as_view(), name='admin-student-related'),

    # Plans and Memberships
    path('plans/', admin_memberships_views.PlansView.as_view(), name='plans'),
    path('plans/<int:pk>/', admin_memberships_views.PlanDetailView.as_view(), name='plan-detail-public'),
    path('admin/plans/stats/', admin_memberships_views.PlanStatsView.as_view(), name='admin-plans-stats'),
    path('admin/plans/', admin_memberships_views.PlansAllView.as_view(), name='admin-plans'),
    path('admin/plans/create/', admin_memberships_views.PlansView.as_view(), name='admin-plans-create'),
    path('admin/plans/<int:pk>/', admin_memberships_views.PlanDetailView.as_view(), name='admin-plan-detail'),
    path('admin/plans/<int:pk>/update/', admin_memberships_views.PlanDetailView.as_view(), name='admin-plan-update-legacy'),
    path('admin/plans/<int:pk>/toggle/', admin_memberships_views.PlanToggleView.as_view(), name='admin-plan-toggle'),
    path('admin/plans/<int:pk>/students/', admin_memberships_views.PlanStudentsView.as_view(), name='admin-plan-students'),
    path('admin/memberships/expiring/', admin_memberships_views.AdminMembershipSpecialView.as_view(), {'kind': 'expiring'}, name='admin-memberships-expiring'),
    path('admin/memberships/expired-today/', admin_memberships_views.AdminMembershipSpecialView.as_view(), {'kind': 'expired-today'}, name='admin-memberships-expired-today'),
    path('admin/memberships/assign/', admin_memberships_views.AdminMembershipActionView.as_view(), {'action': 'assign'}, name='admin-memberships-assign'),
    path('admin/memberships/renew/', admin_memberships_views.AdminMembershipActionView.as_view(), {'action': 'renew'}, name='admin-memberships-renew'),
    path('admin/memberships/upgrade/', admin_memberships_views.AdminMembershipActionView.as_view(), {'action': 'upgrade'}, name='admin-memberships-upgrade'),
    path('admin/memberships/', admin_memberships_views.AdminMembershipsView.as_view(), name='admin-memberships'),
    path('admin/memberships/<int:pk>/', admin_memberships_views.AdminMembershipDetailView.as_view(), name='admin-membership-detail'),

    # QR and Attendance
    path('admin/qr/current/', admin_attendance_views.AdminQRView.as_view(), name='admin-qr-current'),
    path('admin/qr/history/', admin_attendance_views.AdminQRView.as_view(), {'action': 'history'}, name='admin-qr-history'),
    path('admin/qr/generate/', admin_attendance_views.AdminQRView.as_view(), {'action': 'generate'}, name='admin-qr-generate'),
    path('admin/qr/regenerate/', admin_attendance_views.AdminQRView.as_view(), {'action': 'regenerate'}, name='admin-qr-regenerate'),
    path('admin/qr/expire/', admin_attendance_views.AdminQRView.as_view(), {'action': 'expire'}, name='admin-qr-expire'),
    path('admin/qr/<int:pk>/scans/', admin_attendance_views.AdminQRView.as_view(), {'action': 'scans'}, name='admin-qr-scans'),
    path('admin/qrcode/generate/', admin_attendance_views.AdminQRView.as_view(), {'action': 'generate'}, name='admin-qrcode-generate-legacy'),
    path('admin/holidays/', v2.HolidayView.as_view(), name='admin-holidays'),
    path('admin/holidays/<int:pk>/', v2.HolidayView.as_view(), name='admin-holiday-detail'),
    path('admin/attendance/daily-summary/', admin_attendance_views.AdminAttendanceSummaryView.as_view(), {'kind': 'daily-summary'}, name='admin-attendance-daily-summary'),
    path('admin/attendance/absentees/', admin_attendance_views.AdminAttendanceSummaryView.as_view(), {'kind': 'absentees'}, name='admin-attendance-absentees'),
    path('admin/attendance/streak/', admin_attendance_views.AdminAttendanceSummaryView.as_view(), {'kind': 'streak'}, name='admin-attendance-streak'),
    path('admin/attendance/manual/', admin_attendance_views.AdminAttendanceView.as_view(), name='admin-attendance-manual'),
    path('admin/attendance/', admin_attendance_views.AdminAttendanceView.as_view(), name='admin-attendance'),
    path('admin/attendance/<int:pk>/', admin_attendance_views.AdminAttendanceDetailView.as_view(), name='admin-attendance-detail'),

    # Payments
    path('admin/payments/summary/', admin_payments_views.AdminPaymentSpecialView.as_view(), {'kind': 'summary'}, name='admin-payments-summary'),
    path('admin/payments/pending/', admin_payments_views.AdminPaymentSpecialView.as_view(), {'kind': 'pending'}, name='admin-payments-pending'),
    path('admin/payments/overdue/', admin_payments_views.AdminPaymentSpecialView.as_view(), {'kind': 'overdue'}, name='admin-payments-overdue'),
    path('admin/payments/', admin_payments_views.AdminPaymentsView.as_view(), name='admin-payments'),
    path('admin/payments/<int:pk>/', admin_payments_views.AdminPaymentDetailView.as_view(), name='admin-payment-detail'),
    path('admin/payments/<int:pk>/verify/', admin_payments_views.AdminPaymentActionView.as_view(), {'action': 'verify'}, name='admin-payment-verify'),
    path('admin/payments/<int:pk>/refund/', admin_payments_views.AdminPaymentActionView.as_view(), {'action': 'refund'}, name='admin-payment-refund'),
    path('admin/payments/<int:pk>/receipt/', admin_payments_views.AdminPaymentReceiptView.as_view(), name='admin-payment-receipt'),

    # Seats
    path('admin/seats/layout/', v2.AdminSeatsLayoutView.as_view(), name='admin-seats-layout'),
    path('admin/seats/release-all/', v2.AdminSeatsReleaseAllView.as_view(), name='admin-seats-release-all'),
    path('admin/seats/reserve-bulk/', v2.AdminSeatsReserveBulkView.as_view(), name='admin-seats-reserve-bulk'),
    path('admin/seats/available/', v2.SeatSpecialView.as_view(), {'kind': 'available'}, name='admin-seats-available'),
    path('admin/seats/stats/', v2.SeatSpecialView.as_view(), {'kind': 'stats'}, name='admin-seats-stats'),
    path('admin/seats/add/', v2.AdminSeatsView.as_view(), name='admin-seats-add-legacy'),
    path('admin/seats/', v2.AdminSeatsView.as_view(), name='admin-seats'),
    path('admin/seats/<int:pk>/', v2.AdminSeatDetailView.as_view(), name='admin-seat-detail'),
    path('admin/seats/<int:pk>/status/', v2.SeatActionView.as_view(), {'action': 'status'}, name='admin-seat-status'),
    path('admin/seats/<int:pk>/assign/', v2.SeatActionView.as_view(), {'action': 'assign'}, name='admin-seat-assign'),
    path('admin/seats/<int:pk>/unassign/', v2.SeatActionView.as_view(), {'action': 'unassign'}, name='admin-seat-unassign'),
    path('admin/seats/<int:pk>/history/', v2.SeatSpecialView.as_view(), {'kind': 'history'}, name='admin-seat-history'),
    path('admin/floors/', v2.FloorView.as_view(), name='admin-floors'),
    path('admin/floors/<int:pk>/', v2.FloorView.as_view(), name='admin-floor-detail'),
    path('admin/rows/', v2.RowView.as_view(), name='admin-rows'),
    path('admin/rows/<int:pk>/', v2.RowView.as_view(), name='admin-row-detail'),

    # Admin Inbox
    path('admin/inbox/', admin_dashboard_views.AdminInboxView.as_view(), name='admin-inbox'),
    path('admin/inbox/<int:pk>/<str:action>/', admin_dashboard_views.AdminInboxNotificationDetailView.as_view(), name='admin-inbox-action'),
    path('admin/inbox/<int:pk>/', admin_dashboard_views.AdminInboxNotificationDetailView.as_view(), name='admin-inbox-detail'),

    # Notifications
    path('admin/notifications/templates/', v2.AdminNotificationTemplatesView.as_view(), name='admin-notification-templates'),
    path('admin/notifications/scheduled/', v2.AdminNotificationScheduledView.as_view(), name='admin-notification-scheduled'),
    path('admin/notifications/scheduled/<int:pk>/cancel/', v2.AdminNotificationScheduledView.as_view(), name='admin-notification-scheduled-cancel'),
    path('admin/notifications/schedule/', v2.AdminNotificationScheduleView.as_view(), name='admin-notification-schedule'),
    path('admin/notifications/send/', v2.AdminNotificationSendView.as_view(), name='admin-notification-send'),
    path('admin/notifications/', v2.AdminNotificationsView.as_view(), name='admin-notifications'),
    path('admin/notifications/<int:pk>/', v2.AdminNotificationDetailView.as_view(), name='admin-notification-detail'),
    path('admin/notifications/<int:pk>/recipients/', v2.AdminNotificationRecipientsView.as_view(), name='admin-notification-recipients'),

    # Library Admin
    path('admin/library/info/', v2.LibraryInfoAdminView.as_view(), name='admin-library-info'),
    path('admin/library/facilities/reorder/', v2.AdminFacilityToggleView.as_view(), name='admin-library-facilities-reorder'),
    path('admin/library/facilities/', v2.AdminFacilitiesView.as_view(), name='admin-library-facilities'),
    path('admin/library/facilities/<int:pk>/', v2.AdminFacilityDetailView.as_view(), name='admin-library-facility-detail'),
    path('admin/library/facilities/<int:pk>/toggle/', v2.AdminFacilityToggleView.as_view(), name='admin-library-facility-toggle'),
    path('admin/library/achievers/reorder/', v2.AdminAchieverToggleView.as_view(), name='admin-library-achievers-reorder'),
    path('admin/library/achievers/', v2.AdminAchieversView.as_view(), name='admin-library-achievers'),
    path('admin/library/achievers/<int:pk>/', v2.AdminAchieverDetailView.as_view(), name='admin-library-achiever-detail'),
    path('admin/library/achievers/<int:pk>/toggle/', v2.AdminAchieverToggleView.as_view(), name='admin-library-achiever-toggle'),
    path('admin/reviews/pending/', v2.AdminReviewsView.as_view(), {'pending': True}, name='admin-reviews-pending'),
    path('admin/reviews/', v2.AdminReviewsView.as_view(), name='admin-reviews'),
    path('admin/reviews/<int:pk>/approve/', v2.AdminReviewActionView.as_view(), {'action': 'approve'}, name='admin-review-approve'),
    path('admin/reviews/<int:pk>/reject/', v2.AdminReviewActionView.as_view(), {'action': 'reject'}, name='admin-review-reject'),
    path('admin/reviews/<int:pk>/delete/', v2.AdminReviewActionView.as_view(), {'action': 'delete'}, name='admin-review-delete'),
    path('admin/sliders/', v2.AdminSlidersView.as_view(), name='admin-sliders'),
    path('admin/sliders/<int:pk>/', v2.AdminSliderDetailView.as_view(), name='admin-slider-detail'),



    # Reports and Exports
    path('reports/attendance/', v2.ReportsView.as_view(), {'kind': 'attendance'}, name='reports-attendance'),
    path('reports/payments/', v2.ReportsView.as_view(), {'kind': 'payments'}, name='reports-payments'),
    path('reports/students/', v2.ReportsView.as_view(), {'kind': 'students'}, name='reports-students'),
    path('reports/memberships/', v2.ReportsView.as_view(), {'kind': 'memberships'}, name='reports-memberships'),
    path('reports/daily-summary/', v2.ReportsView.as_view(), {'kind': 'daily-summary'}, name='reports-daily-summary'),
    path('reports/<str:kind>/export/', v2.ReportsView.as_view(), {'export': True}, name='reports-export'),
    path('reports/seats/', v2.SeatSpecialView.as_view(), {'kind': 'stats'}, name='reports-seats'),

    # Super Admin
    path('superadmin/admins/add/', v2.SuperAdminAdminsView.as_view(), name='superadmin-admins-add-legacy'),
    path('superadmin/admins/', v2.SuperAdminAdminsView.as_view(), name='superadmin-admins'),
    path('superadmin/admins/<int:pk>/', v2.SuperAdminAdminDetailView.as_view(), name='superadmin-admin-detail'),
    path('superadmin/admins/<int:pk>/remove/', v2.SuperAdminAdminDetailView.as_view(), name='superadmin-admin-remove-legacy'),
    path('superadmin/admins/<int:pk>/deactivate/', v2.SuperAdminDeactivateView.as_view(), name='superadmin-admin-deactivate'),
    path('superadmin/permissions/', v2.SuperAdminToolsView.as_view(), {'kind': 'permissions'}, name='superadmin-permissions'),
    path('superadmin/permissions/assign/', v2.SuperAdminToolsView.as_view(), {'kind': 'permissions/assign'}, name='superadmin-permissions-assign'),
    path('superadmin/backup/create/', v2.SuperAdminToolsView.as_view(), {'kind': 'backup/create'}, name='superadmin-backup-create'),
    path('superadmin/backup/list/', v2.SuperAdminToolsView.as_view(), {'kind': 'backup/list'}, name='superadmin-backup-list'),
    path('superadmin/backup/restore/', v2.SuperAdminToolsView.as_view(), {'kind': 'backup/restore'}, name='superadmin-backup-restore'),
    path('superadmin/activity-log/', v2.SuperAdminToolsView.as_view(), {'kind': 'activity-log'}, name='superadmin-activity-log'),
    path('superadmin/health/', v2.SuperAdminToolsView.as_view(), {'kind': 'health'}, name='superadmin-health'),
    path('admin/settings/', v2.AdminSettingsView.as_view(), name='admin-settings'),
]
