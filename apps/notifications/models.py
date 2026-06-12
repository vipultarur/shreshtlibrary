from django.db import models
from django.conf import settings


class Notification(models.Model):
    TYPE_CHOICES = (
        ('push', 'Push'),
        ('sms', 'SMS'),
        ('email', 'Email'),
        ('whatsapp', 'WhatsApp'),
        ('all', 'All'),
        ('GENERAL', 'General'),
        ('ANNOUNCEMENT', 'Announcement'),
        ('PAYMENT', 'Payment'),
        ('ATTENDANCE', 'Attendance'),
        ('PLAN_EXPIRY', 'Plan Expiry'),
        ('SEAT_CHANGE', 'Seat Change'),
        ('HOLIDAY', 'Holiday'),
    )
    TARGET_CHOICES = (
        ('all', 'All'),
        ('active', 'Active'),
        ('expired', 'Expired'),
        ('specific', 'Specific'),
        ('ALL', 'All'),
        ('GROUP', 'Group'),
        ('INDIVIDUAL', 'Individual'),
    )
    title = models.CharField(max_length=200)
    body = models.TextField()
    
    # Rich Content Fields (New)
    subtitle = models.CharField(max_length=300, blank=True, default='')
    description = models.TextField(blank=True, default='')
    link_url = models.URLField(blank=True, default='')
    link_button_text = models.CharField(max_length=100, blank=True, default='')
    event_date = models.DateField(null=True, blank=True)

    # Layout
    layout = models.CharField(choices=[
        ('text_only', 'Text Only'),
        ('half_image', 'Half Image'),
        ('full_image', 'Full Image'),
        ('background_image', 'Background Image'),
    ], default='text_only', max_length=20)
    background_image = models.ImageField(upload_to='notifications/', null=True, blank=True)

    # Audience targeting
    audience = models.CharField(choices=[
        ('all', 'All Students'),
        ('new', 'New Students (< 7 days)'),
        ('premium', 'Premium (Active Plan)'),
        ('non_premium', 'Non-Premium (No Active Plan)'),
        ('expired', 'Expired Plan'),
        ('selected', 'Selected Students'),
    ], default='all', max_length=20)
    
    # Scheduling & Duration
    display_mode = models.CharField(choices=[
        ('one_time', 'One Time (remove after seen)'),
        ('persistent', 'Persistent (stays until dismissed)'),
        ('recurring', 'Recurring Daily'),
    ], default='persistent', max_length=20)
    recurring_time = models.TimeField(null=True, blank=True)  # For recurring notifications
    expires_at = models.DateTimeField(null=True, blank=True)

    type = models.CharField(max_length=30, choices=TYPE_CHOICES, default='push')
    target_group = models.CharField(max_length=20, choices=TARGET_CHOICES, default='all')
    target = models.CharField(max_length=20, default='ALL')
    goal_filter = models.CharField(max_length=50, null=True, blank=True)
    status_filter = models.CharField(max_length=20, null=True, blank=True)
    send_push = models.BooleanField(default=True)
    send_email = models.BooleanField(default=False)
    send_sms = models.BooleanField(default=False)
    scheduled_at = models.DateTimeField(null=True, blank=True)
    sent_at = models.DateTimeField(null=True, blank=True)
    total_recipients = models.IntegerField(default=0)
    success_count = models.IntegerField(default=0)
    failure_count = models.IntegerField(default=0)
    created_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='created_notifications')
    created_at = models.DateTimeField(auto_now_add=True, null=True)

    def __str__(self):
        return self.title

class NotificationImage(models.Model):
    notification = models.ForeignKey(Notification, on_delete=models.CASCADE, related_name='images')
    image = models.ImageField(upload_to='notifications/')
    sort_order = models.IntegerField(default=0)

    class Meta:
        ordering = ['sort_order']



class StudentNotification(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='student_notifications')
    notification = models.ForeignKey('notifications.Notification', on_delete=models.CASCADE, related_name='student_deliveries')
    is_read = models.BooleanField(default=False)
    push_delivered = models.BooleanField(default=False)
    email_delivered = models.BooleanField(default=False)
    sms_delivered = models.BooleanField(default=False)
    delivered_at = models.DateTimeField(null=True, blank=True)
    read_at = models.DateTimeField(null=True, blank=True)

    def __str__(self):
        return f"Notification {self.notification.id} for {self.student.username} (Read: {self.is_read})"


class DeviceToken(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='device_tokens')
    token = models.TextField()
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"Token for {self.student.username}"

class AdminInboxNotification(models.Model):
    TYPE_CHOICES = (
        ('NEW_STUDENT', 'New Student'),
        ('PAYMENT', 'Payment'),
        ('SUPPORT', 'Support'),
        ('EXPIRING_SOON', 'Expiring Soon'),
        ('EXPIRED', 'Expired'),
        ('OTHER', 'Other'),
    )
    title = models.CharField(max_length=200)
    message = models.TextField()
    type = models.CharField(max_length=50, choices=TYPE_CHOICES, default='OTHER')
    is_read = models.BooleanField(default=False)
    related_id = models.CharField(max_length=100, null=True, blank=True)
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.SET_NULL, null=True, blank=True, related_name='admin_inbox_notifications')
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"{self.type} - {self.title}"
