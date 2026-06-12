from django.db import models
from django.conf import settings

class StudySession(models.Model):
    STATUS_CHOICES = (
        ('starting', 'Starting'),
        ('active', 'Active'),
        ('paused', 'Paused'),
        ('completed', 'Completed'),
    )
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='study_sessions')
    start_time = models.DateTimeField(auto_now_add=True)
    end_time = models.DateTimeField(null=True, blank=True)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='starting')
    duration_minutes = models.IntegerField(default=0)
    paused_minutes = models.IntegerField(default=0)

    def __str__(self):
        return f"{self.student.username} session: {self.start_time.strftime('%Y-%m-%d %H:%M')} - {self.status}"

