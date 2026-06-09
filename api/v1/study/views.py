from decimal import Decimal
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.study.models import StudySession, StudyGoal
from .serializers import StudySessionSerializer, StudyGoalSerializer

class StartStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={201: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        # Prevent starting if a session is already active
        active = StudySession.objects.filter(student=request.user, end_time__isnull=True).exists()
        if active:
            return Response({"errors": {"non_field_errors": ["You already have an active study session running."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        session = StudySession.objects.create(
            student=request.user,
            start_time=timezone.now()
        )
        return standard_response(
            message="Study session started. Stay focused!",
            data=StudySessionSerializer(session).data,
            status_code=status.HTTP_201_CREATED
        )


class EndStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if not session:
            return Response({"errors": {"non_field_errors": ["No active study session found to end."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        session.end_time = timezone.now()
        duration = session.end_time - session.start_time
        session.duration_minutes = int(duration.total_seconds() / 60)
        session.save()

        # Try to automatically add to today's study goal achievement
        today = timezone.now().date()
        goal, created = StudyGoal.objects.get_or_create(student=request.user, date=today, defaults={"target_hours": 6.00})
        goal.achieved_hours += Decimal(session.duration_minutes) / Decimal(60)
        goal.save()

        return standard_response(
            message="Study session ended. Great effort!",
            data=StudySessionSerializer(session).data
        )


class StudyGoalView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudyGoalSerializer}, tags=['Study Features'])
    def get(self, request):
        today = timezone.now().date()
        goal, created = StudyGoal.objects.get_or_create(student=request.user, date=today, defaults={"target_hours": 6.00})
        return standard_response(data=StudyGoalSerializer(goal).data)

    @extend_schema(request=StudyGoalSerializer, responses={200: StudyGoalSerializer}, tags=['Study Features'])
    def post(self, request):
        today = timezone.now().date()
        goal, created = StudyGoal.objects.get_or_create(student=request.user, date=today, defaults={"target_hours": 6.00})
        target = request.data.get('target_hours')
        if target is not None:
            goal.target_hours = target
            goal.save()
            return standard_response(message="Daily study goal updated.", data=StudyGoalSerializer(goal).data)
        return Response({"errors": {"target_hours": ["This field is required."]}}, status=status.HTTP_400_BAD_REQUEST)
