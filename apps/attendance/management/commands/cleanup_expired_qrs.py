from django.core.management.base import BaseCommand
from django.utils import timezone
from apps.attendance.models import QRCode
import datetime

class Command(BaseCommand):
    help = 'Deletes QR codes that have expired more than a month ago to save space.'

    def handle(self, *args, **options):
        now = timezone.now()
        one_month_ago = now - datetime.timedelta(days=30)
        
        expired_qrs = QRCode.objects.filter(
            expiry_timestamp__lt=one_month_ago
        )
        
        count, _ = expired_qrs.delete()
        
        self.stdout.write(self.style.SUCCESS(f"Successfully deleted {count} expired QR codes."))
