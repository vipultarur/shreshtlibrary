import os
import django
import random
from datetime import timedelta
from django.utils import timezone

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from apps.students.models import StudentProfile
from apps.study.models import StudySession

def seed_study_sessions():
    students = StudentProfile.objects.filter(status='LIVE')
    if not students.exists():
        print("No active students found.")
        return

    print(f"Generating study sessions for {students.count()} students...")
    
    # We want to generate data for the last 30 days
    now = timezone.now()
    
    # Track how many created
    count = 0
    
    for student in students:
        # Give each student between 5 to 20 study sessions in the last month
        num_sessions = random.randint(5, 20)
        
        for _ in range(num_sessions):
            # Random start time in the last 30 days
            days_ago = random.randint(0, 30)
            hours_ago = random.randint(0, 23)
            minutes_ago = random.randint(0, 59)
            
            start_time = now - timedelta(days=days_ago, hours=hours_ago, minutes=minutes_ago)
            
            # Duration between 60 minutes and 300 minutes (1 to 5 hours)
            duration_minutes = random.randint(60, 300)
            end_time = start_time + timedelta(minutes=duration_minutes)
            
            # Ensure end_time is not in the future
            if end_time > now:
                end_time = now
                duration_minutes = int((end_time - start_time).total_seconds() / 60)
            
            StudySession.objects.create(
                student=student.user,  # StudySession expects settings.AUTH_USER_MODEL which is usually User. Student has OneToOneField to User? Wait, let's check.
                start_time=start_time,
                end_time=end_time,
                status='completed',
                duration_minutes=duration_minutes
            )
            count += 1
            
    print(f"Successfully created {count} demo study sessions.")

if __name__ == "__main__":
    seed_study_sessions()
