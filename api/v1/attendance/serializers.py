from rest_framework import serializers
from apps.attendance.models import Attendance, QRCode

class AttendanceSerializer(serializers.ModelSerializer):
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)

    class Meta:
        model = Attendance
        fields = ['id', 'student_name', 'date', 'time_in', 'time_out', 'late_mark', 'under_time', 'total_hours', 'is_manual']
        read_only_fields = ['id', 'student_name', 'time_in', 'time_out', 'late_mark', 'under_time', 'total_hours']


class QRCodeSerializer(serializers.ModelSerializer):
    class Meta:
        model = QRCode
        fields = ['id', 'code', 'valid_date', 'is_expired', 'expiry_timestamp']
        read_only_fields = ['id']
