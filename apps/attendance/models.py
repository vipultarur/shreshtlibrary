from django.db import models
from django.conf import settings

class QRCode(models.Model):
    code = models.CharField(max_length=255, unique=True)
    created_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True)
    valid_date = models.DateField()
    is_expired = models.BooleanField(default=False)
    expiry_timestamp = models.DateTimeField()

    def __str__(self):
        return f"QR: {self.code[:10]}... (Date: {self.valid_date})"


class Attendance(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='attendances')
    date = models.DateField()
    time_in = models.TimeField(auto_now_add=True)
    qr_code = models.ForeignKey('attendance.QRCode', on_delete=models.SET_NULL, null=True, blank=True)
    is_manual = models.BooleanField(default=False)

    class Meta:
        unique_together = ('student', 'date')

    def __str__(self):
        return f"{self.student.username} - {self.date} - In: {self.time_in}"
