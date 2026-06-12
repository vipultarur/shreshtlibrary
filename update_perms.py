import re

mapping = {
    'manage_students': ['AdminStudentsView', 'AdminStudentDetailView', 'AdminStudentPhotoView', 'AdminStudentAnalyticsView', 'AdminStudentStatusView', 'AdminStudentRelatedView', 'AdminStudentCountsView', 'AdminStudentExportView'],
    'manage_plans': ['PlansView', 'PlansAllView', 'PlanDetailView', 'PlanToggleView', 'PlanStudentsView', 'PlanStatsView', 'AdminMembershipsView', 'AdminMembershipActionView', 'AdminMembershipDetailView', 'AdminMembershipSpecialView'],
    'manage_attendance': ['StudentQRTodayView', 'StudentQRScanView', 'AdminQRView', 'AdminAttendanceView', 'AdminAttendanceDetailView', 'AdminAttendanceSummaryView'],
    'manage_holidays': ['HolidayView'],
    'manage_payments': ['AdminPaymentsView', 'AdminPaymentDetailView', 'AdminPaymentActionView', 'AdminPaymentReceiptView', 'AdminPaymentSpecialView'],
    'manage_seats': ['AdminSeatsLayoutView', 'AdminSeatsView', 'AdminSeatDetailView', 'FloorView', 'RowView', 'SeatActionView', 'SeatSpecialView'],
    'manage_notifications': ['AdminNotificationsView', 'AdminNotificationSendView', 'AdminNotificationDetailView', 'AdminNotificationRecipientsView', 'AdminNotificationScheduleView', 'AdminNotificationScheduledView', 'AdminNotificationTemplatesView'],
    'manage_library': ['LibraryInfoAdminView', 'AdminFacilitiesView', 'AdminFacilityDetailView', 'AdminFacilityToggleView', 'AdminAchieversView', 'AdminAchieverDetailView', 'AdminAchieverToggleView'],
    'manage_reviews': ['AdminReviewsView', 'AdminReviewActionView'],
}

file_path = 'd:\\extra\\shresht\\shreshtlibrary\\api\\v1\\v2_admin.py'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('from shreshtlibrary.utils.permissions import IsLibraryAdmin, IsSuperAdmin', 'from shreshtlibrary.utils.permissions import IsLibraryAdmin, IsSuperAdmin, HasAdminPermission')

for perm, views in mapping.items():
    for view in views:
        pattern = r'class ' + view + r'\(APIView\):\n    permission_classes = \[IsLibraryAdmin\]'
        replacement = 'class ' + view + '(APIView):\n    permission_classes = [HasAdminPermission("' + perm + '")]'
        content = re.sub(pattern, replacement, content)
        
        pattern2 = r'class ' + view + r'\(APIView\):\n\n    def get_permissions\(self\):\n        return \[IsLibraryAdmin\(\)\]'
        replacement2 = 'class ' + view + '(APIView):\n\n    def get_permissions(self):\n        return [HasAdminPermission("' + perm + '")()]'
        content = re.sub(pattern2, replacement2, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated permissions in v2_admin.py')
