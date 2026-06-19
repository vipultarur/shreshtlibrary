import uuid
import datetime
from django.db.models import Q
from django.utils import timezone
from django.shortcuts import get_object_or_404
from rest_framework import generics, status
from rest_framework.views import APIView
from rest_framework.permissions import IsAuthenticated
from django_filters.rest_framework import DjangoFilterBackend
from django.contrib.auth import get_user_model

from apps.attendance.models import Attendance, Holiday, QRCode
from apps.students.models import StudentProfile
from shreshtlibrary.utils.permissions import HasAdminPermission
from api.v1.admin.pagination import AdminStandardPagination
from api.v1.admin.serializers import AttendanceSerializer, HolidaySerializer, QRCodeSerializer, StudentProfileSerializer
from utils.response import standard_response
from api.v1.v2_admin import _activity, _admin_user, _date, _holiday_for_date

User = get_user_model()

def _generate_qr(request, method="MANUAL", validity_days=1):
    QRCode.objects.filter(is_active=True).update(is_active=False, is_expired=True)
    now = timezone.now()
    qr = QRCode.objects.create(
        token=uuid.uuid4(),
        code=f"library-qr-{now.date()}-{uuid.uuid4()}",
        valid_date=now.date(),
        expiry_timestamp=now + datetime.timedelta(days=validity_days),
        expires_at=now + datetime.timedelta(days=validity_days),
        is_active=True,
        is_expired=False,
        generation_method=method,
        created_by=_admin_user(request),
    )
    _activity(request, "GENERATE_QR", "QRCode", qr.id, f"Generated QR code valid for {validity_days} days")
    return qr

class AdminQRView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def get(self, request, action=None, pk=None):
        if action == "history":
            qs = QRCode.objects.all().order_by("-created_at")
            paginator = AdminStandardPagination()
            paginated_qs = paginator.paginate_queryset(qs, request)
            return paginator.get_paginated_response(QRCodeSerializer(paginated_qs, many=True).data)
            
        if action == "scans":
            qs = Attendance.objects.filter(qr_code_id=pk)
            return standard_response(data=AttendanceSerializer(qs, many=True).data)
            
        qr = QRCode.objects.filter(is_active=True).order_by("-created_at").first()
        return standard_response(data=QRCodeSerializer(qr).data if qr else None)

    def post(self, request, action=None):
        if action in ["generate", "regenerate"]:
            try:
                validity_days = int(request.data.get("validity_days", 1))
            except ValueError:
                validity_days = 1
            if validity_days < 1 or validity_days > 7:
                validity_days = 1
            qr = _generate_qr(request, validity_days=validity_days)
            return standard_response(data=QRCodeSerializer(qr).data, status_code=201)
            
        if action == "expire":
            QRCode.objects.filter(is_active=True).update(is_active=False, is_expired=True)
            return standard_response(message="Current QR expired.")
            
        return standard_response("error", "Unknown QR action.", status_code=404)


class AdminAttendanceView(generics.ListCreateAPIView):
    permission_classes = [HasAdminPermission("manage_attendance")]
    serializer_class = AttendanceSerializer
    pagination_class = AdminStandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['student_id', 'date', 'method']

    def get_queryset(self):
        qs = Attendance.objects.select_related("student", "student__student_profile").all().order_by("-date", "-marked_at")
        qs = qs.exclude(student__student_profile__status__in=['EXPIRED', 'SUSPENDED'])
        from_date = self.request.query_params.get("from_date")
        to_date = self.request.query_params.get("to_date")
        if from_date:
            qs = qs.filter(date__gte=from_date)
        if to_date:
            qs = qs.filter(date__lte=to_date)
        return qs

    def create(self, request, *args, **kwargs):
        student = None
        if request.data.get("student_id"):
            student = get_object_or_404(User, id=request.data["student_id"], role="student")
        elif request.data.get("student_mobile"):
            student = get_object_or_404(User, mobile=request.data["student_mobile"], role="student")
            
        if not student:
            return standard_response("error", "Student is required.", status_code=400)
            
        try:
            if student.student_profile.status in ['EXPIRED', 'SUSPENDED']:
                return standard_response("error", "Cannot mark attendance for suspended/expired students.", status_code=400)
        except:
            pass
            
        date = _date(request.data.get("date"), timezone.now().date())
        holiday = _holiday_for_date(date)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=HolidaySerializer(holiday).data, status_code=400)
            
        record, _ = Attendance.objects.update_or_create(
            student=student,
            date=date,
            defaults={
                "is_present": request.data.get("is_present", True),
                "is_manual": True,
                "method": "MANUAL",
                "marked_by": _admin_user(request),
                "note": request.data.get("note"),
            },
        )
        _activity(request, "MANUAL_ATTENDANCE", "Attendance", record.id, f"Manual attendance for {student.username}")
        return standard_response(data=self.get_serializer(record).data, status_code=201)


