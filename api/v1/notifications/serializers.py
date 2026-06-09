from rest_framework import serializers
from apps.notifications.models import Notification, StudentNotification, DeviceToken

class NotificationSerializer(serializers.ModelSerializer):
    class Meta:
        model = Notification
        fields = ['id', 'title', 'body', 'type', 'sent_at']
        read_only_fields = ['id', 'sent_at']


class StudentNotificationSerializer(serializers.ModelSerializer):
    title = serializers.CharField(source='notification.title', read_only=True)
    body = serializers.CharField(source='notification.body', read_only=True)
    sent_at = serializers.DateTimeField(source='notification.sent_at', read_only=True)

    class Meta:
        model = StudentNotification
        fields = ['id', 'title', 'body', 'sent_at', 'is_read', 'read_at']
        read_only_fields = ['id', 'title', 'body', 'sent_at', 'read_at']


class DeviceTokenSerializer(serializers.ModelSerializer):
    class Meta:
        model = DeviceToken
        fields = ['token']
