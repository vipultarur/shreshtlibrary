from rest_framework import serializers
import datetime
from django.db.models import Q
from django.utils import timezone
from rest_framework import generics, parsers, status
from rest_framework.response import Response
from rest_framework.views import APIView
from django_filters.rest_framework import DjangoFilterBackend
from django.shortcuts import get_object_or_404
from django.contrib.auth import get_user_model

from apps.students.models import StudentProfile
from apps.study.models import StudySession
from apps.attendance.models import Attendance
from apps.payments.models import Payment
from core.models import ActivityLog
from shreshtlibrary.utils.permissions import HasAdminPermission
from api.v1.admin.pagination import AdminStandardPagination
from api.v1.admin.serializers import StudentProfileSerializer, PaymentSerializer, AttendanceSerializer
from utils.response import standard_response
from api.v1.v2_admin import _activity, _admin_user, _image_upload

User = get_user_model()

def get_student_profile(pk_val, select_user=False):
    qs = StudentProfile.objects.select_related("user") if select_user else StudentProfile.objects.all()
    pk_val = str(pk_val)
    if pk_val.isdigit():
        profile = qs.filter(user_id=pk_val).first()
        if profile:
            return profile
        return get_object_or_404(qs, pk=pk_val)
    return get_object_or_404(qs, student_id__iexact=pk_val)

class AdminStudentsView(generics.ListCreateAPIView):
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [parsers.JSONParser, parsers.MultiPartParser, parsers.FormParser]
    pagination_class = AdminStandardPagination
    serializer_class = StudentProfileSerializer
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['status', 'goal', 'gender']

    def get_queryset(self):
        qs = StudentProfile.objects.select_related("user").order_by("-created_at", "-id")
        search = self.request.query_params.get("search")
        if search:
            qs = qs.filter(
                Q(student_id__icontains=search) |
                Q(user__first_name__icontains=search) |
                Q(user__last_name__icontains=search) |
                Q(user__email__icontains=search) |
                Q(user__mobile__icontains=search)
            )
        
        created_from = self.request.query_params.get("created_from")
        created_to = self.request.query_params.get("created_to")
        if created_from:
            qs = qs.filter(created_at__date__gte=created_from)
        if created_to:
            qs = qs.filter(created_at__date__lte=created_to)
        return qs

    def create(self, request, *args, **kwargs):
        payload = request.data
        mobile = payload.get("mobile")
        email = payload.get("email")
        password = payload.get("password") or mobile or "studentpassword123"
        
        if not mobile:
            return standard_response("error", "Mobile is required.", errors={"mobile": ["This field is required."]}, status_code=400)
            
        username = payload.get("username") or mobile
        if User.objects.filter(Q(username=username) | Q(mobile=mobile)).exists():
            return standard_response("error", "User with this mobile number already exists.", errors={"mobile": ["A student with this mobile number already exists."]}, status_code=400)
            
        user = User.objects.create_user(
            username=payload.get("username") or mobile,
            email=email,
            mobile=mobile,
            first_name=payload.get("first_name", ""),
            last_name=payload.get("last_name", ""),
            role="student",
            password=password,
        )
        
        profile = StudentProfile.objects.create(
            user=user,
            middle_name=payload.get("middle_name"),
            goal=payload.get("goal", "Other"),
            dob=payload.get("dob") or payload.get("date_of_birth") or None,
            gender=payload.get("gender", "Other"),
            caste=payload.get("caste"),
            address=payload.get("address"),
            profile_photo=_image_upload(request, "profile_photo", "profile_image", "image"),
            parent_mobile=payload.get("parent_mobile"),
            preferred_language=payload.get("preferred_language", "en"),
        )
        
        _activity(request, "ADD_STUDENT", "StudentProfile", profile.id, f"Created student {profile.student_id}")
        
        try:
            from apps.notifications.models import AdminInboxNotification
            from api.v1.v2_admin import _admin_user
            creator = _admin_user(request)
            creator_name = creator.username if creator else "Admin/Keeper"
            AdminInboxNotification.objects.create(
                type='NEW_STUDENT',
                title='New Student Added Manually',
                message=f"Student {user.username} was added by {creator_name}.",
                related_id=str(user.id),
                student=user
            )
        except Exception:
            pass

        return standard_response(message="Student created successfully.", data=self.get_serializer(profile).data, status_code=201)


