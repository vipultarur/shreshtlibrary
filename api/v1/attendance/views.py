from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.utils import timezone

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.attendance.models import Attendance, QRCode
from apps.memberships.models import Membership
from .serializers import AttendanceSerializer, QRCodeSerializer

class StudentScanQRView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(request=QRCodeSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['QR Attendance'])
    def post(self, request):
        code = request.data.get('code')
        if not code:
            return standard_response("error", "Code is required.", status_code=status.HTTP_400_BAD_REQUEST)
        
        user = request.user
        
        # Check active membership
        membership = Membership.objects.filter(student=user, status='active').last()
        if not membership or membership.end_date < timezone.now().date():
            return standard_response("error", "Access denied. Active membership required to check in.", status_code=status.HTTP_403_FORBIDDEN)

        # Validate QR code exists, valid date, not expired
        try:
            qr = QRCode.objects.get(code=code)
            if qr.is_expired or qr.valid_date != timezone.now().date() or qr.expiry_timestamp < timezone.now():
                return standard_response("error", "Invalid or expired QR code.", status_code=status.HTTP_400_BAD_REQUEST)
        except QRCode.DoesNotExist:
            return standard_response("error", "Invalid QR code.", status_code=status.HTTP_400_BAD_REQUEST)

        from django.db import IntegrityError
        
        # Check if already marked attendance today
        today = timezone.now().date()
        if Attendance.objects.filter(student=user, date=today).exists():
            return standard_response("error", "Attendance already marked for today.", status_code=status.HTTP_400_BAD_REQUEST)

        # Create attendance record
        try:
            attendance = Attendance.objects.create(student=user, date=today, qr_code=qr, is_manual=False, method='QR')
        except IntegrityError:
            return standard_response("error", "Attendance already marked for today.", status_code=status.HTTP_400_BAD_REQUEST)
            
        return standard_response(message="Attendance marked successfully.", data=AttendanceSerializer(attendance).data)


class StudentAttendanceLogsView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: AttendanceSerializer(many=True)}, tags=['QR Attendance'])
    def get(self, request):
        logs = Attendance.objects.filter(student=request.user).order_by('-date')
        serializer = AttendanceSerializer(logs, many=True)
        return standard_response(data=serializer.data)
