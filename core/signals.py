from django.db.models.signals import post_save, post_delete
from django.dispatch import receiver
from django.core.cache import cache

from apps.payments.models import Payment
from apps.attendance.models import Attendance
from apps.seats.models import Seat
from apps.students.models import StudentProfile

def invalidate_dashboard_cache():
    from django.utils import timezone
    today = timezone.now().date()
    keys_to_delete = []
    
    sections = [
        "students", "attendance/today", "payments/today", 
        "payments/month", "memberships", "seats", "overview"
    ]
    for section in sections:
        keys_to_delete.append(f"dashboard_stats_{section}_{today}")
        
    domains = ["attendance", "revenue", "students", "memberships", "seats", "overview"]
    for domain in domains:
        # Chart views use cache_key = f"dashboard_chart_{domain}_{chart}_{today}"
        for chart in ["line", "bar", "pie", "doughnut", "radar", "polarArea"]:
            keys_to_delete.append(f"dashboard_chart_{domain}_{chart}_{today}")

    cache.delete_many(keys_to_delete)

@receiver([post_save, post_delete], sender=Payment)
@receiver([post_save, post_delete], sender=Attendance)
@receiver([post_save, post_delete], sender=Seat)
@receiver([post_save, post_delete], sender=StudentProfile)
def clear_dashboard_stats_on_update(sender, instance, **kwargs):
    invalidate_dashboard_cache()
