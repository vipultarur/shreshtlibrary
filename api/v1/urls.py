from django.urls import path
from api.v1.authentication.views import (
    StudentRegisterView, SendOTPView, VerifyOTPView, StudentLoginView, StudentLoginMobileView,
    ForgotPasswordView, ResetPasswordView, LogoutView, AdminLoginView, AdminChangePasswordView
)
from api.v1.students.views import (
    StudentProfileView, StudentProfileUpdateView, StudentProfilePhotoView,
    StudentDashboardView, StudentIDCardView, ReferralCodeView, ReferralApplyView, ReferralHistoryView
)
from api.v1.attendance.views import StudentScanQRView, StudentAttendanceLogsView
from api.v1.memberships.views import MembershipPlansListView, StudentMembershipHistoryView
from api.v1.payments.views import StudentPaymentHistoryView, StudentInitiatePaymentView
from api.v1.seats.views import SeatLayoutView, StudentSeatHistoryView
from api.v1.notifications.views import StudentNotificationsListView, NotificationReadView, RegisterDeviceTokenView
from api.v1.library.views import LibraryInfoView, AchieversListView, ReviewsListView, StudentSubmitReviewView
from api.v1.study.views import StartStudySessionView, EndStudySessionView, StudyGoalView
from api.v1.admin.views import (
    AdminDashboardStatsView, AdminStudentsListView, AdminStudentProfileView, AdminVerifyPaymentView,
    AdminSeatStatusUpdateView, AdminAddSeatView, AdminQRCodeGenerateView, AdminManualAttendanceView,
    AdminUpdateMembershipPlanView, AdminReviewsListView, AdminApproveReviewView, AdminSendNotificationView
)
from api.v1.reports.views import AdminAttendanceReportView, AdminPaymentReportView, AdminSeatOccupancyReportView
from api.v1.superadmin.views import SuperAdminAddAdminView, SuperAdminRemoveAdminView, SuperAdminListAdminsView

urlpatterns = [
    # Authentication
    path('auth/register/', StudentRegisterView.as_view(), name='auth-register'),
    path('auth/send-otp/', SendOTPView.as_view(), name='auth-send-otp'),
    path('auth/verify-otp/', VerifyOTPView.as_view(), name='auth-verify-otp'),
    path('auth/login/email/', StudentLoginView.as_view(), name='auth-login-email'),
    path('auth/login/mobile/', StudentLoginMobileView.as_view(), name='auth-login-mobile'),
    path('auth/forgot-password/', ForgotPasswordView.as_view(), name='auth-forgot-password'),
    path('auth/reset-password/', ResetPasswordView.as_view(), name='auth-reset-password'),
    path('auth/logout/', LogoutView.as_view(), name='auth-logout'),
    path('auth/admin/login/', AdminLoginView.as_view(), name='auth-admin-login'),
    path('auth/admin/change-password/', AdminChangePasswordView.as_view(), name='auth-admin-change-password'),

    # Student Profile & Dashboard
    path('student/profile/', StudentProfileView.as_view(), name='student-profile'),
    path('student/profile/update/', StudentProfileUpdateView.as_view(), name='student-profile-update'),
    path('student/profile/photo/', StudentProfilePhotoView.as_view(), name='student-profile-photo'),
    path('student/dashboard/', StudentDashboardView.as_view(), name='student-dashboard'),
    path('student/id-card/', StudentIDCardView.as_view(), name='student-id-card'),
    path('student/referral/', ReferralCodeView.as_view(), name='student-referral'),
    path('student/referral/apply/', ReferralApplyView.as_view(), name='student-referral-apply'),
    path('student/referral/history/', ReferralHistoryView.as_view(), name='student-referral-history'),

    # Attendance
    path('attendance/scan/', StudentScanQRView.as_view(), name='attendance-scan'),
    path('attendance/logs/', StudentAttendanceLogsView.as_view(), name='attendance-logs'),

    # Memberships
    path('memberships/plans/', MembershipPlansListView.as_view(), name='memberships-plans'),
    path('memberships/history/', StudentMembershipHistoryView.as_view(), name='memberships-history'),

    # Payments
    path('payments/history/', StudentPaymentHistoryView.as_view(), name='payments-history'),
    path('payments/initiate/', StudentInitiatePaymentView.as_view(), name='payments-initiate'),

    # Seats
    path('seats/layout/', SeatLayoutView.as_view(), name='seats-layout'),
    path('seats/history/', StudentSeatHistoryView.as_view(), name='seats-history'),

    # Notifications
    path('notifications/list/', StudentNotificationsListView.as_view(), name='notifications-list'),
    path('notifications/read/<int:pk>/', NotificationReadView.as_view(), name='notifications-read'),
    path('notifications/register-device/', RegisterDeviceTokenView.as_view(), name='notifications-register-device'),

    # Library Info & Achievers
    path('library/info/', LibraryInfoView.as_view(), name='library-info'),
    path('library/achievers/', AchieversListView.as_view(), name='library-achievers'),
    path('library/reviews/', ReviewsListView.as_view(), name='library-reviews'),
    path('library/reviews/submit/', StudentSubmitReviewView.as_view(), name='library-reviews-submit'),

    # Study Features
    path('study/session/start/', StartStudySessionView.as_view(), name='study-session-start'),
    path('study/session/end/', EndStudySessionView.as_view(), name='study-session-end'),
    path('study/goal/', StudyGoalView.as_view(), name='study-goal'),

    # Admin Panel Management
    path('admin/dashboard/stats/', AdminDashboardStatsView.as_view(), name='admin-dashboard-stats'),
    path('admin/students/', AdminStudentsListView.as_view(), name='admin-students-list'),
    path('admin/students/<int:pk>/', AdminStudentProfileView.as_view(), name='admin-student-profile'),
    path('admin/payments/<int:pk>/verify/', AdminVerifyPaymentView.as_view(), name='admin-verify-payment'),
    path('admin/seats/<int:pk>/status/', AdminSeatStatusUpdateView.as_view(), name='admin-seat-status-update'),
    path('admin/seats/add/', AdminAddSeatView.as_view(), name='admin-add-seat'),
    path('admin/qrcode/generate/', AdminQRCodeGenerateView.as_view(), name='admin-qrcode-generate'),
    path('admin/attendance/manual/', AdminManualAttendanceView.as_view(), name='admin-manual-attendance'),
    path('admin/plans/<int:pk>/update/', AdminUpdateMembershipPlanView.as_view(), name='admin-update-plan'),
    path('admin/reviews/', AdminReviewsListView.as_view(), name='admin-reviews-list'),
    path('admin/reviews/<int:pk>/approve/', AdminApproveReviewView.as_view(), name='admin-approve-review'),
    path('admin/notifications/send/', AdminSendNotificationView.as_view(), name='admin-send-notification'),

    # Reports
    path('reports/attendance/', AdminAttendanceReportView.as_view(), name='reports-attendance'),
    path('reports/payments/', AdminPaymentReportView.as_view(), name='reports-payments'),
    path('reports/seats/', AdminSeatOccupancyReportView.as_view(), name='reports-seats'),

    # Super Admin
    path('superadmin/admins/add/', SuperAdminAddAdminView.as_view(), name='superadmin-add-admin'),
    path('superadmin/admins/<int:pk>/remove/', SuperAdminRemoveAdminView.as_view(), name='superadmin-remove-admin'),
    path('superadmin/admins/', SuperAdminListAdminsView.as_view(), name='superadmin-list-admins'),
]
