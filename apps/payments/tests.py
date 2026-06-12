from django.test import TestCase
from django.utils import timezone
from django.contrib.auth import get_user_model
from rest_framework.test import APIClient
from apps.payments.models import Payment
from apps.memberships.models import Membership, MembershipPlan
from apps.accounts.models import AdminUser

User = get_user_model()

class PaymentVerificationTestCase(TestCase):
    def setUp(self):
        self.client = APIClient()
        
        # Setup Student
        self.student = User.objects.create(username="student1", role="student", email="student1@test.com")
        
        # Setup Admin
        self.admin = AdminUser.objects.create(
            username="admin1", 
            role="admin", 
            email="admin@test.com",
            permissions={"manage_payments": True}
        )
        self.client.force_authenticate(user=self.admin)
        
        # Setup Membership Plan
        self.plan = MembershipPlan.objects.create(
            name="Premium Plan",
            duration_months=1,
            price=1500.00
        )
        
        # Setup Pending Membership and Payment
        self.membership = Membership.objects.create(
            student=self.student,
            plan=self.plan,
            start_date=timezone.now().date(),
            end_date=timezone.now().date() + timezone.timedelta(days=30),
            status="pending",
            is_active=False
        )
        
        self.payment = Payment.objects.create(
            student=self.student,
            membership=self.membership,
            amount=1500.00,
            status="pending",
            payment_mode="UPI"
        )
        
    def test_verify_payment_updates_membership_atomically(self):
        """Test that verifying a payment also activates the membership atomically."""
        url = f"/api/v1/admin/payments/{self.payment.id}/verify/"
        response = self.client.post(url, format="json")
        
        self.assertEqual(response.status_code, 200)
        
        # Reload from DB
        self.payment.refresh_from_db()
        self.membership.refresh_from_db()
        
        # Assert Payment state
        self.assertEqual(self.payment.status, "verified")
        self.assertIsNotNone(self.payment.verified_at)
        
        # Assert Side Effect: Membership state
        self.assertEqual(self.membership.status, "active")
        self.assertTrue(self.membership.is_active)
        
    def test_verify_payment_not_found(self):
        """Test 404 response for invalid payment ID."""
        url = "/api/v1/admin/payments/999999/verify/"
        response = self.client.post(url, format="json")
        self.assertEqual(response.status_code, 404)
