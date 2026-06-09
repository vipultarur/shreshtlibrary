import os
import django
import datetime

# Setup django environment
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from django.utils import timezone
from django.contrib.auth import get_user_model
from apps.accounts.models import AdminUser
from apps.students.models import StudentProfile, ReferralCode
from apps.memberships.models import MembershipPlan, Membership
from apps.seats.models import Seat, SeatAssignment
from apps.attendance.models import QRCode, Attendance
from apps.payments.models import Payment
from core.models import GlobalSetting

User = get_user_model()

def seed():
    print("Seeding demo data into Supabase...")
    
    # Clean previous data to ensure a fresh, reproducible state
    Attendance.objects.all().delete()
    Payment.objects.all().delete()
    Membership.objects.all().delete()
    SeatAssignment.objects.all().delete()
    Seat.objects.all().delete()

    # 1. Create Admins
    admin1, _ = AdminUser.objects.get_or_create(
        username="admin1",
        defaults={
            "email": "admin1@shresht.com",
            "mobile": "8888888888",
            "role": "admin",
            "permissions": {"all": True}
        }
    )
    admin1.set_password("adminpassword123")
    admin1.save()

    superadmin, _ = AdminUser.objects.get_or_create(
        username="superadmin",
        defaults={
            "email": "superadmin@shresht.com",
            "mobile": "9999999999",
            "role": "super_admin",
            "permissions": {"all": True}
        }
    )
    superadmin.set_password("superpassword123")
    superadmin.save()

    # 2. Create Students
    student1, _ = User.objects.get_or_create(
        username="student1",
        defaults={
            "email": "student1@gmail.com",
            "mobile": "7777777777",
            "role": "student"
        }
    )
    student1.set_password("studentpassword123")
    student1.save()

    StudentProfile.objects.get_or_create(
        user=student1,
        defaults={
            "goal": "UPSC Exam",
            "dob": datetime.date(2000, 1, 1),
            "caste": "General",
            "address": "123 Library Street, New Delhi",
            "parent_mobile": "9876543210"
        }
    )

    student2, _ = User.objects.get_or_create(
        username="student2",
        defaults={
            "email": "student2@gmail.com",
            "mobile": "6666666666",
            "role": "student"
        }
    )
    student2.set_password("studentpassword123")
    student2.save()

    StudentProfile.objects.get_or_create(
        user=student2,
        defaults={
            "goal": "SSC CGL",
            "dob": datetime.date(1999, 5, 12),
            "caste": "OBC",
            "address": "456 Academy Road, New Delhi",
            "parent_mobile": "9876543211"
        }
    )

    # 3. Create Referral Codes
    ReferralCode.objects.get_or_create(
        student=student1,
        defaults={
            "code": "STUDENT1REF",
            "used_by_count": 0,
            "benefit_given": "10% off on next renewal"
        }
    )

    # 4. Create Membership Plans
    plan_basic, _ = MembershipPlan.objects.get_or_create(
        name="Basic Plan (6 Hours)",
        defaults={
            "description": "Access for 6 hours daily",
            "price": 800.00,
            "duration_months": 1
        }
    )

    plan_premium, _ = MembershipPlan.objects.get_or_create(
        name="Premium Plan (12 Hours)",
        defaults={
            "description": "Access for 12 hours daily",
            "price": 1500.00,
            "duration_months": 1
        }
    )

    # 5. Create Memberships
    m1, _ = Membership.objects.get_or_create(
        student=student1,
        plan=plan_premium,
        defaults={
            "start_date": timezone.now().date() - datetime.timedelta(days=5),
            "end_date": timezone.now().date() + datetime.timedelta(days=25),
            "status": "active"
        }
    )

    m2, _ = Membership.objects.get_or_create(
        student=student2,
        plan=plan_basic,
        defaults={
            "start_date": timezone.now().date() - datetime.timedelta(days=1),
            "end_date": timezone.now().date() + datetime.timedelta(days=29),
            "status": "active"
        }
    )

    # 6. Create Payments
    Payment.objects.get_or_create(
        membership=m1,
        defaults={
            "student": student1,
            "amount": 1500.00,
            "status": "verified",
            "payment_mode": "UPI",
            "transaction_id": "TXN_UPI_8372917",
            "notes": "Premium membership initial payment"
        }
    )

    Payment.objects.get_or_create(
        membership=m2,
        defaults={
            "student": student2,
            "amount": 800.00,
            "status": "pending",
            "payment_mode": "Cash",
            "notes": "Cash payment verification pending"
        }
    )

    # 7. Create Seats and Assignments
    for floor in ["Ground", "First"]:
        for row in ["A", "B"]:
            for num in range(1, 6):
                seat_num = f"{num}"
                seat, _ = Seat.objects.get_or_create(
                    floor=floor,
                    row=row,
                    seat_number=seat_num,
                    defaults={
                        "status": "available"
                    }
                )
                if floor == "Ground" and row == "A" and num == 1:
                    seat.status = "occupied"
                    seat.save()
                    SeatAssignment.objects.get_or_create(
                        student=student1,
                        seat=seat,
                        released_date=None
                    )

    # 8. Create QR Code
    QRCode.objects.get_or_create(
        code="demo-qrcode-token-1234",
        defaults={
            "created_by": admin1,
            "valid_date": timezone.now().date(),
            "expiry_timestamp": timezone.now() + datetime.timedelta(hours=4),
            "is_expired": False
        }
    )

    # 9. Create Attendance
    Attendance.objects.get_or_create(
        student=student1,
        date=timezone.now().date(),
        defaults={
            "time_in": timezone.now().time(),
            "is_manual": False
        }
    )

    # 10. Global Settings
    GlobalSetting.objects.get_or_create(
        key="library_name",
        defaults={
            "value": "Shresht Library Digital",
            "description": "Display name of the library"
        }
    )

    print("Demo data seeded successfully into Supabase!")

if __name__ == '__main__':
    seed()
