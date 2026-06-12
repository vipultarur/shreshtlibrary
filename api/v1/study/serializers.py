from rest_framework import serializers
from apps.study.models import StudySession

class StudySessionSerializer(serializers.ModelSerializer):
    class Meta:
        model = StudySession
        fields = ['id', 'start_time', 'end_time', 'status', 'duration_minutes', 'paused_minutes']
        read_only_fields = ['id', 'start_time']

