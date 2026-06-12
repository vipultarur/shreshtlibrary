from django.db import models
from django.conf import settings
from utils.images import compress_image_field


class StudentProfile(models.Model):
    GOAL_CHOICES = (
        ('UPSC', 'UPSC'),
        ('GPSC', 'GPSC'),
        ('CONSTABLE', 'Constable'),
        ('Banking', 'Banking'),
        ('Army', 'Army'),
        ('Teacher', 'Teacher'),
        ('Railway', 'Railway'),
        ('SSC', 'SSC'),
        ('CA', 'CA'),
        ('UPSC Exam', 'UPSC Exam'),
        ('SSC CGL', 'SSC CGL'),
        ('Other', 'Other'),
    )
    GENDER_CHOICES = (
        ('Male', 'Male'),
        ('Female', 'Female'),
        ('Other', 'Other'),
    )
    STATUS_CHOICES = (
        ('LIVE', 'Live'),
        ('EXPIRED', 'Expired'),
        ('SUSPENDED', 'Suspended'),
    )

    user = models.OneToOneField(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='student_profile')
    student_id = models.CharField(max_length=20, unique=True, null=True, blank=True)
    middle_name = models.CharField(max_length=100, null=True, blank=True)
    goal = models.CharField(max_length=50, choices=GOAL_CHOICES, default='Other')
    dob = models.DateField(null=True, blank=True)
    gender = models.CharField(max_length=20, choices=GENDER_CHOICES, default='Other')
    caste = models.CharField(max_length=50, null=True, blank=True)
    address = models.TextField(null=True, blank=True)
    profile_photo = models.ImageField(upload_to='profiles/', null=True, blank=True)
    parent_mobile = models.CharField(max_length=15, null=True, blank=True)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='LIVE')
    suspension_reason = models.TextField(null=True, blank=True)
    suspended_at = models.DateTimeField(null=True, blank=True)
    suspended_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='suspended_students')
    preferred_language = models.CharField(max_length=10, default='en')
    referred_by = models.ForeignKey('self', on_delete=models.SET_NULL, null=True, blank=True, related_name='referred_students')
    created_at = models.DateTimeField(auto_now_add=True, null=True)
    updated_at = models.DateTimeField(auto_now=True, null=True)

    class Meta:
        indexes = [
            models.Index(fields=['student_id']),
            models.Index(fields=['status']),
            models.Index(fields=['goal']),
            models.Index(fields=['created_at']),
        ]

    def save(self, *args, **kwargs):
        if not self.student_id:
            next_id = StudentProfile.objects.exclude(student_id__isnull=True).count() + 1
            self.student_id = f"SHR-{next_id:04d}"
        compress_image_field(self.profile_photo)
        super().save(*args, **kwargs)

    def __str__(self):
        return f"Student: {self.user.username}"


class ReferralCode(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='referral_codes')
    code = models.CharField(max_length=20, unique=True)
    used_by_count = models.IntegerField(default=0)
    benefit_given = models.CharField(max_length=255, null=True, blank=True)

    def __str__(self):
        return f"Code {self.code} by {self.student.username}"


class ReferralHistory(models.Model):
    referrer = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='referrals_sent')
    referred_student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='referrals_received')
    applied_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"{self.referred_student.username} referred by {self.referrer.username}"
