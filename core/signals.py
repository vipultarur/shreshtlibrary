from django.db.models.signals import post_save, post_delete
from django.dispatch import receiver
from django.core.cache import cache

from apps.payments.models import Payment
from apps.attendance.models import Attendance
from apps.seats.models import Seat
from apps.students.models import StudentProfile

def invalidate_dashboard_cache():
    # Django's default locmemcache doesn't support wildcard deletes easily unless we clear all,
    # but we can try to clear known keys or clear all.
    # For a simple cache, clear all might be too aggressive, but since dashboard stats are heavy,
    # and default cache is simple, clearing is safer. Or we can just let it expire in 5 minutes.
    # To be precise, since dashboard keys have today's date, we clear the entire cache for simplicity if not using Redis,
    # or rely on cache.clear().
    cache.clear()

@receiver([post_save, post_delete], sender=Payment)
@receiver([post_save, post_delete], sender=Attendance)
@receiver([post_save, post_delete], sender=Seat)
@receiver([post_save, post_delete], sender=StudentProfile)
def clear_dashboard_stats_on_update(sender, instance, **kwargs):
    invalidate_dashboard_cache()
