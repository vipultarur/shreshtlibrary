from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.contrib.auth import get_user_model
from django.utils import timezone
import datetime

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsLibraryAdmin
from utils.response import standard_response
from utils.qr_generator import generate_qr_token
from apps.accounts.models import AdminUser
from apps.students.models import StudentProfile
from apps.attendance.models import QRCode, Attendance
from apps.memberships.models import MembershipPlan, Membership
from apps.payments.models import Payment
from apps.seats.models import Seat, SeatAssignment
from apps.library.models import Review
from apps.notifications.models import Notification, StudentNotification, DeviceToken
from utils.fcm import send_push_notification

from .serializers import AdminProfileSerializer, AdminStudentProfileUpdateSerializer, AdminAddSeatSerializer, AdminManualAttendanceSerializer
from api.v1.students.serializers import StudentProfileSerializer
from api.v1.attendance.serializers import QRCodeSerializer, AttendanceSerializer
from api.v1.memberships.serializers import MembershipPlanSerializer
from api.v1.library.serializers import ReviewSerializer
from api.v1.notifications.serializers import NotificationSerializer

User = get_user_model()

class AdminDashboardStatsView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Admin Panel'])
    def get(self, request):
        today = timezone.now().date()
        total_students = User.objects.filter(role='student').count()
        active_members = Membership.objects.filter(status='active', end_date__gte=today).values('student').distinct().count()
        today_attendance = Attendance.objects.filter(date=today).count()
        total_seats = Seat.objects.count()
        occupied_seats = Seat.objects.filter(status='occupied').count()

        data = {
            "total_registered_students": total_students,
            "active_memberships": active_members,
            "today_attendance_count": today_attendance,
            "total_seats": total_seats,
            "occupied_seats": occupied_seats,
            "available_seats": total_seats - occupied_seats
        }
        return standard_response(data=data)


class AdminStudentsListView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: StudentProfileSerializer(many=True)}, tags=['Admin Panel'])
    def get(self, request):
        students = StudentProfile.objects.all()
        serializer = StudentProfileSerializer(students, many=True)
        return standard_response(data=serializer.data)


class AdminStudentProfileView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: StudentProfileSerializer}, tags=['Admin Panel'])
    def get(self, request, pk):
        student = get_object_or_404(StudentProfile, user__id=pk)
        serializer = StudentProfileSerializer(student)
        return standard_response(data=serializer.data)

    @extend_schema(request=AdminStudentProfileUpdateSerializer, responses={200: StudentProfileSerializer}, tags=['Admin Panel'])
    def put(self, request, pk):
        student = get_object_or_404(StudentProfile, user__id=pk)
        serializer = AdminStudentProfileUpdateSerializer(student, data=request.data, partial=True)
        if serializer.is_valid():
            serializer.save()
            return standard_response(message="Student profile updated by admin.", data=StudentProfileSerializer(student).data)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class AdminVerifyPaymentView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Admin Panel'])
    def post(self, request, pk):
        payment = get_object_or_404(Payment, id=pk)
        payment.status = 'verified'
        payment.save()

        # Activate membership
        if payment.membership:
            payment.membership.status = 'active'
            payment.membership.save()

        return standard_response(message="Payment verified and membership activated.")


class AdminSeatStatusUpdateView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(
        request=OpenApiTypes.OBJECT,
        responses={200: OpenApiTypes.OBJECT},
        tags=['Admin Panel']
    )
    def post(self, request, pk):
        seat = get_object_or_404(Seat, id=pk)
        new_status = request.data.get('status')
        student_id = request.data.get('student_id')

        if new_status not in ['available', 'occupied', 'reserved']:
            return Response({"errors": {"status": ["Invalid status value."]}}, status=status.HTTP_400_BAD_REQUEST)

        seat.status = new_status
        seat.save()

        # Handle seat assignment tracking
        if new_status == 'occupied' and student_id:
            student = get_object_or_404(User, id=student_id, role='student')
            # Release old seat assignments
            SeatAssignment.objects.filter(student=student, released_date__isnull=True).update(released_date=timezone.now().date())
            # Create new assignment
            SeatAssignment.objects.create(student=student, seat=seat)
        elif new_status == 'available':
            # Release assignments linked to this seat
            SeatAssignment.objects.filter(seat=seat, released_date__isnull=True).update(released_date=timezone.now().date())

        return standard_response(message=f"Seat status updated to '{new_status}' successfully.")


