import datetime
from django.utils import timezone
from django.urls import reverse
from django.contrib.auth import get_user_model
from rest_framework.test import APITestCase
from rest_framework import status

from apps.accounts.models import CustomUser
from apps.students.models import StudentProfile
from apps.seats.models import Seat, SeatAssignment
from apps.attendance.models import QRCode, Attendance
from apps.memberships.models import MembershipPlan, Membership

User = get_user_model()

class ShreshtLibraryAPITestCase(APITestCase):

    def setUp(self):
        # Create a standard membership plan
        self.plan = MembershipPlan.objects.create(
            name="1 Month Plan",
            duration_months=1,
            price=1000.00,
            is_active=True
        )

        # Create student user
        self.student_user = User.objects.create_user(
            username="student1",
            email="student1@shresht.com",
            mobile="9876543210",
            role="student",
            password="securepassword123"
        )
        # Create student profile
        self.student_profile = StudentProfile.objects.create(
            user=self.student_user,
            goal="UPSC",
            dob=datetime.date(2000, 1, 1),
            parent_mobile="9999999999"
        )

        # Create admin user
        from apps.accounts.models import AdminUser
        self.admin_user = AdminUser(
            username="admin1",
            email="admin1@shresht.com",
            mobile="8888888888",
            role="admin",
            permissions={"manage_seats": True, "manage_students": True}
        )
        self.admin_user.set_password("adminpassword123")
        self.admin_user.save()

        # Create active membership for student
        self.membership = Membership.objects.create(
            student=self.student_user,
            plan=self.plan,
            start_date=timezone.now().date() - datetime.timedelta(days=1),
            end_date=timezone.now().date() + datetime.timedelta(days=29),
            status="active"
        )

        # Create a seat
        self.seat = Seat.objects.create(
            floor="Ground",
            row="A",
            seat_number="1",
            status="available"
        )

        # Create active QRCode
        self.qr_code = QRCode.objects.create(
            code="mock-active-qr-token",
            created_by=self.admin_user,
            valid_date=timezone.now().date(),
            expiry_timestamp=timezone.now() + datetime.timedelta(hours=2),
            is_expired=False
        )

    def get_jwt_token(self, username, password):
        from apps.accounts.models import AdminUser
        try:
            user = User.objects.get(username=username)
            role = user.role
            email = user.email
        except User.DoesNotExist:
            user = AdminUser.objects.get(username=username)
            role = user.role
            email = user.email

        if role in ['admin', 'super_admin']:
            url = reverse('auth-admin-login-legacy')
            data = {
                "username": username,
                "password": password
            }
        else:
            url = reverse('auth-login-email')
            data = {
                "email": email,
                "password": password
            }
        response = self.client.post(url, data, format='json')
        return response.data['data']['tokens']['access']

    def test_student_registration(self):
        url = reverse('auth-register')
        data = {
            "first_name": "New",
            "last_name": "Student",
            "email": "newstudent@shresht.com",
            "mobile": "9998887776",
            "password": "NewStudentPassword123!",
            "confirm_password": "NewStudentPassword123!",
            "goal": "GPSC",
            "dob": "1998-05-15",
            "caste": "General",
            "address": "123 Street",
            "parent_mobile": "9998887770"
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['status'], 'success')
        self.assertIn('tokens', response.data['data'])
        self.assertIn('user', response.data['data'])
        self.assertEqual(response.data['data']['user']['username'], '9998887776')

    def test_verify_otp_flow(self):
        # Set OTP for student
        self.student_user.otp = "654321"
        self.student_user.otp_expiry = timezone.now() + datetime.timedelta(minutes=5)
        self.student_user.save()

        url = reverse('auth-verify-otp')
        data = {
            "mobile": self.student_user.mobile,
            "otp": "654321"
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], 'success')
        self.assertIn('tokens', response.data['data'])

    def test_seat_assignment_by_admin(self):
        token = self.get_jwt_token("admin1", "adminpassword123")
        self.client.credentials(HTTP_AUTHORIZATION=f'Bearer {token}')

        url = reverse('admin-seat-assign', kwargs={'pk': self.seat.id})
        data = {
            "student_id": self.student_user.id
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        
        # Verify seat status changed to occupied
        self.seat.refresh_from_db()
        self.assertEqual(self.seat.status, "occupied")

        # Verify seat assignment record
        assignment = SeatAssignment.objects.filter(student=self.student_user, released_date__isnull=True).first()
        self.assertIsNotNone(assignment)
        self.assertEqual(assignment.seat, self.seat)

    def test_qr_attendance_scan_successful(self):
        token = self.get_jwt_token("student1", "securepassword123")
        self.client.credentials(HTTP_AUTHORIZATION=f'Bearer {token}')

        url = reverse('attendance-scan')
        data = {
            "code": "mock-active-qr-token"
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertEqual(response.data['status'], 'success')

        # Verify attendance record created
        att = Attendance.objects.filter(student=self.student_user, date=timezone.now().date()).exists()
        self.assertTrue(att)

    def test_qr_attendance_scan_duplicate(self):
        token = self.get_jwt_token("student1", "securepassword123")
        self.client.credentials(HTTP_AUTHORIZATION=f'Bearer {token}')

        # Pre-mark attendance
        Attendance.objects.create(
            student=self.student_user,
            date=timezone.now().date(),
            qr_code=self.qr_code
        )

        url = reverse('attendance-scan')
        data = {
            "code": "mock-active-qr-token"
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertEqual(response.data['status'], 'error')
        self.assertEqual(response.data['message'], "You have already marked attendance today.")

    def test_qr_attendance_scan_expired_qr(self):
        token = self.get_jwt_token("student1", "securepassword123")
        self.client.credentials(HTTP_AUTHORIZATION=f'Bearer {token}')

        # Expire QR code
        self.qr_code.is_expired = True
        self.qr_code.save()

        url = reverse('attendance-scan')
        data = {
            "code": "mock-active-qr-token"
        }
        response = self.client.post(url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertEqual(response.data['status'], 'error')
        self.assertEqual(response.data['message'], "Invalid QR code.")

    def test_standard_response_errors(self):
        # Call a details view with invalid pk to test custom exception handler structure
        token = self.get_jwt_token("admin1", "adminpassword123")
        self.client.credentials(HTTP_AUTHORIZATION=f'Bearer {token}')

        url = reverse('admin-student-detail', kwargs={'pk': 99999})
        response = self.client.get(url)
        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)
        self.assertEqual(response.data['status'], 'error')
        self.assertIn('message', response.data)
        self.assertIn('errors', response.data)
