from django.db import models
from django.conf import settings


class Payment(models.Model):
    STATUS_CHOICES = (
        ('pending', 'Pending'),
        ('verified', 'Verified'),
        ('refunded', 'Refunded'),
        ('failed', 'Failed'),
    )
    MODE_CHOICES = (
        ('Cash', 'Cash'),
        ('UPI', 'UPI'),
        ('Card', 'Card'),
        ('Bank Transfer', 'Bank Transfer'),
    )
    payment_id = models.CharField(max_length=30, unique=True, null=True, blank=True)
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='payments')
    membership = models.ForeignKey('memberships.Membership', on_delete=models.SET_NULL, null=True, blank=True, related_name='payments')
    amount = models.DecimalField(max_digits=10, decimal_places=2)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='pending')
    payment_mode = models.CharField(max_length=20, choices=MODE_CHOICES, default='Cash')
    method = models.CharField(max_length=20, default='CASH')
    payment_date = models.DateField(auto_now_add=True)
    transaction_id = models.CharField(max_length=100, null=True, blank=True)
    transaction_ref = models.CharField(max_length=100, null=True, blank=True)
    receipt_url = models.FileField(upload_to='receipts/', null=True, blank=True)
    notes = models.TextField(null=True, blank=True)
    paid_at = models.DateTimeField(null=True, blank=True)
    recorded_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='recorded_payments')
    verified_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='verified_payments')
    verified_at = models.DateTimeField(null=True, blank=True)
    refund_amount = models.DecimalField(max_digits=10, decimal_places=2, null=True, blank=True)
    refund_reason = models.TextField(null=True, blank=True)
    refunded_at = models.DateTimeField(null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True, null=True)

    class Meta:
        indexes = [
            models.Index(fields=['student', 'status']),
            models.Index(fields=['payment_date']),
            models.Index(fields=['status']),
        ]

    def save(self, *args, **kwargs):
        if not self.method and self.payment_mode:
            self.method = self.payment_mode.upper().replace(' ', '_')
        if not self.transaction_ref:
            self.transaction_ref = self.transaction_id
        super().save(*args, **kwargs)
        if not self.payment_id:
            self.payment_id = f"PAY-{self.payment_date:%Y%m%d}-{self.id:03d}"
            super().save(update_fields=['payment_id'])

    def __str__(self):
        return f"Payment {self.id}: {self.student.username} - {self.amount} ({self.status})"
