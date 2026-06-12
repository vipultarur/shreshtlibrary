from django.core.management.base import BaseCommand
from django.utils import timezone
import datetime
from apps.attendance.models import Holiday
from apps.notifications.models import Notification, StudentNotification
from django.contrib.auth import get_user_model
from utils.fcm import send_push_notification

User = get_user_model()

class Command(BaseCommand):
    help = 'Sends notifications to students 1 day before an active holiday'

    def handle(self, *args, **options):
        tomorrow = timezone.now().date() + datetime.timedelta(days=1)
        holidays = Holiday.objects.filter(date=tomorrow, is_active=True)

        if not holidays.exists():
            self.stdout.write("No holidays scheduled for tomorrow.")
            return

        students = User.objects.filter(role='student', is_active=True)
        if not students.exists():
            self.stdout.write("No active students to notify.")
            return

        for holiday in holidays:
            # Check if notification already exists to prevent duplicate
            title = f"Reminder: Upcoming Holiday Tomorrow"
            body = f"Just a reminder that the library will be closed tomorrow, {holiday.date.strftime('%B %d, %Y')}, due to {holiday.title}."
            
            # Avoid sending duplicate reminders
            if Notification.objects.filter(title=title, created_at__date=timezone.now().date()).exists():
                self.stdout.write(f"Notification for {holiday.title} already sent today.")
                continue

            notification = Notification.objects.create(
                title=title,
                body=body,
                type="HOLIDAY",
                target="ALL",
                target_group="all",
                send_push=True,
                sent_at=timezone.now()
            )
            
            notification.total_recipients = students.count()
            notification.success_count = students.count()
            notification.save(update_fields=['total_recipients', 'success_count'])

            for student in students:
                StudentNotification.objects.create(
                    student=student,
                    notification=notification
                )
                
                # In a real setup, send_push_notification might be queued via celery.
                from apps.notifications.models import DeviceToken
                tokens = DeviceToken.objects.filter(student=student)
                for token_obj in tokens:
                    send_push_notification(token_obj.token, title, body)
                    
            self.stdout.write(self.style.SUCCESS(f"Successfully sent advance notification for {holiday.title}."))
