from rest_framework import serializers
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.notifications.models import StudentNotification, DeviceToken
from .serializers import StudentNotificationSerializer, DeviceTokenSerializer

class StudentNotificationsListView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudentNotificationSerializer(many=True)}, tags=['Notifications'])
    def get(self, request):
        notes = StudentNotification.objects.filter(student=request.user).order_by('-notification__sent_at')
        serializer = StudentNotificationSerializer(notes, many=True)
        return standard_response(data=serializer.data)


class NotificationReadView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsStudent]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Notifications'])
    def post(self, request, pk):
        note = get_object_or_404(StudentNotification, id=pk, student=request.user)
        if not note.is_read:
            note.is_read = True
            note.read_at = timezone.now()
            note.save()
        return standard_response(message="Notification marked as read.")


class RegisterDeviceTokenView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=DeviceTokenSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Notifications'])
    def post(self, request):
        serializer = DeviceTokenSerializer(data=request.data)
        if serializer.is_valid():
            token = serializer.validated_data['token']
            user = request.user
            # Ensure unique device registration
            DeviceToken.objects.get_or_create(student=user, token=token)
            return standard_response(message="FCM device token registered successfully.")
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)
