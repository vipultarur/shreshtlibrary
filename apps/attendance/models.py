from django.db import models
from django.conf import settings
import uuid


class QRCode(models.Model):
    token = models.UUIDField(unique=True, null=True, blank=True)
    code = models.CharField(max_length=255, unique=True)
    qr_hash = models.TextField(blank=True, default='')
    created_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True)
    valid_date = models.DateField()
    is_expired = models.BooleanField(default=False)
    is_active = models.BooleanField(default=True)
    generation_method = models.CharField(max_length=20, default='MANUAL')
    expiry_timestamp = models.DateTimeField()
    expires_at = models.DateTimeField(null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True, null=True)

    def save(self, *args, **kwargs):
        if not self.token:
            self.token = uuid.uuid4()
        if not self.code:
            self.code = str(self.token)
        if not self.qr_hash:
            self.qr_hash = self.code
        if not self.expires_at:
            self.expires_at = self.expiry_timestamp
        super().save(*args, **kwargs)

    def __str__(self):
        return f"QR: {self.code[:10]}... (Date: {self.valid_date})"


class Attendance(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='attendances')
    date = models.DateField()
    time_in = models.TimeField(auto_now_add=True)
    qr_code = models.ForeignKey('attendance.QRCode', on_delete=models.SET_NULL, null=True, blank=True)
    is_present = models.BooleanField(default=True)
    is_manual = models.BooleanField(default=False)
    method = models.CharField(max_length=20, default='QR')
    marked_at = models.DateTimeField(auto_now_add=True, null=True)
    marked_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='marked_attendance')
    note = models.TextField(null=True, blank=True)

    class Meta:
        unique_together = ('student', 'date')
        indexes = [
            models.Index(fields=['student', 'date']),
            models.Index(fields=['date']),
            models.Index(fields=['is_present']),
        ]

    def __str__(self):
        return f"{self.student.username} - {self.date} - Present: {self.is_present}"


class Holiday(models.Model):
    date = models.DateField(unique=True)
    title = models.CharField(max_length=120)
    description = models.TextField(null=True, blank=True)
    is_active = models.BooleanField(default=True)
    created_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='created_holidays')
    created_at = models.DateTimeField(auto_now_add=True, null=True)
    updated_at = models.DateTimeField(auto_now=True, null=True)

    class Meta:
        ordering = ['date']
        indexes = [
            models.Index(fields=['date', 'is_active']),
        ]

    def __str__(self):
        return f"{self.date} - {self.title}"
