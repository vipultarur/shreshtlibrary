from django.test import TestCase
from django.urls import reverse
from django.contrib.auth import get_user_model
from rest_framework.test import APIClient
from apps.seats.models import Seat, Floor, SeatRow, SeatAssignment
from apps.accounts.models import AdminUser

User = get_user_model()

class SeatManagementTestCase(TestCase):
    def setUp(self):
        self.client = APIClient()
        
        # Setup Student
        self.student = User.objects.create(username="student_seat", role="student", email="student_seat@test.com")
        self.student2 = User.objects.create(username="student_seat2", role="student", email="student_seat2@test.com")
        
        # Setup Admin
        self.admin = AdminUser.objects.create(
            username="admin_seat", 
            role="admin", 
            email="admin_seat@test.com",
            permissions={"manage_seats": True}
        )
        self.client.force_authenticate(user=self.admin)
        
        # Setup Seat
        self.floor = Floor.objects.create(name="Ground Floor")
        self.row = SeatRow.objects.create(floor=self.floor, label="A")
        self.seat = Seat.objects.create(
            row_ref=self.row,
            floor="Ground Floor",
            row="A",
            seat_number="A1",
            status="available"
        )
        
    def test_assign_seat_success(self):
        """Test assigning an available seat to a student."""
        url = f"/api/v1/admin/seats/{self.seat.id}/assign/"
        response = self.client.post(url, {
            "student_id": self.student.id
        }, format="json")
        
        self.assertEqual(response.status_code, 200)
        self.seat.refresh_from_db()
        self.assertEqual(self.seat.status, "occupied")
        
        # Verify SeatAssignment is created
        assignment = SeatAssignment.objects.filter(student=self.student, seat=self.seat, released_date__isnull=True).first()
        self.assertIsNotNone(assignment)
        
    def test_unassign_seat(self):
        """Test changing seat status back to available releases the assignment."""
        # First assign it
        self.client.post(f"/api/v1/admin/seats/{self.seat.id}/assign/", {
            "student_id": self.student.id
        }, format="json")
        
        # Then unassign
        response = self.client.post(f"/api/v1/admin/seats/{self.seat.id}/unassign/", format="json")
        
        self.assertEqual(response.status_code, 200)
        self.seat.refresh_from_db()
        self.assertEqual(self.seat.status, "available")
        
        # Verify SeatAssignment is released
        assignment = SeatAssignment.objects.filter(student=self.student, seat=self.seat).last()
        self.assertIsNotNone(assignment.released_date)

    def test_invalid_seat_status(self):
        """Test sending an invalid status returns 400 Bad Request."""
        url = f"/api/v1/admin/seats/{self.seat.id}/status/"
        response = self.client.post(url, {
            "status": "destroyed"
        }, format="json")
        self.assertEqual(response.status_code, 400)
