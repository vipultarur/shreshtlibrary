from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import AllowAny
from django.shortcuts import get_object_or_404
from django.utils import timezone
import datetime

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.students.models import StudentProfile, ReferralCode, ReferralHistory
from apps.memberships.models import Membership
from apps.attendance.models import Attendance
from apps.seats.models import SeatAssignment
from .serializers import StudentProfileSerializer, ReferralCodeSerializer, ReferralHistorySerializer

class StudentProfileView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: StudentProfileSerializer}, tags=['Student Profile'])
    def get(self, request):
        profile = get_object_or_404(StudentProfile, user=request.user)
        serializer = StudentProfileSerializer(profile)
        return standard_response(data=serializer.data)


class StudentProfileUpdateView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=StudentProfileSerializer, responses={200: StudentProfileSerializer}, tags=['Student Profile'])
    def put(self, request):
        profile = get_object_or_404(StudentProfile, user=request.user)
        serializer = StudentProfileSerializer(profile, data=request.data, partial=True)
        if serializer.is_valid():
            serializer.save()
            return standard_response(message="Profile updated successfully.", data=serializer.data)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class StudentProfilePhotoView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Student Profile'])
    def post(self, request):
        profile = get_object_or_404(StudentProfile, user=request.user)
        photo = request.FILES.get('profile_photo')
        if not photo:
            return Response({"errors": {"profile_photo": ["No photo file provided."]}}, status=status.HTTP_400_BAD_REQUEST)
        profile.profile_photo = photo
        profile.save()
        return standard_response(message="Profile photo updated successfully.", data={"photo_url": profile.profile_photo.url})


class StudentDashboardView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Student Profile'])
    def get(self, request):
        user = request.user
        
        # Membership details
        membership = Membership.objects.filter(student=user, status='active').last()
        days_left = 0
        plan_name = "No active plan"
        if membership:
            days_left = (membership.end_date - timezone.now().date()).days
            days_left = max(0, days_left)
            plan_name = membership.plan.name

        # Seat details
        seat_assign = SeatAssignment.objects.filter(student=user, released_date__isnull=True).last()
        seat_number = seat_assign.seat.seat_number if seat_assign else "None"
        floor = seat_assign.seat.floor if seat_assign else "None"

        # Today's attendance
        attended = Attendance.objects.filter(student=user, date=timezone.now().date()).exists()

        data = {
            "student_id": user.id,
            "full_name": user.get_full_name() or user.username,
            "membership_plan": plan_name,
            "membership_days_left": days_left,
            "assigned_seat": seat_number,
            "assigned_seat_floor": floor,
            "marked_attendance_today": attended
        }
        return standard_response(data=data)


class StudentIDCardView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Student Profile'])
    def get(self, request):
        user = request.user
        profile = get_object_or_404(StudentProfile, user=user)
        data = {
            "student_id": user.id,
            "full_name": user.get_full_name() or user.username,
            "mobile": user.mobile,
            "email": user.email,
            "goal": profile.goal,
            "dob": profile.dob,
            "photo_url": profile.profile_photo.url if profile.profile_photo else None,
            "qr_data": f"shresht-student-{user.id}"
        }
        return standard_response(data=data)


class ReferralCodeView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: ReferralCodeSerializer}, tags=['Student Profile'])
    def get(self, request):
        ref = get_object_or_404(ReferralCode, student=request.user)
        return standard_response(data=ReferralCodeSerializer(ref).data)


class ReferralApplyView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=ReferralCodeSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Student Profile'])
    def post(self, request):
        code = request.data.get('code')
        if not code:
            return Response({"errors": {"code": ["Code is required."]}}, status=status.HTTP_400_BAD_REQUEST)
        try:
            ref_code = ReferralCode.objects.get(code=code)
            return standard_response(message="Referral code valid.", data={"referrer": ref_code.student.get_full_name()})
        except ReferralCode.DoesNotExist:
            return Response({"errors": {"code": ["Invalid referral code."]}}, status=status.HTTP_400_BAD_REQUEST)


class ReferralHistoryView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: ReferralHistorySerializer(many=True)}, tags=['Student Profile'])
    def get(self, request):
        history = ReferralHistory.objects.filter(referrer=request.user)
        serializer = ReferralHistorySerializer(history, many=True)
        return standard_response(data=serializer.data)
