from rest_framework import serializers
from django.contrib.auth import get_user_model
from apps.students.models import StudentProfile, ReferralCode, ReferralHistory

User = get_user_model()

class StudentProfileSerializer(serializers.ModelSerializer):
    username = serializers.CharField(source='user.username', read_only=True)
    first_name = serializers.CharField(source='user.first_name')
    last_name = serializers.CharField(source='user.last_name')
    email = serializers.EmailField(source='user.email')
    mobile = serializers.CharField(source='user.mobile', read_only=True)

    class Meta:
        model = StudentProfile
        fields = ['username', 'first_name', 'last_name', 'email', 'mobile', 'goal', 'dob', 'caste', 'address', 'profile_photo', 'parent_mobile']
        ref_name = "AppStudentProfile"

    def update(self, instance, validated_data):
        # Update nested user fields
        user_data = validated_data.pop('user', {})
        user = instance.user
        for attr, value in user_data.items():
            setattr(user, attr, value)
        user.save()

        # Update profile fields
        for attr, value in validated_data.items():
            setattr(instance, attr, value)
        instance.save()
        return instance


class ReferralCodeSerializer(serializers.ModelSerializer):
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)

    class Meta:
        model = ReferralCode
        fields = ['id', 'student_name', 'code', 'used_by_count', 'benefit_given']
        read_only_fields = ['id', 'student_name', 'used_by_count', 'benefit_given']


class ReferralHistorySerializer(serializers.ModelSerializer):
    referred_student_name = serializers.CharField(source='referred_student.get_full_name', read_only=True)

    class Meta:
        model = ReferralHistory
        fields = ['id', 'referred_student_name', 'applied_at']
