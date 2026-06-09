from rest_framework import serializers
from apps.memberships.models import MembershipPlan, Membership

class MembershipPlanSerializer(serializers.ModelSerializer):
    class Meta:
        model = MembershipPlan
        fields = ['id', 'name', 'duration_months', 'price', 'description', 'is_active']
        read_only_fields = ['id']


class MembershipSerializer(serializers.ModelSerializer):
    plan_name = serializers.CharField(source='plan.name', read_only=True)
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)

    class Meta:
        model = Membership
        fields = ['id', 'student_name', 'plan', 'plan_name', 'start_date', 'end_date', 'status']
        read_only_fields = ['id', 'plan_name', 'student_name']