class AdminAttendanceDetailView(generics.RetrieveUpdateDestroyAPIView):
    permission_classes = [HasAdminPermission("manage_attendance")]
    serializer_class = AttendanceSerializer
    queryset = Attendance.objects.all()

    def update(self, request, *args, **kwargs):
        record = self.get_object()
        try:
            if record.student.student_profile.status in ['EXPIRED', 'SUSPENDED']:
                return standard_response("error", "Cannot update attendance for suspended/expired students.", status_code=400)
        except:
            pass
            
        target_date = _date(request.data.get("date"), record.date)
        holiday = _holiday_for_date(target_date)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=HolidaySerializer(holiday).data, status_code=400)
            
        serializer = self.get_serializer(record, data=request.data, partial=True)
        if serializer.is_valid():
            if "date" in request.data:
                serializer.validated_data["date"] = target_date
            record = serializer.save()
            return standard_response(data=self.get_serializer(record).data)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)

    def destroy(self, request, *args, **kwargs):
        record = self.get_object()
        try:
            if record.student.student_profile.status in ['EXPIRED', 'SUSPENDED']:
                return standard_response("error", "Cannot delete attendance for suspended/expired students.", status_code=400)
        except:
            pass
        record.delete()
        return standard_response(message="Attendance deleted.")


class AdminAttendanceSummaryView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def get(self, request, kind):
        date = _date(request.query_params.get("date"), timezone.now().date())
        present_students = Attendance.objects.filter(date=date, is_present=True).values_list("student_id", flat=True)
        
        is_pending_period = False
        if date == timezone.now().date():
            from core.models import GlobalSetting
            from apps.library.models import LibraryInfo
            lib_info = LibraryInfo.objects.first()
            open_time_str = lib_info.open_time.strftime('%H:%M') if lib_info and lib_info.open_time else "08:00"
            padding_str = GlobalSetting.objects.filter(key="attendance_padding_time").values_list("value", flat=True).first() or "60"
            try:
                open_h, open_m = map(int, open_time_str.split(':'))
                padding = int(padding_str)
                now = timezone.now()
                open_dt = timezone.datetime.combine(now.date(), timezone.datetime.min.time().replace(hour=open_h, minute=open_m))
                open_dt = timezone.make_aware(open_dt, timezone.get_current_timezone())
                if now <= open_dt + timezone.timedelta(minutes=padding):
                    is_pending_period = True
            except:
                pass

        if kind == "daily-summary":
            total = StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).count()
            present = len(set(present_students))
            pending_count = max(total - present, 0) if is_pending_period else 0
            absent_count = 0 if is_pending_period else max(total - present, 0)
            return standard_response(data={"date": date, "present": present, "absent": absent_count, "pending": pending_count, "total": total})
            
        if kind == "absentees":
            qs = StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).exclude(user_id__in=present_students).select_related("user")
            res = StudentProfileSerializer(qs, many=True).data
            for item in res:
                item["attendance_status"] = "pending" if is_pending_period else "absent"
            return standard_response(data=res)
            
        from django.db.models import Count, Q
        profiles = StudentProfile.objects.exclude(
            status__in=['EXPIRED', 'SUSPENDED']
        ).select_related("user").annotate(
            streak=Count('user__attendances', filter=Q(user__attendances__is_present=True))
        ).order_by('-streak')[:20]

        streaks = []
        for profile in profiles:
            streaks.append({"student": StudentProfileSerializer(profile).data, "streak": profile.streak})
            
        return standard_response(data=streaks)