class AdminAddSeatView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(request=AdminAddSeatSerializer, responses={201: OpenApiTypes.OBJECT}, tags=['Admin Panel'])
    def post(self, request):
        serializer = AdminAddSeatSerializer(data=request.data)
        if serializer.is_valid():
            serializer.save()
            return standard_response(message="New study desk added to the library layout.", data=serializer.data, status_code=status.HTTP_201_CREATED)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class AdminQRCodeGenerateView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={201: QRCodeSerializer}, tags=['Admin Panel'])
    def post(self, request):
        code_token = generate_qr_token()
        now = timezone.now()
        qr = QRCode.objects.create(
            code=code_token,
            created_by=request.user,
            valid_date=now.date(),
            expiry_timestamp=now + datetime.timedelta(seconds=60), # Valid for 60 seconds
            is_expired=False
        )
        return standard_response(
            message="New secure QR code generated.",
            data=QRCodeSerializer(qr).data,
            status_code=status.HTTP_201_CREATED
        )


class AdminManualAttendanceView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(request=AdminManualAttendanceSerializer, responses={201: AttendanceSerializer}, tags=['Admin Panel'])
    def post(self, request):
        serializer = AdminManualAttendanceSerializer(data=request.data)
        if serializer.is_valid():
            student_id = serializer.validated_data.get('student_id')
            student_mobile = serializer.validated_data.get('student_mobile')
            
            student = None
            if student_id:
                student = User.objects.filter(id=student_id, role='student').first()
            elif student_mobile:
                student = User.objects.filter(mobile=student_mobile, role='student').first()
                
            if not student:
                return Response({"errors": {"student_id": ["Student not found."]}}, status=status.HTTP_400_BAD_REQUEST)
                
            date = serializer.validated_data.get('date', timezone.now().date())
            
            if Attendance.objects.filter(student=student, date=date).exists():
                return Response({"errors": {"student_id": ["Attendance already marked for this date."]}}, status=status.HTTP_400_BAD_REQUEST)

            attendance = Attendance.objects.create(
                student=student,
                date=date,
                is_manual=True
            )
            return standard_response(message="Manual attendance logged by admin.", data=AttendanceSerializer(attendance).data, status_code=status.HTTP_201_CREATED)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class AdminUpdateMembershipPlanView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(request=MembershipPlanSerializer, responses={200: MembershipPlanSerializer}, tags=['Admin Panel'])
    def put(self, request, pk):
        plan = get_object_or_404(MembershipPlan, id=pk)
        serializer = MembershipPlanSerializer(plan, data=request.data, partial=True)
        if serializer.is_valid():
            serializer.save()
            return standard_response(message="Membership plan updated.", data=serializer.data)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class AdminReviewsListView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: ReviewSerializer(many=True)}, tags=['Admin Panel'])
    def get(self, request):
        reviews = Review.objects.all().order_by('-created_at')
        serializer = ReviewSerializer(reviews, many=True)
        return standard_response(data=serializer.data)


class AdminApproveReviewView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Admin Panel'])
    def post(self, request, pk):
        review = get_object_or_404(Review, id=pk)
        review.is_approved = True
        review.save()
        return standard_response(message="Review approved for public display.")


class AdminSendNotificationView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(request=NotificationSerializer, responses={201: OpenApiTypes.OBJECT}, tags=['Admin Panel'])
    def post(self, request):
        serializer = NotificationSerializer(data=request.data)
        if serializer.is_valid():
            title = serializer.validated_data['title']
            body = serializer.validated_data['body']
            notif_type = request.data.get('type', 'push')
            target = request.data.get('target_group', 'all')

            notification = Notification.objects.create(
                title=title,
                body=body,
                type=notif_type,
                target_group=target
            )

            # Filter targets
            students = User.objects.filter(role='student')
            if target == 'active':
                students = students.filter(memberships__status='active', memberships__end_date__gte=timezone.now().date()).distinct()
            elif target == 'expired':
                students = students.exclude(memberships__status='active', memberships__end_date__gte=timezone.now().date()).distinct()

            # Delivery
            for student in students:
                StudentNotification.objects.create(
                    student=student,
                    notification=notification
                )
                
                # Push Notification Dispatch
                if notif_type in ['push', 'all']:
                    tokens = DeviceToken.objects.filter(student=student)
                    for token_obj in tokens:
                        send_push_notification(token_obj.token, title, body)

            return standard_response(message="Broadcasting notification dispatched to targeted student segments.", status_code=status.HTTP_201_CREATED)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)
