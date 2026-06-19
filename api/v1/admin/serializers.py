from rest_framework import serializers
from django.contrib.auth import get_user_model
from apps.accounts.models import AdminUser
from apps.attendance.models import Attendance, Holiday, QRCode
from apps.library.models import Achiever, Facility, LibraryInfo, Review
from apps.memberships.models import Membership, MembershipPlan
from apps.notifications.models import Notification, StudentNotification
from apps.payments.models import Payment
from apps.seats.models import Floor, Seat, SeatAssignment, SeatChangeLog, SeatRow
from apps.students.models import StudentProfile
from apps.study.models import StudySession

User = get_user_model()

class AdminUserSerializer(serializers.ModelSerializer):
    profile_image = serializers.ImageField(required=False, allow_null=True)

    class Meta:
        model = AdminUser
        fields = [
            'id', 'username', 'first_name', 'last_name', 'email', 'mobile',
            'role', 'permissions', 'profile_image', 'is_active', 'date_joined', 'last_login'
        ]

AdminProfileSerializer = AdminUserSerializer

class UserBaseSerializer(serializers.ModelSerializer):
    class Meta:
        model = User
        fields = ['id', 'username', 'first_name', 'last_name', 'email', 'mobile', 'is_active']

class StudentProfileSerializer(serializers.ModelSerializer):
    username = serializers.CharField(source='user.username', read_only=True)
    first_name = serializers.CharField(source='user.first_name', read_only=True)
    last_name = serializers.CharField(source='user.last_name', read_only=True)
    email = serializers.CharField(source='user.email', read_only=True)
    mobile = serializers.CharField(source='user.mobile', read_only=True)
    is_active = serializers.BooleanField(source='user.is_active', read_only=True)
    profile_image = serializers.SerializerMethodField()

    class Meta:
        model = StudentProfile
        fields = [
            'id', 'user_id', 'student_id', 'username', 'first_name', 'middle_name', 'last_name',
            'email', 'mobile', 'parent_mobile', 'goal', 'dob', 'gender', 'caste', 'address',
            'profile_photo', 'profile_image', 'status', 'is_active', 'suspension_reason', 'suspended_at',
            'preferred_language', 'created_at', 'updated_at', 'joining_date', 'allowed_study_minutes'
        ]

    def get_profile_image(self, obj):
        """Return an absolute URL for the student's profile photo."""
        if not obj.profile_photo:
            return None
        request = self.context.get('request')
        url = obj.profile_photo.url
        if request:
            return request.build_absolute_uri(url)
        return url

class MembershipPlanSerializer(serializers.ModelSerializer):
    class Meta:
        model = MembershipPlan
        fields = '__all__'

class MembershipSerializer(serializers.ModelSerializer):
    student_name = serializers.SerializerMethodField()
    plan_name = serializers.CharField(source='plan.name', read_only=True)

    class Meta:
        model = Membership
        fields = '__all__'

    def get_student_name(self, obj):
        return f"{getattr(obj.student, 'first_name', '')} {getattr(obj.student, 'last_name', '')}".strip() or getattr(obj.student, 'username', '')

class PaymentSerializer(serializers.ModelSerializer):
    student_name = serializers.SerializerMethodField()
    plan_name = serializers.CharField(source='membership.plan.name', read_only=True)

    class Meta:
        model = Payment
        fields = '__all__'

    def get_student_name(self, obj):
        return f"{getattr(obj.student, 'first_name', '')} {getattr(obj.student, 'last_name', '')}".strip() or getattr(obj.student, 'username', '')

class AttendanceSerializer(serializers.ModelSerializer):
    student_name = serializers.SerializerMethodField()

    class Meta:
        model = Attendance
        fields = '__all__'

    def get_student_name(self, obj):
        return f"{getattr(obj.student, 'first_name', '')} {getattr(obj.student, 'last_name', '')}".strip() or getattr(obj.student, 'username', '')

class HolidaySerializer(serializers.ModelSerializer):
    class Meta:
        model = Holiday
        fields = '__all__'

class QRCodeSerializer(serializers.ModelSerializer):
    class Meta:
        model = QRCode
        fields = '__all__'

class SeatSerializer(serializers.ModelSerializer):
    student_name = serializers.SerializerMethodField()
    student_code = serializers.SerializerMethodField()

    class Meta:
        model = Seat
        fields = '__all__'

    def get_student_name(self, obj):
        if obj.student:
            return f"{getattr(obj.student, 'first_name', '')} {getattr(obj.student, 'last_name', '')}".strip() or getattr(obj.student, 'username', '')
        return None

    def get_student_code(self, obj):
        if obj.student and hasattr(obj.student, 'student_profile'):
            return obj.student.student_profile.student_id
        return None

class SeatRowSerializer(serializers.ModelSerializer):
    seats = SeatSerializer(many=True, read_only=True)

    class Meta:
        model = SeatRow
        fields = '__all__'

class FloorSerializer(serializers.ModelSerializer):
    rows = SeatRowSerializer(many=True, read_only=True)

    class Meta:
        model = Floor
        fields = '__all__'

class NotificationSerializer(serializers.ModelSerializer):
    class Meta:
        model = Notification
        fields = '__all__'

class ReviewSerializer(serializers.ModelSerializer):
    student_name = serializers.SerializerMethodField()

    class Meta:
        model = Review
        fields = '__all__'

    def get_student_name(self, obj):
        return f"{getattr(obj.student, 'first_name', '')} {getattr(obj.student, 'last_name', '')}".strip() or getattr(obj.student, 'username', '')

class LibraryInfoSerializer(serializers.ModelSerializer):
    class Meta:
        model = LibraryInfo
        fields = '__all__'

class FacilitySerializer(serializers.ModelSerializer):
    class Meta:
        model = Facility
        fields = '__all__'

class AchieverSerializer(serializers.ModelSerializer):
    class Meta:
        model = Achiever
        fields = '__all__'
