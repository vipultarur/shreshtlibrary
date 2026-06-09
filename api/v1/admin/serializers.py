from rest_framework import serializers
from django.contrib.auth import get_user_model
from apps.accounts.models import AdminUser
from apps.students.models import StudentProfile
from apps.seats.models import Seat

User = get_user_model()

class AdminProfileSerializer(serializers.ModelSerializer):
    class Meta:
        model = AdminUser
        fields = ['id', 'username', 'first_name', 'last_name', 'email', 'mobile', 'role', 'permissions', 'is_active', 'date_joined']
        read_only_fields = ['id', 'username', 'date_joined']


class AdminStudentProfileUpdateSerializer(serializers.ModelSerializer):
    first_name = serializers.CharField(source='user.first_name', required=False)
    last_name = serializers.CharField(source='user.last_name', required=False)
    email = serializers.EmailField(source='user.email', required=False)
    is_active = serializers.BooleanField(source='user.is_active', required=False)

    class Meta:
        model = StudentProfile
        fields = ['first_name', 'last_name', 'email', 'is_active', 'goal', 'dob', 'caste', 'address', 'parent_mobile']

    def update(self, instance, validated_data):
        user_data = validated_data.pop('user', {})
        user = instance.user
        for attr, value in user_data.items():
            setattr(user, attr, value)
        user.save()

        for attr, value in validated_data.items():
            setattr(instance, attr, value)
        instance.save()
        return instance


class AdminAddSeatSerializer(serializers.ModelSerializer):
    class Meta:
        model = Seat
        fields = ['floor', 'row', 'seat_number', 'status']


class AdminManualAttendanceSerializer(serializers.Serializer):
    student_id = serializers.IntegerField(required=False, allow_null=True)
    student_mobile = serializers.CharField(required=False, allow_null=True, max_length=15)
    date = serializers.DateField(required=False)

    def validate(self, attrs):
        student_id = attrs.get('student_id')
        student_mobile = attrs.get('student_mobile')
        if not student_id and not student_mobile:
            raise serializers.ValidationError("Either student_id or student_mobile must be provided.")
        return attrs
