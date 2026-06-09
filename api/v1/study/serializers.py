from rest_framework import serializers
from apps.study.models import StudySession, StudyGoal

class StudySessionSerializer(serializers.ModelSerializer):
    class Meta:
        model = StudySession
        fields = ['id', 'start_time', 'end_time', 'duration_minutes']
        read_only_fields = ['id', 'duration_minutes']


class StudyGoalSerializer(serializers.ModelSerializer):
    class Meta:
        model = StudyGoal
        fields = ['id', 'date', 'target_hours', 'achieved_hours']
        read_only_fields = ['id']
