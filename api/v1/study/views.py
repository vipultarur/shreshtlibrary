from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.study.models import StudySession
from .serializers import StudySessionSerializer

class StartStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={201: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        active = StudySession.objects.filter(student=request.user, end_time__isnull=True).first()
        if active:
            return standard_response(
                message="You already have an active study session.",
                data=StudySessionSerializer(active).data,
            )
        
        session = StudySession.objects.create(
            student=request.user,
            status='starting'
        )
        return standard_response(
            message="Study session started.",
            data=StudySessionSerializer(session).data,
            status_code=status.HTTP_201_CREATED
        )


class UpdateStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=StudySessionSerializer, responses={200: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if not session:
            return Response({"errors": {"non_field_errors": ["No active study session found."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        new_status = request.data.get('status')
        if new_status in dict(StudySession.STATUS_CHOICES):
            session.status = new_status
            if 'duration_minutes' in request.data:
                session.duration_minutes = request.data.get('duration_minutes', session.duration_minutes)
            if 'paused_minutes' in request.data:
                session.paused_minutes = request.data.get('paused_minutes', session.paused_minutes)
            session.save()
            return standard_response(message=f"Session status updated to {new_status}.", data=StudySessionSerializer(session).data)
        return Response({"errors": {"status": ["Invalid status."]}}, status=status.HTTP_400_BAD_REQUEST)


class EndStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=StudySessionSerializer, responses={200: StudySessionSerializer}, tags=['Study Features'])
    def post(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if not session:
            return Response({"errors": {"non_field_errors": ["No active study session found to end."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        session.end_time = timezone.now()
        session.status = 'completed'
        session.duration_minutes = request.data.get('duration_minutes', 0)
        session.paused_minutes = request.data.get('paused_minutes', 0)
        session.save()

        return standard_response(
            message="Study session ended. Great effort!",
            data=StudySessionSerializer(session).data
        )

class CurrentStudySessionView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudySessionSerializer}, tags=['Study Features'])
    def get(self, request):
        session = StudySession.objects.filter(student=request.user, end_time__isnull=True).last()
        if session:
            return standard_response(data=StudySessionSerializer(session).data)
        return standard_response(data=None)