class AdminStudentDetailView(generics.RetrieveUpdateDestroyAPIView):
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [parsers.JSONParser, parsers.MultiPartParser, parsers.FormParser]
    serializer_class = StudentProfileSerializer

    def get_object(self):
        return get_student_profile(self.kwargs['pk'], select_user=True)

    def retrieve(self, request, *args, **kwargs):
        instance = self.get_object()
        return standard_response(data=self.get_serializer(instance).data)

    def update(self, request, *args, **kwargs):
        profile = self.get_object()
        user = profile.user
        
        for field in ["first_name", "last_name", "email", "mobile"]:
            if field in request.data:
                setattr(user, field, request.data[field])
        if "is_active" in request.data:
            user.is_active = str(request.data["is_active"]).lower() in ["true", "1", "yes"]
        user.save()
        
        old_status = profile.status
        for field in ["middle_name", "goal", "dob", "gender", "caste", "address", "parent_mobile", "status", "preferred_language"]:
            if field in request.data:
                val = request.data[field]
                if field == "dob" and val == "":
                    val = None
                setattr(profile, field, val)
        if "date_of_birth" in request.data:
            val = request.data["date_of_birth"]
            profile.dob = None if val == "" else val
            
        image = _image_upload(request, "profile_photo", "profile_image", "image")
        if image:
            profile.profile_photo = image
            
        profile.save()
        
        if old_status not in ["EXPIRED", "SUSPENDED"] and profile.status in ["EXPIRED", "SUSPENDED"]:
            from apps.seats.models import Seat, SeatAssignment, SeatChangeLog
            seat = Seat.objects.filter(student=user).first()
            if seat:
                seat.student = None
                seat.status = 'available'
                seat.save()
                SeatAssignment.objects.filter(student=user, seat=seat, released_date__isnull=True).update(released_date=timezone.now().date())
                SeatChangeLog.objects.create(seat=seat, student=user, action="UNASSIGNED", changed_by=_admin_user(request), reason=f"Student status changed to {profile.status}")

        _activity(request, "EDIT_STUDENT", "StudentProfile", profile.id, f"Updated student {profile.student_id}")
        return standard_response(message="Student updated successfully.", data=self.get_serializer(profile).data)

    def destroy(self, request, *args, **kwargs):
        from apps.accounts.models import AdminUser
        if not isinstance(request.user, AdminUser) or request.user.role != "super_admin":
            return standard_response("error", "Super admin access required.", status_code=403)
            
        profile = self.get_object()
        user = profile.user
        _activity(request, "DELETE_STUDENT", "StudentProfile", profile.id, f"Deleted student {profile.student_id}")
        user.delete()
        return standard_response(message="Student deleted successfully.")


class AdminStudentPhotoView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [parsers.MultiPartParser, parsers.FormParser]

    def _upload(self, request, pk):
        profile = get_student_profile(pk)
        image = _image_upload(request, "profile_photo", "profile_image", "image")
        if not image:
            return standard_response(
                "error",
                "Profile image is required.",
                errors={"profile_photo": ["Upload an image file."]},
                status_code=400,
            )
        profile.profile_photo = image
        profile.save()
        _activity(request, "UPLOAD_STUDENT_PHOTO", "StudentProfile", profile.id, f"Updated photo for {profile.student_id}")
        return standard_response(message="Profile image uploaded successfully.", data=StudentProfileSerializer(profile, context={'request': request}).data)

    def post(self, request, pk): return self._upload(request, pk)
    def put(self, request, pk): return self._upload(request, pk)


