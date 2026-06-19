from django.core.management.base import BaseCommand
from django.utils import timezone
from apps.memberships.models import Membership
from apps.seats.models import Seat, SeatAssignment, SeatChangeLog
from apps.accounts.models import AdminUser

class Command(BaseCommand):
    help = 'Expires active memberships that have passed their end_date and releases seats.'

    def handle(self, *args, **options):
        today = timezone.now().date()
        
        # Get all active memberships that have passed end_date
        expired_memberships = Membership.objects.filter(status='active', end_date__lt=today)
        
        count = 0
        system_admin = AdminUser.objects.filter(role='super_admin').first()
        
        for membership in expired_memberships:
            student = membership.student
            
            # 1. Update Membership status
            membership.status = 'expired'
            membership.is_active = False
            membership.save(update_fields=['status', 'is_active'])
            
            # 2. Update StudentProfile status
            if hasattr(student, 'student_profile'):
                student.student_profile.status = 'EXPIRED'
                student.student_profile.save(update_fields=['status'])
                
            # 3. Unassign Seat if assigned
            seat = Seat.objects.filter(student=student).first()
            if seat:
                seat.student = None
                seat.status = 'available'
                seat.save(update_fields=['student', 'status'])
                
                # Update SeatAssignment
                SeatAssignment.objects.filter(
                    student=student, 
                    seat=seat, 
                    released_date__isnull=True
                ).update(released_date=today)
                
                # Log the change
                SeatChangeLog.objects.create(
                    seat=seat,
                    student=student,
                    action="UNASSIGNED",
                    changed_by=system_admin,
                    reason="Automated: Membership expired"
                )
                
            count += 1
            
        self.stdout.write(self.style.SUCCESS(f'Successfully expired {count} memberships.'))
