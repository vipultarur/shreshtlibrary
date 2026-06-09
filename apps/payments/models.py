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
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='payments')
    membership = models.ForeignKey('memberships.Membership', on_delete=models.SET_NULL, null=True, blank=True, related_name='payments')
    amount = models.DecimalField(max_digits=10, decimal_places=2)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='pending')
    payment_mode = models.CharField(max_length=20, choices=MODE_CHOICES, default='Cash')
    payment_date = models.DateField(auto_now_add=True)
    transaction_id = models.CharField(max_length=100, null=True, blank=True)
    receipt_url = models.FileField(upload_to='receipts/', null=True, blank=True)
    notes = models.TextField(null=True, blank=True)

    def __str__(self):
        return f"Payment {self.id}: {self.student.username} - ₹{self.amount} ({self.status})"
