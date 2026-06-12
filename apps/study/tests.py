from django.test import TestCase
from django.contrib.auth import get_user_model
from rest_framework.test import APIClient
from apps.study.models import StudySession

User = get_user_model()

class StudySessionTestCase(TestCase):
    def setUp(self):
        self.client = APIClient()
        
        # Setup Student
        self.student = User.objects.create(username="student_study", role="student", email="student_study@test.com")
        self.client.force_authenticate(user=self.student)
        
    def test_start_study_session_success(self):
        """Test a student can start a new study session."""
        response = self.client.post("/api/v1/study/session/start/", format="json")
        self.assertEqual(response.status_code, 201)
        self.assertTrue(StudySession.objects.filter(student=self.student, status="starting").exists())
        
    def test_prevent_multiple_active_sessions(self):
        """Test a student cannot start a new session if one is already active."""
        # Start first session
        self.client.post("/api/v1/study/session/start/", format="json")
        
        # Attempt to start second session
        response = self.client.post("/api/v1/study/session/start/", format="json")
        
        # API should return 200 with the EXISTING session, not create a new one
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()['message'], "You already have an active study session.")
        
        # Verify only one active session exists
        active_count = StudySession.objects.filter(student=self.student, end_time__isnull=True).count()
        self.assertEqual(active_count, 1)

    def test_end_study_session(self):
        """Test ending an active study session."""
        # Start session
        self.client.post("/api/v1/study/session/start/", format="json")
        
        # End session
        response = self.client.post("/api/v1/study/session/end/", {
            "duration_minutes": 120,
            "paused_minutes": 10
        }, format="json")
        
        self.assertEqual(response.status_code, 200)
        
        session = StudySession.objects.filter(student=self.student).last()
        self.assertIsNotNone(session.end_time)
        self.assertEqual(session.status, "completed")
        self.assertEqual(session.duration_minutes, 120)
        self.assertEqual(session.paused_minutes, 10)
