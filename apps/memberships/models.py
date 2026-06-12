from django.db import models
from django.conf import settings


class MembershipPlan(models.Model):
    name = models.CharField(max_length=100)
    duration_months = models.IntegerField()
    duration_days = models.IntegerField(default=30)
    price = models.DecimalField(max_digits=10, decimal_places=2)
    benefits = models.JSONField(default=list, blank=True)
    description = models.TextField(null=True, blank=True)
    is_active = models.BooleanField(default=True)
    sort_order = models.IntegerField(default=0)
    created_at = models.DateTimeField(auto_now_add=True, null=True)
    updated_at = models.DateTimeField(auto_now=True, null=True)

    def save(self, *args, **kwargs):
        if not self.duration_days and self.duration_months:
            self.duration_days = self.duration_months * 30
        super().save(*args, **kwargs)

    def __str__(self):
        return self.name


class Membership(models.Model):
    STATUS_CHOICES = (
        ('active', 'Active'),
        ('expired', 'Expired'),
        ('suspended', 'Suspended'),
        ('cancelled', 'Cancelled'),
    )
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='memberships')
    plan = models.ForeignKey('memberships.MembershipPlan', on_delete=models.PROTECT)
    plan_name_snapshot = models.CharField(max_length=100, blank=True, default='')
    price_snapshot = models.DecimalField(max_digits=10, decimal_places=2, default=0)
    start_date = models.DateField()
    end_date = models.DateField()
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='active')
    is_active = models.BooleanField(default=True)
    renewal_count = models.IntegerField(default=0)
    notes = models.TextField(null=True, blank=True)
    created_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='created_memberships')
    created_at = models.DateTimeField(auto_now_add=True, null=True)

    class Meta:
        indexes = [
            models.Index(fields=['student', 'is_active']),
            models.Index(fields=['end_date']),
        ]

    def save(self, *args, **kwargs):
        if self.plan and not self.plan_name_snapshot:
            self.plan_name_snapshot = self.plan.name
        if self.plan and not self.price_snapshot:
            self.price_snapshot = self.plan.price
        self.is_active = self.status == 'active'
        super().save(*args, **kwargs)

    def __str__(self):
        return f"{self.student.username} - {self.plan.name} ({self.status})"