class AdminStudentStatusView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [HasAdminPermission("manage_students")]

    def post(self, request, pk, action):
        import threading
        profile = get_student_profile(pk)
        
        if action == "suspend":
            profile.status = "SUSPENDED"
            profile.suspension_reason = request.data.get("reason") or request.data.get("suspension_reason")
            profile.suspended_at = timezone.now()
            profile.suspended_by = _admin_user(request)
            event = "SUSPEND_STUDENT"
            self._unassign_seat(profile.user, request)
            
            def send_suspend_email(prof):
                try:
                    from utils.emails import send_transactional_email
                    send_transactional_email("SUSPEND_STUDENT", prof)
                except Exception as e:
                    print(f"Error sending suspend email: {e}")
            
            threading.Thread(target=send_suspend_email, args=(profile,)).start()

        else:
            profile.status = "LIVE"
            profile.suspension_reason = None
            profile.suspended_at = None
            profile.suspended_by = None
            event = "ACTIVATE_STUDENT"
            
            def send_activate_email(prof):
                try:
                    from utils.emails import send_transactional_email
                    send_transactional_email("ACTIVATE_STUDENT", prof)
                except Exception as e:
                    print(f"Error sending reactivate email: {e}")
            
            threading.Thread(target=send_activate_email, args=(profile,)).start()

        profile.save()
        _activity(request, event, "StudentProfile", profile.id, f"{event} {profile.student_id}")
        return standard_response(data=StudentProfileSerializer(profile, context={'request': request}).data)

    def _unassign_seat(self, student_user, request):
        from apps.seats.models import Seat, SeatAssignment, SeatChangeLog
        seat = Seat.objects.filter(student=student_user).first()
        if seat:
            seat.student = None
            seat.status = 'available'
            seat.save()
            SeatAssignment.objects.filter(student=student_user, seat=seat, released_date__isnull=True).update(released_date=timezone.now().date())
            SeatChangeLog.objects.create(seat=seat, student=student_user, action="UNASSIGNED", changed_by=_admin_user(request), reason="Student status changed to Suspended")



class AdminStudentRelatedView(APIView):
    from rest_framework import serializers
    permission_classes = [HasAdminPermission("manage_students")]
    serializer_class = serializers.Serializer

    def get(self, request, pk, kind):
        profile = get_student_profile(pk)
        if kind == "timeline":
            activities = ActivityLog.objects.filter(details__icontains=str(profile.id)).order_by("-timestamp")[:50]
            return standard_response(data=[{
                "id": item.id,
                "action": item.action,
                "description": item.details.get("description", ""),
                "created_at": item.timestamp,
            } for item in activities])
        if kind == "payments":
            return standard_response(data=PaymentSerializer(Payment.objects.filter(student=profile.user).order_by("-payment_date"), many=True).data)
        if kind == "attendance":
            return standard_response(data=AttendanceSerializer(Attendance.objects.filter(student=profile.user).order_by("-date"), many=True).data)
        return standard_response("error", "Unknown related view.", status_code=404)


class AdminStudentCountsView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [HasAdminPermission("manage_students")]

    def get(self, request):
        qs = StudentProfile.objects.all()
        girls = qs.filter(gender__iexact="Female").count()
        boys = qs.filter(gender__iexact="Male").count()
        return standard_response(data={
            "total": qs.count(),
            "live": qs.filter(status="LIVE").count(),
            "expired": StudentProfile.objects.filter(status="EXPIRED").count(),
            "suspended": StudentProfile.objects.filter(status="SUSPENDED").count(),
            "girls": girls,
            "boys": boys,
            "other": qs.exclude(gender__iexact="Female").exclude(gender__iexact="Male").count(),
        })

# Keep the analytics and export logic from v2_admin, simply updating it to use the new serializer structure if needed.
from api.v1.v2_admin import AdminStudentAnalyticsView as LegacyAdminStudentAnalyticsView
from api.v1.v2_admin import AdminStudentExportView as LegacyAdminStudentExportView
class AdminStudentAnalyticsView(LegacyAdminStudentAnalyticsView): pass
class AdminStudentExportView(LegacyAdminStudentExportView): pass
