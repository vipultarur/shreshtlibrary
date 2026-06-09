from django.db import models
from django.conf import settings

class Notification(models.Model):
    TYPE_CHOICES = (
        ('push', 'Push'),
        ('sms', 'SMS'),
        ('email', 'Email'),
        ('whatsapp', 'WhatsApp'),
        ('all', 'All'),
    )
    TARGET_CHOICES = (
        ('all', 'All'),
        ('active', 'Active'),
        ('expired', 'Expired'),
        ('specific', 'Specific'),
    )
    title = models.CharField(max_length=200)
    body = models.TextField()
    type = models.CharField(max_length=20, choices=TYPE_CHOICES, default='push')
    target_group = models.CharField(max_length=20, choices=TARGET_CHOICES, default='all')
    sent_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return self.title


class StudentNotification(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='student_notifications')
    notification = models.ForeignKey('notifications.Notification', on_delete=models.CASCADE, related_name='student_deliveries')
    is_read = models.BooleanField(default=False)
    read_at = models.DateTimeField(null=True, blank=True)

    def __str__(self):
        return f"Notification {self.notification.id} for {self.student.username} (Read: {self.is_read})"


class DeviceToken(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='device_tokens')
    token = models.TextField()
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"Token for {self.student.username}"
