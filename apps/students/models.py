from django.db import models
from django.conf import settings

class StudentProfile(models.Model):
    GOAL_CHOICES = (
        ('UPSC', 'UPSC'),
        ('GPSC', 'GPSC'),
        ('Banking', 'Banking'),
        ('Army', 'Army'),
        ('Teacher', 'Teacher'),
        ('Railway', 'Railway'),
        ('CA', 'CA'),
        ('Other', 'Other'),
    )
    user = models.OneToOneField(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='student_profile')
    goal = models.CharField(max_length=50, choices=GOAL_CHOICES, default='Other')
    dob = models.DateField(null=True, blank=True)
    caste = models.CharField(max_length=50, null=True, blank=True)
    address = models.TextField(null=True, blank=True)
    profile_photo = models.ImageField(upload_to='profiles/', null=True, blank=True)
    parent_mobile = models.CharField(max_length=15, null=True, blank=True)

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
