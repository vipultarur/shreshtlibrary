from django.core.management.base import BaseCommand
from django.utils import timezone
from apps.attendance.models import Attendance
from apps.study.models import StudySession
from apps.library.models import LibraryInfo

class Command(BaseCommand):
    help = 'Automatically check out students left checked in after library closing time.'

    def handle(self, *args, **options):
        now = timezone.now()
        lib_info = LibraryInfo.objects.first()
        
        if not lib_info or not lib_info.close_time:
            self.stdout.write("Library close time not set. Skipping.")
            return
            
        close_time = lib_info.close_time
        today = now.date()
        
        if now.time() < close_time:
            today = (now - timezone.timedelta(days=1)).date()
        
        pending_sessions = StudySession.objects.filter(
            end_time__isnull=True,
            start_time__date=today
        )
        count_sessions = 0
        for session in pending_sessions:
            session.end_time = timezone.datetime.combine(today, close_time)
            session.end_time = timezone.make_aware(session.end_time, timezone.get_current_timezone())
            session.status = 'completed'
            delta = session.end_time - session.start_time
            if delta.total_seconds() > 0:
                session.duration_minutes = int(delta.total_seconds() / 60)
            else:
                session.duration_minutes = 0
            session.save()
            count_sessions += 1
            
        pending_attendance = Attendance.objects.filter(
            date=today,
            is_present=True,
            time_out__isnull=True
        )
        count_attendance = pending_attendance.update(time_out=close_time)
        
        self.stdout.write(self.style.SUCCESS(f"Auto-checkout complete. Ended {count_sessions} study sessions and checked out {count_attendance} students for {today}."))
