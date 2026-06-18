import datetime
import json
import uuid
from decimal import Decimal

from django.contrib.auth import get_user_model
from django.db import connection
from django.db.models import Avg, Count, Q, Sum
from django.http import HttpResponse
from django.shortcuts import get_object_or_404
from django.utils import timezone
from rest_framework import status
from rest_framework.parsers import FormParser, JSONParser, MultiPartParser
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework.response import Response
from rest_framework.views import APIView

from apps.accounts.models import AdminUser
from apps.attendance.models import Attendance, Holiday, QRCode
from apps.library.models import Achiever, Facility, LibraryInfo, Review, HomeSlider, AppConfig
from apps.memberships.models import Membership, MembershipPlan
from apps.notifications.models import Notification, StudentNotification
from apps.payments.models import Payment
from apps.seats.models import Floor, Seat, SeatAssignment, SeatChangeLog, SeatRow
from apps.students.models import StudentProfile
from apps.study.models import StudySession
from core.models import ActivityLog, GlobalSetting
from shreshtlibrary.utils.permissions import IsLibraryAdmin, IsSuperAdmin, HasAdminPermission, IsStudent
from utils.exporters import export_to_excel, export_to_pdf
from utils.response import standard_response

from api.v1.authentication.views import AdminLoginView, get_tokens_for_user

User = get_user_model()


def _admin_user(request):
    return request.user if isinstance(request.user, AdminUser) else None


def _full_name(user):
    name = f"{getattr(user, 'first_name', '')} {getattr(user, 'last_name', '')}".strip()
    return name or getattr(user, 'username', '')


def _profile_for_pk(pk):
    pk = str(pk)
    if pk.isdigit():
        return get_object_or_404(StudentProfile, Q(pk=pk) | Q(user_id=pk))
    return get_object_or_404(StudentProfile, student_id__iexact=pk)


def _date(value, default=None):
    if not value:
        return default
    return datetime.date.fromisoformat(str(value))


def _bool(value, default=False):
    if value is None:
        return default
    if isinstance(value, bool):
        return value
    return str(value).lower() in {"1", "true", "yes", "on"}


def _image_upload(request, *names):
    for name in names:
        if name in request.FILES:
            return request.FILES[name]
    return None


def _now():
    return timezone.now()


def _activity(request, action, target_model="", target_id=None, description=""):
    admin = _admin_user(request)
    ActivityLog.objects.create(
        admin=admin,
        action=action,
        ip_address=request.META.get("REMOTE_ADDR"),
        details={
            "target_model": target_model,
            "target_id": target_id,
            "description": description,
            "path": request.path,
            "method": request.method,
        },
    )


def _paginate(request, rows):
    page = max(int(request.query_params.get("page", 1)), 1)
    page_size = min(max(int(request.query_params.get("page_size", 20)), 1), 100)
    count = len(rows)
    start = (page - 1) * page_size
    end = start + page_size
    total_pages = (count + page_size - 1) // page_size
    return Response({
        "success": True,
        "count": count,
        "total_pages": total_pages,
        "current_page": page,
        "next": None if page >= total_pages else f"?page={page + 1}&page_size={page_size}",
        "previous": None if page <= 1 else f"?page={page - 1}&page_size={page_size}",
        "data": rows[start:end],
    })


def _file_response(content, filename, content_type):
    response = HttpResponse(content, content_type=content_type)
    response["Content-Disposition"] = f'attachment; filename="{filename}"'
    return response


def _export(rows, filename):
    fmt = filename.rsplit(".", 1)[-1]
    if fmt == "pdf":
        return _file_response(export_to_pdf(str(rows)), filename, "application/pdf")
    return _file_response(export_to_excel(rows, []), filename, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")


def serialize_student(profile):
    user = profile.user
    return {
        "id": profile.id,
        "user_id": user.id,
        "student_id": profile.student_id,
        "username": user.username,
        "first_name": user.first_name,
        "middle_name": profile.middle_name,
        "last_name": user.last_name,
        "email": user.email,
        "mobile": user.mobile,
        "parent_mobile": profile.parent_mobile,
        "goal": profile.goal,
        "dob": profile.dob,
        "date_of_birth": profile.dob,
        "gender": profile.gender,
        "caste": profile.caste,
        "address": profile.address,
        "profile_photo": profile.profile_photo.url if profile.profile_photo else None,
        "profile_image": profile.profile_photo.url if profile.profile_photo else None,
        "status": profile.status,
        "is_active": user.is_active and profile.status != "SUSPENDED",
        "suspension_reason": profile.suspension_reason,
        "suspended_at": profile.suspended_at,
        "preferred_language": profile.preferred_language,
        "referral_code": (profile.user.referral_codes.first().code if profile.user.referral_codes.exists() else None),
        "created_at": profile.created_at,
        "updated_at": profile.updated_at,
    }


def serialize_plan(plan):
    return {
        "id": plan.id,
        "name": plan.name,
        "duration_months": plan.duration_months,
        "duration_days": plan.duration_days,
        "price": str(plan.price),
        "benefits": plan.benefits,
        "description": plan.description,
        "is_active": plan.is_active,
        "sort_order": plan.sort_order,
        "created_at": plan.created_at,
        "updated_at": plan.updated_at,
    }


def serialize_membership(membership):
    return {
        "id": membership.id,
        "student": membership.student.id,
        "student_name": _full_name(membership.student),
        "plan": membership.plan_id,
        "plan_name": membership.plan.name if membership.plan_id else membership.plan_name_snapshot,
        "plan_name_snapshot": membership.plan_name_snapshot,
        "price_snapshot": str(membership.price_snapshot),
        "start_date": membership.start_date,
        "end_date": membership.end_date,
        "status": membership.status.upper(),
        "is_active": membership.is_active,
        "renewal_count": membership.renewal_count,
        "notes": membership.notes,
        "created_at": membership.created_at,
    }


def serialize_payment(payment):
    return {
        "id": payment.id,
        "payment_id": payment.payment_id,
        "student": payment.student_id,
        "student_name": _full_name(payment.student),
        "membership": payment.membership_id,
        "plan_name": payment.membership.plan.name if payment.membership_id else None,
        "amount": str(payment.amount),
        "method": payment.method or payment.payment_mode,
        "payment_mode": payment.payment_mode,
        "status": payment.status.upper(),
        "transaction_ref": payment.transaction_ref or payment.transaction_id,
        "transaction_id": payment.transaction_id,
        "receipt_url": payment.receipt_url.url if payment.receipt_url else None,
        "notes": payment.notes,
        "paid_at": payment.paid_at,
        "payment_date": payment.payment_date,
        "verified_at": payment.verified_at,
        "refund_amount": str(payment.refund_amount) if payment.refund_amount is not None else None,
        "refund_reason": payment.refund_reason,
        "refunded_at": payment.refunded_at,
        "created_at": payment.created_at,
    }


def serialize_attendance(record):
    return {
        "id": record.id,
        "student": record.student_id,
        "student_name": _full_name(record.student),
        "date": record.date,
        "time_in": record.time_in,
        "is_present": record.is_present,
        "method": record.method,
        "is_manual": record.is_manual,
        "marked_at": record.marked_at,
        "note": record.note,
    }


def serialize_holiday(holiday):
    return {
        "id": holiday.id,
        "date": holiday.date,
        "title": holiday.title,
        "description": holiday.description,
        "is_active": holiday.is_active,
        "created_at": holiday.created_at,
        "updated_at": holiday.updated_at,
    }


def _holiday_for_date(value):
    return Holiday.objects.filter(date=value, is_active=True).first()


def serialize_qr(qr):
    return {
        "id": qr.id,
        "token": str(qr.token) if qr.token else None,
        "code": qr.code,
        "qr_hash": qr.qr_hash,
        "valid_date": qr.valid_date,
        "is_active": qr.is_active,
        "is_expired": qr.is_expired,
        "generation_method": qr.generation_method,
        "expiry_timestamp": qr.expiry_timestamp,
        "expires_at": qr.expires_at,
        "created_at": qr.created_at,
    }


def serialize_student_qr_status(qr, valid_date):
    return {
        "valid_date": valid_date,
        "is_available": bool(qr),
        "expires_at": qr.expires_at if qr else None,
    }


def serialize_floor(floor):
    return {
        "id": floor.id,
        "name": floor.name,
        "description": floor.description,
        "order": floor.order,
        "is_active": floor.is_active,
        "rows": [serialize_row(row) for row in floor.rows.all()],
    }


def serialize_row(row):
    return {
        "id": row.id,
        "floor": row.floor_id,
        "label": row.label,
        "order": row.order,
        "seats": [serialize_seat(seat) for seat in row.seats.all()],
    }


def serialize_seat(seat):
    occupant = seat.student
    profile = getattr(occupant, "student_profile", None) if occupant else None
    profile_image = profile.profile_photo.url if profile and profile.profile_photo else None
    return {
        "id": seat.id,
        "floor": seat.floor,
        "row": seat.row,
        "row_ref": seat.row_ref_id,
        "seat_number": seat.seat_number,
        "status": seat.status.upper(),
        "student": occupant.id if occupant else None,
        "student_name": _full_name(occupant) if occupant else None,
        "student_code": profile.student_id if profile else None,
        "student_profile_image": profile_image,
        "student_profile_photo": profile_image,
        "assigned_at": seat.assigned_at,
        "notes": seat.notes,
    }


def serialize_notification(notification):
    return {
        "id": notification.id,
        "title": notification.title,
        "body": notification.body,
        "type": notification.type,
        "target": notification.target,
        "target_group": notification.target_group,
        "goal_filter": notification.goal_filter,
        "status_filter": notification.status_filter,
        "send_push": notification.send_push,
        "send_email": notification.send_email,
        "send_sms": notification.send_sms,
        "scheduled_at": notification.scheduled_at,
        "sent_at": notification.sent_at,
        "total_recipients": notification.total_recipients,
        "success_count": notification.success_count,
        "failure_count": notification.failure_count,
        "created_at": notification.created_at,
        "subtitle": notification.subtitle,
        "description": notification.description,
        "link_url": notification.link_url,
        "link_button_text": notification.link_button_text,
        "event_date": notification.event_date,
        "layout": notification.layout,
        "background_image": notification.background_image.url if notification.background_image else None,
        "audience": notification.audience,
        "display_mode": notification.display_mode,
        "recurring_time": notification.recurring_time.strftime("%H:%M") if notification.recurring_time else None,
        "expires_at": notification.expires_at,
        "images": [img.image.url for img in notification.images.all()] if hasattr(notification, 'images') else [],
    }


def serialize_review(review):
    return {
        "id": review.id,
        "student": review.student_id,
        "student_name": _full_name(review.student),
        "rating": review.rating,
        "comment": review.comment,
        "text": review.comment,
        "is_approved": review.is_approved,
        "rejection_reason": review.rejection_reason,
        "approved_at": review.approved_at,
        "created_at": review.created_at,
        "updated_at": review.updated_at,
    }


def serialize_library_info(info):
    return {
        "id": info.id,
        "name": info.name,
        "tagline": info.tagline,
        "description": info.description or info.about,
        "feature_image": info.feature_image.url if info.feature_image else None,
        "about": info.about,
        "address": info.address,
        "phone_primary": info.phone_primary,
        "phone_secondary": info.phone_secondary,
        "email": info.email,
        "website": info.website,
        "open_time": info.open_time,
        "close_time": info.close_time,
        "off_days": info.off_days,
        "rules": info.rules,
        "facilities": info.facilities,
        "google_maps_url": info.google_maps_url,
        "instagram_url": info.instagram_url,
        "facebook_url": info.facebook_url,
        "updated_at": info.updated_at,
    }


def serialize_facility(facility):
    return {
        "id": facility.id,
        "name": facility.name,
        "icon_key": facility.icon_key,
        "image": facility.image.url if facility.image else None,
        "description": facility.description,
        "is_active": facility.is_active,
        "order": facility.order,
    }


def serialize_achiever(achiever):
    return {
        "id": achiever.id,
        "name": achiever.name,
        "photo": achiever.photo.url if achiever.photo else None,
        "goal": achiever.goal,
        "achievement": achiever.achievement,
        "year": achiever.year,
        "is_featured": achiever.is_featured,
        "is_active": achiever.is_active,
        "order": achiever.order,
        "created_at": achiever.created_at,
    }


def serialize_admin(admin):
    ALL_PERMISSIONS = {
        "manage_students": True,
        "manage_attendance": True,
        "manage_plans": True,
        "manage_payments": True,
        "manage_notifications": True,
        "manage_library": True,
        "manage_seats": True,
        "manage_holidays": True,
        "manage_reviews": True,
    }
    permissions = ALL_PERMISSIONS if admin.role == "super_admin" else admin.permissions

    return {
        "id": admin.id,
        "username": admin.username,
        "first_name": admin.first_name,
        "last_name": admin.last_name,
        "email": admin.email,
        "mobile": admin.mobile,
        "role": admin.role,
        "permissions": permissions,
        "profile_image": admin.profile_image.url if admin.profile_image else None,
        "is_active": admin.is_active,
        "date_joined": admin.date_joined,
        "last_login": admin.last_login,
    }


class AdminMeView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        if isinstance(request.user, AdminUser):
            return standard_response(data=serialize_admin(request.user))
        return standard_response(data={
            "id": request.user.id,
            "username": request.user.username,
            "email": request.user.email,
            "mobile": request.user.mobile,
            "role": request.user.role,
            "is_active": request.user.is_active,
        })


class AdminProfileView(APIView):
    permission_classes = [IsLibraryAdmin]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get(self, request):
        admin = _admin_user(request)
        activity_count = ActivityLog.objects.filter(admin=admin).count() if admin else 0
        data = {
            **serialize_admin(admin),
            "activity_count": activity_count,
            "created_admins_count": AdminUser.objects.filter(created_by=admin).count() if admin else 0,
            "verified_payments_count": Payment.objects.filter(verified_by=admin).count() if admin else 0,
            "marked_attendance_count": Attendance.objects.filter(marked_by=admin).count() if admin else 0,
        }
        return standard_response(data=data)

    def put(self, request):
        return self._update(request)

    def patch(self, request):
        return self._update(request)

    def _update(self, request):
        admin = _admin_user(request)
        if not admin:
            return standard_response("error", "Admin profile not found.", status_code=404)

        errors = {}
        for field in ["email", "mobile"]:
            if field in request.data and request.data.get(field):
                exists = AdminUser.objects.filter(**{field: request.data[field]}).exclude(id=admin.id).exists()
                if exists:
                    errors[field] = ["This value is already used by another admin."]
        if errors:
            return standard_response("error", "Profile update failed.", errors=errors, status_code=400)

        for field in ["first_name", "last_name", "email", "mobile"]:
            if field in request.data:
                setattr(admin, field, request.data[field])
        image = _image_upload(request, "profile_image", "image", "photo")
        if image:
            admin.profile_image = image
        admin.save()
        _activity(request, "EDIT_ADMIN_PROFILE", "AdminUser", admin.id, "Updated own admin profile")
        return standard_response(message="Profile updated successfully.", data=serialize_admin(admin))


class FCMTokenUpdateView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        token = request.data.get("token") or request.data.get("fcm_token")
        if not token:
            return standard_response("error", "Token is required.", errors={"token": ["This field is required."]}, status_code=400)
        if hasattr(request.user, "fcm_token"):
            request.user.fcm_token = token
            request.user.save(update_fields=["fcm_token"])
        return standard_response(message="FCM token updated.")


class ChangePasswordAliasView(APIView):
    permission_classes = [IsAuthenticated]

    def put(self, request):
        return self._change(request)

    def post(self, request):
        return self._change(request)

    def _change(self, request):
        old_password = request.data.get("old_password")
        new_password = request.data.get("new_password")
        confirm_password = request.data.get("confirm_password", new_password)
        if new_password != confirm_password:
            return standard_response("error", "Passwords do not match.", errors={"confirm_password": ["Passwords do not match."]}, status_code=400)
        if not request.user.check_password(old_password):
            return standard_response("error", "Incorrect old password.", errors={"old_password": ["Incorrect old password."]}, status_code=400)
        request.user.set_password(new_password)
        request.user.save()
        return standard_response(message="Password changed successfully.")


class AdminStudentsView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get(self, request):
        qs = StudentProfile.objects.select_related("user").all().order_by("-created_at", "-id")
        search = request.query_params.get("search")
        if search:
            qs = qs.filter(
                Q(student_id__icontains=search) |
                Q(user__first_name__icontains=search) |
                Q(user__last_name__icontains=search) |
                Q(user__email__icontains=search) |
                Q(user__mobile__icontains=search)
            )
        for field in ["status", "goal", "gender"]:
            value = request.query_params.get(field)
            if value:
                qs = qs.filter(**{field: value})
        created_from = _date(request.query_params.get("created_from"))
        created_to = _date(request.query_params.get("created_to"))
        if created_from:
            qs = qs.filter(created_at__date__gte=created_from)
        if created_to:
            qs = qs.filter(created_at__date__lte=created_to)
        return _paginate(request, [serialize_student(profile) for profile in qs])

    def post(self, request):
        payload = request.data
        mobile = payload.get("mobile")
        email = payload.get("email")
        password = payload.get("password") or mobile or "studentpassword123"
        if not mobile:
            return standard_response("error", "Mobile is required.", errors={"mobile": ["This field is required."]}, status_code=400)
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
        return standard_response(message="Student created successfully.", data=serialize_student(profile), status_code=201)


class AdminStudentDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get(self, request, pk):
        return standard_response(data=serialize_student(_profile_for_pk(pk)))

    def put(self, request, pk):
        return self._update(request, pk)

    def patch(self, request, pk):
        return self._update(request, pk)

    def delete(self, request, pk):
        if not isinstance(request.user, AdminUser) or request.user.role != "super_admin":
            return standard_response("error", "Super admin access required.", status_code=403)
        profile = _profile_for_pk(pk)
        user = profile.user
        _activity(request, "DELETE_STUDENT", "StudentProfile", profile.id, f"Deleted student {profile.student_id}")
        user.delete()
        return standard_response(message="Student deleted successfully.")

    def _update(self, request, pk):
        profile = _profile_for_pk(pk)
        user = profile.user
        for field in ["first_name", "last_name", "email", "mobile"]:
            if field in request.data:
                setattr(user, field, request.data[field])
        if "is_active" in request.data:
            user.is_active = bool(request.data["is_active"])
        user.save()
        for field in ["middle_name", "goal", "dob", "gender", "caste", "address", "parent_mobile", "status", "preferred_language"]:
            if field in request.data:
                setattr(profile, field, request.data[field])
        if "date_of_birth" in request.data:
            profile.dob = request.data["date_of_birth"]
        image = _image_upload(request, "profile_photo", "profile_image", "image")
        if image:
            profile.profile_photo = image
        profile.save()
        _activity(request, "EDIT_STUDENT", "StudentProfile", profile.id, f"Updated student {profile.student_id}")
        return standard_response(message="Student updated successfully.", data=serialize_student(profile))


class AdminStudentPhotoView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]
    parser_classes = [MultiPartParser, FormParser]

    def post(self, request, pk):
        return self._upload(request, pk)

    def put(self, request, pk):
        return self._upload(request, pk)

    def _upload(self, request, pk):
        profile = _profile_for_pk(pk)
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
        return standard_response(message="Profile image uploaded successfully.", data=serialize_student(profile))


class AdminStudentAnalyticsView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]

    def get(self, request, pk):
        profile = _profile_for_pk(pk)
        period = request.query_params.get("period", "weekly")
        if period not in {"weekly", "monthly", "yearly"}:
            period = "weekly"

        today = timezone.localdate()
        if period == "weekly":
            start = today - datetime.timedelta(days=6)
            buckets = [(start + datetime.timedelta(days=index), (start + datetime.timedelta(days=index)).strftime("%d %b")) for index in range(7)]
            bucket_for_date = lambda value: value
        elif period == "monthly":
            start = today - datetime.timedelta(days=29)
            buckets = [(start + datetime.timedelta(days=index), (start + datetime.timedelta(days=index)).strftime("%d %b")) for index in range(30)]
            bucket_for_date = lambda value: value
        else:
            first_this_month = today.replace(day=1)
            buckets = []
            cursor = first_this_month
            for _ in range(12):
                buckets.append((cursor, cursor.strftime("%b %Y")))
                cursor = (cursor.replace(day=1) - datetime.timedelta(days=1)).replace(day=1)
            buckets.reverse()
            start = buckets[0][0]
            bucket_for_date = lambda value: value.replace(day=1)

        attendance_map = {key: {"label": label, "present": 0, "absent": 0, "total": 0} for key, label in buckets}
        attendance_qs = Attendance.objects.filter(student=profile.user, date__gte=start, date__lte=today)
        for record in attendance_qs:
            key = bucket_for_date(record.date)
            if key in attendance_map:
                attendance_map[key]["total"] += 1
                if record.is_present:
                    attendance_map[key]["present"] += 1
                else:
                    attendance_map[key]["absent"] += 1

        study_map = {key: {"label": label, "hours": 0.0} for key, label in buckets}
        sessions = StudySession.objects.filter(student=profile.user, start_time__date__gte=start, start_time__date__lte=today)
        for session in sessions:
            session_date = timezone.localtime(session.start_time).date()
            key = bucket_for_date(session_date)
            if key in study_map:
                minutes = session.duration_minutes or 0
                if not minutes and session.end_time:
                    minutes = max(int((session.end_time - session.start_time).total_seconds() // 60), 0)
                study_map[key]["hours"] += round(minutes / 60, 2)

        return standard_response(data={
            "period": period,
            "attendance": list(attendance_map.values()),
            "study": [
                {
                    "label": item["label"],
                    "hours": round(item["hours"], 2),
                }
                for item in study_map.values()
            ],
        })


class AdminStudentStatusView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]

    def post(self, request, pk, action):
        profile = _profile_for_pk(pk)
        if action == "suspend":
            profile.status = "SUSPENDED"
            profile.suspension_reason = request.data.get("reason") or request.data.get("suspension_reason")
            profile.suspended_at = _now()
            profile.suspended_by = _admin_user(request)
            event = "SUSPEND_STUDENT"
        else:
            profile.status = "LIVE"
            profile.suspension_reason = None
            profile.suspended_at = None
            profile.suspended_by = None
            event = "ACTIVATE_STUDENT"
        profile.save()
        _activity(request, event, "StudentProfile", profile.id, f"{event} {profile.student_id}")
        return standard_response(data=serialize_student(profile))


class AdminStudentRelatedView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]

    def get(self, request, pk, kind):
        profile = _profile_for_pk(pk)
        if kind == "timeline":
            activities = ActivityLog.objects.filter(details__icontains=str(profile.id)).order_by("-timestamp")[:50]
            return standard_response(data=[{
                "id": item.id,
                "action": item.action,
                "description": item.details.get("description", ""),
                "created_at": item.timestamp,
            } for item in activities])
        if kind == "payments":
            return standard_response(data=[serialize_payment(item) for item in Payment.objects.filter(student=profile.user).order_by("-payment_date")])
        if kind == "attendance":
            return standard_response(data=[serialize_attendance(item) for item in Attendance.objects.filter(student=profile.user).order_by("-date")])
        return standard_response("error", "Unknown related view.", status_code=404)


class AdminStudentCountsView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]

    def get(self, request):
        girls = StudentProfile.objects.filter(gender__iexact="Female").count()
        boys = StudentProfile.objects.filter(gender__iexact="Male").count()
        return standard_response(data={
            "total": StudentProfile.objects.count(),
            "live": StudentProfile.objects.filter(status="LIVE").count(),
            "expired": StudentProfile.objects.filter(status="EXPIRED").count(),
            "suspended": StudentProfile.objects.filter(status="SUSPENDED").count(),
            "girls": girls,
            "boys": boys,
            "other": StudentProfile.objects.exclude(gender__iexact="Female").exclude(gender__iexact="Male").count(),
        })


class AdminStudentExportView(APIView):
    permission_classes = [HasAdminPermission("manage_students")]

    def get(self, request):
        rows = [serialize_student(profile) for profile in StudentProfile.objects.select_related("user").all()]
        fmt = request.query_params.get("format", "xlsx")
        return _export(rows, f"students.{fmt}")


class PlansView(APIView):
    def get_permissions(self):
        if self.request.method == "GET":
            return [AllowAny()]
        return [HasAdminPermission("manage_plans")()]

    def get(self, request):
        qs = MembershipPlan.objects.filter(is_active=True).order_by("sort_order", "price")
        return standard_response(data=[serialize_plan(plan) for plan in qs])

    def post(self, request):
        if not request.data.get("name"):
            return standard_response("error", "Plan name is required.", errors={"name": ["This field is required."]}, status_code=400)
        plan = MembershipPlan.objects.create(
            name=request.data.get("name"),
            duration_months=int(request.data.get("duration_months") or max(int(request.data.get("duration_days", 30)) // 30, 1)),
            duration_days=int(request.data.get("duration_days", 30)),
            price=Decimal(str(request.data.get("price", 0))),
            benefits=request.data.get("benefits", []),
            description=request.data.get("description"),
            is_active=bool(request.data.get("is_active", True)),
            sort_order=int(request.data.get("sort_order", 0)),
        )
        _activity(request, "CREATE_PLAN", "MembershipPlan", plan.id, f"Created plan {plan.name}")
        return standard_response(message="Plan created successfully.", data=serialize_plan(plan), status_code=201)


class PlansAllView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request):
        return standard_response(data=[serialize_plan(plan) for plan in MembershipPlan.objects.all().order_by("sort_order", "price")])


class PlanDetailView(APIView):
    def get_permissions(self):
        if self.request.method == "GET":
            return [AllowAny()]
        return [HasAdminPermission("manage_plans")()]

    def get(self, request, pk):
        return standard_response(data=serialize_plan(get_object_or_404(MembershipPlan, id=pk)))

    def put(self, request, pk):
        plan = get_object_or_404(MembershipPlan, id=pk)
        for field in ["name", "description", "benefits", "is_active", "sort_order"]:
            if field in request.data:
                setattr(plan, field, request.data[field])
        for field in ["duration_months", "duration_days"]:
            if field in request.data:
                setattr(plan, field, int(request.data[field]))
        if "price" in request.data:
            plan.price = Decimal(str(request.data["price"]))
        plan.save()
        _activity(request, "EDIT_PLAN", "MembershipPlan", plan.id, f"Updated plan {plan.name}")
        return standard_response(data=serialize_plan(plan))

    def delete(self, request, pk):
        plan = get_object_or_404(MembershipPlan, id=pk)
        if Membership.objects.filter(plan=plan).exists():
            return standard_response("error", "Plan has memberships and cannot be deleted.", status_code=400)
        plan.delete()
        return standard_response(message="Plan deleted successfully.")


class PlanToggleView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def patch(self, request, pk):
        plan = get_object_or_404(MembershipPlan, id=pk)
        plan.is_active = request.data.get("is_active", not plan.is_active)
        plan.save(update_fields=["is_active", "updated_at"])
        return standard_response(data=serialize_plan(plan))


class PlanStudentsView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request, pk):
        memberships = Membership.objects.filter(plan_id=pk, status="active").select_related("student")
        return standard_response(data=[serialize_student(item.student.student_profile) for item in memberships if hasattr(item.student, "student_profile")])


class PlanStatsView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request):
        data = []
        for plan in MembershipPlan.objects.all():
            data.append({
                **serialize_plan(plan),
                "active_students": Membership.objects.filter(plan=plan, status="active").values("student").distinct().count(),
                "all_time_students": Membership.objects.filter(plan=plan).values("student").distinct().count(),
            })
        return standard_response(data=data)


class AdminMembershipsView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request):
        qs = Membership.objects.select_related("student", "plan").all().order_by("-start_date")
        if request.query_params.get("status"):
            qs = qs.filter(status=request.query_params["status"].lower())
        if request.query_params.get("student_id"):
            qs = qs.filter(student_id=request.query_params["student_id"])
        if request.query_params.get("plan_id"):
            qs = qs.filter(plan_id=request.query_params["plan_id"])
        return _paginate(request, [serialize_membership(item) for item in qs])


class AdminMembershipActionView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def post(self, request, action):
        student = get_object_or_404(User, id=request.data.get("student_id"), role="student")
        plan = get_object_or_404(MembershipPlan, id=request.data.get("plan_id"))
        today = timezone.now().date()
        if action == "renew":
            current = Membership.objects.filter(student=student, status="active").order_by("-end_date").first()
            start = current.end_date + datetime.timedelta(days=1) if current else today
            renewal_count = (current.renewal_count + 1) if current else 1
        else:
            Membership.objects.filter(student=student, status="active").update(status="cancelled", is_active=False)
            start = _date(request.data.get("start_date"), today)
            renewal_count = 0
        end = _date(request.data.get("end_date"), start + datetime.timedelta(days=plan.duration_days))
        membership = Membership.objects.create(
            student=student,
            plan=plan,
            start_date=start,
            end_date=end,
            status="active",
            renewal_count=renewal_count,
            notes=request.data.get("notes"),
            created_by=_admin_user(request),
        )
        _activity(request, f"MEMBERSHIP_{action.upper()}", "Membership", membership.id, f"{action} membership for {student.username}")
        return standard_response(data=serialize_membership(membership), status_code=201)


class AdminMembershipDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request, pk):
        return standard_response(data=serialize_membership(get_object_or_404(Membership, id=pk)))

    def put(self, request, pk):
        membership = get_object_or_404(Membership, id=pk)
        for field in ["start_date", "end_date", "notes"]:
            if field in request.data:
                setattr(membership, field, request.data[field])
        if "status" in request.data:
            membership.status = request.data["status"].lower()
        membership.save()
        return standard_response(data=serialize_membership(membership))


class AdminMembershipSpecialView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request, kind):
        today = timezone.now().date()
        if kind == "expiring":
            days = int(request.query_params.get("days", 7))
            qs = Membership.objects.filter(status="active", end_date__lte=today + datetime.timedelta(days=days), end_date__gte=today)
        else:
            qs = Membership.objects.filter(end_date=today)
        return standard_response(data=[serialize_membership(item) for item in qs.select_related("student", "plan")])


def _generate_qr(request, method="MANUAL"):
    today = timezone.now().date()
    holiday = _holiday_for_date(today)
    if holiday:
        return None
    QRCode.objects.filter(is_active=True).update(is_active=False, is_expired=True)
    now = timezone.now()
    qr = QRCode.objects.create(
        token=uuid.uuid4(),
        code=f"library-qr-{now.date()}-{uuid.uuid4()}",
        valid_date=now.date(),
        expiry_timestamp=now + datetime.timedelta(days=1),
        expires_at=now + datetime.timedelta(days=1),
        is_active=True,
        is_expired=False,
        generation_method=method,
        created_by=_admin_user(request),
    )
    _activity(request, "GENERATE_QR", "QRCode", qr.id, "Generated QR code")
    return qr


class StudentQRTodayView(APIView):
    permission_classes = [IsStudent]

    def get(self, request):
        today = timezone.now().date()
        holiday = _holiday_for_date(today)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=serialize_holiday(holiday), status_code=400)
        qr = QRCode.objects.filter(is_active=True, valid_date=today).order_by("-created_at").first()
        return standard_response(data=serialize_student_qr_status(qr, today))


class StudentQRScanView(APIView):
    permission_classes = [IsStudent]

    def post(self, request):
        qr_hash = request.data.get("qr_hash") or request.data.get("code")
        qr = QRCode.objects.filter(Q(qr_hash=qr_hash) | Q(code=qr_hash), is_active=True, is_expired=False).first()
        if not qr:
            return standard_response("error", "Invalid QR code.", status_code=400)
        today = timezone.now().date()
        holiday = _holiday_for_date(today)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=serialize_holiday(holiday), status_code=400)
        if Attendance.objects.filter(student=request.user, date=today).exists():
            return standard_response("error", "You have already marked attendance today.", status_code=400)
        record = Attendance.objects.create(student=request.user, date=today, qr_code=qr, method="QR", is_present=True)
        
        # Start study session
        from apps.study.models import StudySession
        active = StudySession.objects.filter(student=request.user, end_time__isnull=True).exists()
        if not active:
            StudySession.objects.create(student=request.user, status='starting')
            
        return standard_response(message="Attendance marked successfully. Study session started.", data=serialize_attendance(record), status_code=201)


class AdminQRView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def get(self, request, action=None, pk=None):
        if action == "history":
            return _paginate(request, [serialize_qr(qr) for qr in QRCode.objects.all().order_by("-created_at")])
        if action == "scans":
            return standard_response(data=[serialize_attendance(item) for item in Attendance.objects.filter(qr_code_id=pk)])
        qr = QRCode.objects.filter(is_active=True).order_by("-created_at").first()
        return standard_response(data=serialize_qr(qr) if qr else None)

    def post(self, request, action=None):
        if action in ["generate", "regenerate"] and _holiday_for_date(timezone.now().date()):
            holiday = _holiday_for_date(timezone.now().date())
            return standard_response("error", f"QR cannot be generated on holiday: {holiday.title}.", data=serialize_holiday(holiday), status_code=400)
        if action in ["generate", "regenerate"]:
            qr = _generate_qr(request)
            if not qr:
                return standard_response("error", "QR cannot be generated on a holiday.", status_code=400)
            return standard_response(data=serialize_qr(qr), status_code=201)
        if action == "expire":
            QRCode.objects.filter(is_active=True).update(is_active=False, is_expired=True)
            return standard_response(message="Current QR expired.")
        return standard_response("error", "Unknown QR action.", status_code=404)


class AdminAttendanceView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def get(self, request):
        qs = Attendance.objects.select_related("student").all().order_by("-date", "-marked_at")
        if request.query_params.get("student_id"):
            qs = qs.filter(student_id=request.query_params["student_id"])
        if request.query_params.get("date"):
            qs = qs.filter(date=request.query_params["date"])
        if request.query_params.get("from_date"):
            qs = qs.filter(date__gte=request.query_params["from_date"])
        if request.query_params.get("to_date"):
            qs = qs.filter(date__lte=request.query_params["to_date"])
        if request.query_params.get("method"):
            qs = qs.filter(method=request.query_params["method"])
        return _paginate(request, [serialize_attendance(item) for item in qs])

    def post(self, request):
        student = None
        if request.data.get("student_id"):
            student = get_object_or_404(User, id=request.data["student_id"], role="student")
        elif request.data.get("student_mobile"):
            student = get_object_or_404(User, mobile=request.data["student_mobile"], role="student")
        if not student:
            return standard_response("error", "Student is required.", status_code=400)
        date = _date(request.data.get("date"), timezone.now().date())
        holiday = _holiday_for_date(date)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=serialize_holiday(holiday), status_code=400)
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
        return standard_response(data=serialize_attendance(record), status_code=201)


class AdminAttendanceDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def put(self, request, pk):
        record = get_object_or_404(Attendance, id=pk)
        target_date = _date(request.data.get("date"), record.date)
        holiday = _holiday_for_date(target_date)
        if holiday:
            return standard_response("error", f"Attendance is closed for holiday: {holiday.title}.", data=serialize_holiday(holiday), status_code=400)
        for field in ["is_present", "method", "note"]:
            if field in request.data:
                setattr(record, field, request.data[field])
        if "date" in request.data:
            record.date = target_date
        record.save()
        return standard_response(data=serialize_attendance(record))

    def delete(self, request, pk):
        get_object_or_404(Attendance, id=pk).delete()
        return standard_response(message="Attendance deleted.")


class HolidayView(APIView):
    def get_permissions(self):
        if self.request.method == "GET":
            return [IsAuthenticated()]
        return [HasAdminPermission("manage_holidays")()]

    def get(self, request, pk=None):
        if pk:
            return standard_response(data=serialize_holiday(get_object_or_404(Holiday, id=pk)))
        qs = Holiday.objects.all().order_by("date")
        if request.query_params.get("date"):
            qs = qs.filter(date=request.query_params["date"])
        if request.query_params.get("from_date"):
            qs = qs.filter(date__gte=request.query_params["from_date"])
        if request.query_params.get("to_date"):
            qs = qs.filter(date__lte=request.query_params["to_date"])
        if request.query_params.get("is_active") is not None:
            qs = qs.filter(is_active=_bool(request.query_params.get("is_active"), True))
        return standard_response(data=[serialize_holiday(item) for item in qs])

    def post(self, request, pk=None):
        date = _date(request.data.get("date"))
        title = (request.data.get("title") or "").strip()
        if not date or not title:
            return standard_response("error", "Holiday date and title are required.", status_code=400)
        existing = Holiday.objects.filter(date=date).first()
        was_active = existing.is_active if existing else False
        holiday, _ = Holiday.objects.update_or_create(
            date=date,
            defaults={
                "title": title,
                "description": request.data.get("description", ""),
                "is_active": _bool(request.data.get("is_active", True), True),
                "created_by": _admin_user(request),
            },
        )
        if holiday.is_active:
            Attendance.objects.filter(date=holiday.date).delete()
            QRCode.objects.filter(valid_date=holiday.date).update(is_active=False, is_expired=True)
            if not was_active:
                _notify_holiday(holiday, action="added", admin_user=_admin_user(request))
        elif was_active and not holiday.is_active:
            _notify_holiday(holiday, action="cancelled", admin_user=_admin_user(request))
        _activity(request, "UPSERT_HOLIDAY", "Holiday", holiday.id, f"Saved holiday {holiday.date}")
        return standard_response(message="Holiday saved.", data=serialize_holiday(holiday), status_code=201)

    def put(self, request, pk=None):
        holiday = get_object_or_404(Holiday, id=pk)
        was_active = holiday.is_active
        if "date" in request.data:
            holiday.date = _date(request.data.get("date"), holiday.date)
        if "title" in request.data:
            holiday.title = request.data.get("title") or holiday.title
        if "description" in request.data:
            holiday.description = request.data.get("description", "")
        if "is_active" in request.data:
            holiday.is_active = _bool(request.data.get("is_active"), holiday.is_active)
        holiday.save()
        if holiday.is_active:
            Attendance.objects.filter(date=holiday.date).delete()
            QRCode.objects.filter(valid_date=holiday.date).update(is_active=False, is_expired=True)
            if not was_active:
                _notify_holiday(holiday, action="added", admin_user=_admin_user(request))
        elif was_active and not holiday.is_active:
            _notify_holiday(holiday, action="cancelled", admin_user=_admin_user(request))
        _activity(request, "UPDATE_HOLIDAY", "Holiday", holiday.id, f"Updated holiday {holiday.date}")
        return standard_response(message="Holiday updated.", data=serialize_holiday(holiday))

    def delete(self, request, pk=None):
        holiday = get_object_or_404(Holiday, id=pk)
        was_active = holiday.is_active
        holiday.delete()
        if was_active:
            _notify_holiday(holiday, action="cancelled", admin_user=_admin_user(request))
        _activity(request, "DELETE_HOLIDAY", "Holiday", pk, "Deleted holiday")
        return standard_response(message="Holiday deleted.")


class AdminAttendanceSummaryView(APIView):
    permission_classes = [HasAdminPermission("manage_attendance")]

    def get(self, request, kind):
        date = _date(request.query_params.get("date"), timezone.now().date())
        present_students = Attendance.objects.filter(date=date, is_present=True).values_list("student_id", flat=True)
        
        # calculate pending state for today
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
            total = User.objects.filter(role="student").count()
            present = len(set(present_students))
            pending_count = max(total - present, 0) if is_pending_period else 0
            absent_count = 0 if is_pending_period else max(total - present, 0)
            return standard_response(data={"date": date, "present": present, "absent": absent_count, "pending": pending_count, "total": total})
        if kind == "absentees":
            qs = StudentProfile.objects.exclude(user_id__in=present_students).select_related("user")
            res = []
            for item in qs:
                ser = serialize_student(item)
                ser["attendance_status"] = "pending" if is_pending_period else "absent"
                res.append(ser)
            return standard_response(data=res)
        streaks = []
        for profile in StudentProfile.objects.select_related("user"):
            count = Attendance.objects.filter(student=profile.user, is_present=True).count()
            streaks.append({"student": serialize_student(profile), "streak": count})
        return standard_response(data=sorted(streaks, key=lambda item: item["streak"], reverse=True)[:20])


class AdminPaymentsView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def get(self, request):
        qs = Payment.objects.select_related("student", "membership", "membership__plan").all().order_by("-payment_date", "-id")
        for field in ["status", "method", "student_id"]:
            if request.query_params.get(field):
                lookup = "payment_mode" if field == "method" else field
                value = request.query_params[field].lower() if field == "status" else request.query_params[field]
                qs = qs.filter(**{lookup: value})
        if request.query_params.get("from_date"):
            qs = qs.filter(payment_date__gte=request.query_params["from_date"])
        return _paginate(request, [serialize_payment(item) for item in qs])

    def post(self, request):
        student = get_object_or_404(User, id=request.data.get("student_id"), role="student")
        membership = Membership.objects.filter(id=request.data.get("membership_id")).first()
        payment = Payment.objects.create(
            student=student,
            membership=membership,
            amount=Decimal(str(request.data.get("amount", 0))),
            payment_mode=request.data.get("payment_mode") or request.data.get("method", "Cash"),
            method=(request.data.get("method") or request.data.get("payment_mode", "Cash")).upper(),
            transaction_id=request.data.get("transaction_id") or request.data.get("transaction_ref"),
            transaction_ref=request.data.get("transaction_ref") or request.data.get("transaction_id"),
            notes=request.data.get("notes"),
            recorded_by=_admin_user(request),
            paid_at=_now(),
            status="pending",
        )
        _activity(request, "RECORD_PAYMENT", "Payment", payment.id, f"Recorded payment {payment.payment_id}")
        return standard_response(data=serialize_payment(payment), status_code=201)


class AdminPaymentDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def get(self, request, pk):
        return standard_response(data=serialize_payment(get_object_or_404(Payment, id=pk)))

    def put(self, request, pk):
        payment = get_object_or_404(Payment, id=pk)
        for field in ["notes", "transaction_id", "transaction_ref", "payment_mode", "method"]:
            if field in request.data:
                setattr(payment, field, request.data[field])
        if "amount" in request.data:
            payment.amount = Decimal(str(request.data["amount"]))
        if "status" in request.data:
            payment.status = request.data["status"].lower()
        payment.save()
        return standard_response(data=serialize_payment(payment))


class AdminPaymentActionView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def post(self, request, pk, action):
        payment = get_object_or_404(Payment, id=pk)
        if action == "verify":
            payment.status = "verified"
            payment.verified_by = _admin_user(request)
            payment.verified_at = _now()
            if payment.membership:
                payment.membership.status = "active"
                payment.membership.save()
            event = "VERIFY_PAYMENT"
        elif action == "refund":
            payment.status = "refunded"
            payment.refund_amount = Decimal(str(request.data.get("refund_amount", payment.amount)))
            payment.refund_reason = request.data.get("refund_reason") or request.data.get("reason")
            payment.refunded_at = _now()
            event = "REFUND_PAYMENT"
        else:
            return standard_response("error", "Unknown payment action.", status_code=404)
        payment.save()
        _activity(request, event, "Payment", payment.id, f"{event} {payment.payment_id}")
        return standard_response(data=serialize_payment(payment))


class AdminPaymentReceiptView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def get(self, request, pk):
        payment = get_object_or_404(Payment, id=pk)
        return _file_response(export_to_pdf(str(serialize_payment(payment))), f"{payment.payment_id or payment.id}.pdf", "application/pdf")


class AdminPaymentSpecialView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def get(self, request, kind):
        today = timezone.now().date()
        if kind == "summary":
            verified = Payment.objects.filter(status="verified")
            return standard_response(data={
                "today_amount": str(verified.filter(payment_date=today).aggregate(total=Sum("amount"))["total"] or 0),
                "today_count": verified.filter(payment_date=today).count(),
                "month_amount": str(verified.filter(payment_date__year=today.year, payment_date__month=today.month).aggregate(total=Sum("amount"))["total"] or 0),
                "year_amount": str(verified.filter(payment_date__year=today.year).aggregate(total=Sum("amount"))["total"] or 0),
                "pending_count": Payment.objects.filter(status="pending").count(),
            })
        if kind == "pending":
            qs = Payment.objects.filter(status="pending")
        else:
            active_ids = Membership.objects.filter(status="active").values_list("student_id", flat=True)
            qs = Payment.objects.exclude(student_id__in=active_ids)
        return standard_response(data=[serialize_payment(item) for item in qs.select_related("student")])


class AdminSeatsLayoutView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def get(self, request):
        _ensure_floor_rows()
        floors = Floor.objects.prefetch_related("rows__seats__student__student_profile").all()
        return standard_response(data=[serialize_floor(floor) for floor in floors])


def _ensure_floor_rows():
    for seat in Seat.objects.filter(row_ref__isnull=True):
        floor, _ = Floor.objects.get_or_create(name=seat.floor, defaults={"order": Floor.objects.count()})
        row, _ = SeatRow.objects.get_or_create(floor=floor, label=seat.row, defaults={"order": SeatRow.objects.filter(floor=floor).count()})
        seat.row_ref = row
        seat.save(update_fields=["row_ref"])


class AdminSeatsView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def get(self, request):
        _ensure_floor_rows()
        seats = Seat.objects.select_related("student", "student__student_profile", "row_ref").all().order_by("floor", "row", "seat_number")
        return _paginate(request, [serialize_seat(seat) for seat in seats])

    def post(self, request):
        floor_name = request.data.get("floor") or request.data.get("floor_name") or "Ground"
        row_label = request.data.get("row") or request.data.get("row_label") or "A"
        floor, _ = Floor.objects.get_or_create(name=floor_name)
        row, _ = SeatRow.objects.get_or_create(floor=floor, label=row_label)
        seat = Seat.objects.create(
            row_ref=row,
            floor=floor.name,
            row=row.label,
            seat_number=request.data.get("seat_number"),
            status=(request.data.get("status", "available")).lower(),
            notes=request.data.get("notes"),
        )
        return standard_response(data=serialize_seat(seat), status_code=201)


class AdminSeatDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def get(self, request, pk):
        seat = get_object_or_404(Seat.objects.select_related("student", "student__student_profile", "row_ref"), id=pk)
        return standard_response(data=serialize_seat(seat))

    def put(self, request, pk):
        seat = get_object_or_404(Seat, id=pk)
        for field in ["seat_number", "notes"]:
            if field in request.data:
                setattr(seat, field, request.data[field])
        seat.save()
        return standard_response(data=serialize_seat(seat))


class FloorView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def post(self, request):
        floor = Floor.objects.create(name=request.data.get("name"), description=request.data.get("description"), order=request.data.get("order", 0))
        return standard_response(data=serialize_floor(floor), status_code=201)

    def put(self, request, pk):
        floor = get_object_or_404(Floor, id=pk)
        for field in ["name", "description", "order", "is_active"]:
            if field in request.data:
                setattr(floor, field, request.data[field])
        floor.save()
        return standard_response(data=serialize_floor(floor))

    def delete(self, request, pk):
        floor = get_object_or_404(Floor, id=pk)
        if floor.rows.exists():
            return standard_response("error", "Floor is not empty.", status_code=400)
        floor.delete()
        return standard_response(message="Floor deleted.")


class RowView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def post(self, request):
        row = SeatRow.objects.create(floor_id=request.data.get("floor_id"), label=request.data.get("label"), order=request.data.get("order", 0))
        return standard_response(data=serialize_row(row), status_code=201)

    def put(self, request, pk):
        row = get_object_or_404(SeatRow, id=pk)
        for field in ["label", "order"]:
            if field in request.data:
                setattr(row, field, request.data[field])
        row.save()
        return standard_response(data=serialize_row(row))

    def delete(self, request, pk):
        row = get_object_or_404(SeatRow, id=pk)
        if row.seats.exists():
            return standard_response("error", "Row is not empty.", status_code=400)
        row.delete()
        return standard_response(message="Row deleted.")


class SeatActionView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def patch(self, request, pk, action):
        return self._handle(request, pk, action)

    def post(self, request, pk, action):
        return self._handle(request, pk, action)

    def _handle(self, request, pk, action):
        from django.db import transaction
        from django.db.utils import OperationalError
        
        try:
            with transaction.atomic():
                # Lock the seat to prevent concurrent modifications. Remove nowait to queue instead of failing,
                # or keep it and catch the error. We will use wait to serialize assignments gracefully.
                seat = get_object_or_404(Seat.objects.select_for_update(), id=pk)
                
                if action == "status":
                    new_status = request.data.get("status", seat.status).lower()
                    if new_status not in ['available', 'occupied', 'reserved']:
                        return standard_response("error", "Invalid status.", status_code=400)
                    seat.status = new_status
                    if new_status == 'available':
                        seat.student = None
                    seat.save()
                    SeatChangeLog.objects.create(seat=seat, student=seat.student, action="STATUS", changed_by=_admin_user(request), reason=request.data.get("reason"))
                    
                elif action == "assign":
                    student_identifier = request.data.get("student_id")
                    student = None
                    if str(student_identifier).isdigit():
                        student = User.objects.filter(id=student_identifier, role="student").first()
                    if not student:
                        profile = StudentProfile.objects.filter(student_id__iexact=student_identifier).first()
                        if profile:
                            student = profile.user
                            
                    if not student:
                        return standard_response("error", "Student not found.", status_code=404)
                    
                    # Realtime check: prevent double booking the seat
                    if seat.student and seat.student.id != student.id:
                        return standard_response("error", f"Seat is already occupied by another student.", status_code=400)
                    
                    # Prevent a student from having multiple seats assigned (one seat one time)
                    previous_seats = Seat.objects.select_for_update().filter(student=student)
                    previous = previous_seats.exclude(id=seat.id).first()
                    
                    for prev in previous_seats:
                        if prev.id != seat.id:
                            prev.student = None
                            prev.status = "available"
                            prev.save()
                            
                    seat.student = student
                    seat.status = "occupied"
                    seat.assigned_at = _now()
                    seat.assigned_by = _admin_user(request)
                    seat.save()
                    
                    SeatAssignment.objects.filter(student=student, released_date__isnull=True).update(released_date=timezone.now().date())
                    SeatAssignment.objects.create(student=student, seat=seat)
                    SeatChangeLog.objects.create(seat=seat, student=student, action="ASSIGNED", changed_by=_admin_user(request), previous_seat=previous)
                    
                elif action == "unassign":
                    student = seat.student
                    seat.student = None
                    seat.status = "available"
                    seat.save()
                    if student:
                        SeatAssignment.objects.filter(student=student, seat=seat, released_date__isnull=True).update(released_date=timezone.now().date())
                    SeatChangeLog.objects.create(seat=seat, student=student, action="UNASSIGNED", changed_by=_admin_user(request), reason=request.data.get("reason"))
                    
                else:
                    return standard_response("error", "Unknown seat action.", status_code=404)
                    
                return standard_response(data=serialize_seat(seat))
        except OperationalError:
            return standard_response("error", "System is busy processing this seat. Please try again.", status_code=409)


class SeatSpecialView(APIView):
    permission_classes = [HasAdminPermission("manage_seats")]

    def get(self, request, kind, pk=None):
        if kind == "available":
            return standard_response(data=[serialize_seat(seat) for seat in Seat.objects.filter(status="available")])
        if kind == "history":
            return standard_response(data=[{
                "id": item.id,
                "seat": item.seat_id,
                "student": item.student_id,
                "student_name": _full_name(item.student) if item.student else None,
                "assigned_date": item.assigned_date,
                "released_date": item.released_date,
            } for item in SeatAssignment.objects.filter(seat_id=pk).order_by('-assigned_date')])
        summary = []
        for floor in Floor.objects.all():
            seats = Seat.objects.filter(floor=floor.name)
            summary.append({
                "floor": floor.name,
                "total": seats.count(),
                "occupied": seats.filter(status="occupied").count(),
                "available": seats.filter(status="available").count(),
                "reserved": seats.filter(status="reserved").count(),
            })
        return standard_response(data=summary)


class AdminNotificationsView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]

    def get(self, request):
        return _paginate(request, [serialize_notification(item) for item in Notification.objects.all().order_by("-created_at")])


class AdminNotificationSendView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def post(self, request):
        if not request.data.get("title") or not request.data.get("body"):
            return standard_response("error", "Notification title and body are required.", errors={"title": ["Required."], "body": ["Required."]}, status_code=400)
        notification = _create_notification(request, send_now=True)
        return standard_response(data=serialize_notification(notification), status_code=201)


class AdminNotificationDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]

    def get(self, request, pk):
        return standard_response(data=serialize_notification(get_object_or_404(Notification, id=pk)))


class AdminNotificationRecipientsView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]

    def get(self, request, pk):
        rows = []
        for recipient in StudentNotification.objects.filter(notification_id=pk).select_related("student"):
            rows.append({
                "id": recipient.id,
                "student": recipient.student_id,
                "student_name": _full_name(recipient.student),
                "is_read": recipient.is_read,
                "push_delivered": recipient.push_delivered,
                "email_delivered": recipient.email_delivered,
                "sms_delivered": recipient.sms_delivered,
                "delivered_at": recipient.delivered_at,
                "read_at": recipient.read_at,
            })
        return standard_response(data=rows)


class AdminNotificationScheduleView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def post(self, request):
        if not request.data.get("title") or not request.data.get("body"):
            return standard_response("error", "Notification title and body are required.", errors={"title": ["Required."], "body": ["Required."]}, status_code=400)
        notification = _create_notification(request, send_now=False)
        return standard_response(data=serialize_notification(notification), status_code=201)


class AdminNotificationScheduledView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]

    def get(self, request):
        return standard_response(data=[serialize_notification(item) for item in Notification.objects.filter(scheduled_at__isnull=False, sent_at__isnull=True)])

    def delete(self, request, pk):
        get_object_or_404(Notification, id=pk, sent_at__isnull=True).delete()
        return standard_response(message="Scheduled notification cancelled.")


class AdminNotificationTemplatesView(APIView):
    permission_classes = [HasAdminPermission("manage_notifications")]

    def get(self, request):
        return standard_response(data=[])


def _create_notification(request, send_now):
    from apps.notifications.models import NotificationImage
    data = request.data
    audience = data.get("audience", "all").lower()
    
    notification = Notification.objects.create(
        title=data.get("title"),
        body=data.get("body"),
        subtitle=data.get("subtitle", ""),
        description=data.get("description", ""),
        link_url=data.get("link_url", ""),
        link_button_text=data.get("link_button_text", ""),
        event_date=data.get("event_date") if data.get("event_date") else None,
        layout=data.get("layout", "text_only"),
        background_image=_image_upload(request, "background_image"),
        audience=audience,
        display_mode=data.get("display_mode", "persistent"),
        recurring_time=data.get("recurring_time") if data.get("recurring_time") else None,
        expires_at=data.get("expires_at") if data.get("expires_at") else None,

        type=data.get("type", "GENERAL"),
        target=audience.upper(), # Map audience to target logic
        target_group=audience,
        send_push=bool(data.get("send_push", True)),
        send_email=bool(data.get("send_email", False)),
        send_sms=bool(data.get("send_sms", False)),
        scheduled_at=data.get("scheduled_at"),
        sent_at=_now() if send_now else None,
        created_by=_admin_user(request),
    )

    # Process image gallery (images[] or images depending on FormData structure)
    images = request.FILES.getlist("images") or request.FILES.getlist("images[]")
    for i, img in enumerate(images):
        NotificationImage.objects.create(notification=notification, image=img, sort_order=i)

    if send_now:
        students = _notification_students(notification, data.get("selected_students", []))
        notification.total_recipients = students.count()
        notification.success_count = students.count()
        notification.save(update_fields=["total_recipients", "success_count"])
        for student in students:
            StudentNotification.objects.create(
                student=student,
                notification=notification,
                push_delivered=notification.send_push,
                email_delivered=notification.send_email,
                sms_delivered=notification.send_sms,
                delivered_at=_now(),
            )
    _activity(request, "SEND_NOTIFICATION" if send_now else "SCHEDULE_NOTIFICATION", "Notification", notification.id, notification.title)
    return notification


def _notification_students(notification, selected_ids=None):
    qs = User.objects.filter(role="student")
    
    # Old logic
    if notification.target_group in ["active", "live"] or notification.status_filter == "LIVE":
        qs = qs.filter(student_profile__status="LIVE")
    if notification.target_group == "expired" or notification.status_filter == "EXPIRED":
        qs = qs.filter(student_profile__status="EXPIRED")
    if notification.goal_filter:
        qs = qs.filter(student_profile__goal=notification.goal_filter)

    # New audience logic
    audience = notification.audience
    if audience == "new":
        seven_days_ago = timezone.now() - datetime.timedelta(days=7)
        qs = qs.filter(date_joined__gte=seven_days_ago)
    elif audience == "premium":
        qs = qs.filter(student_profile__status="LIVE")
    elif audience == "non_premium":
        # Never had plan OR expired
        qs = qs.exclude(student_profile__status="LIVE")
    elif audience == "expired":
        qs = qs.filter(student_profile__status="EXPIRED")
    elif audience == "selected" and selected_ids:
        # selected_ids could be string of comma separated IDs if sent via FormData
        if isinstance(selected_ids, str):
            try:
                selected_ids = [int(id.strip()) for id in selected_ids.split(",") if id.strip()]
            except ValueError:
                selected_ids = []
        elif isinstance(selected_ids, list) and len(selected_ids) == 1 and isinstance(selected_ids[0], str) and ',' in selected_ids[0]:
            try:
                selected_ids = [int(id.strip()) for id in selected_ids[0].split(",") if id.strip()]
            except ValueError:
                pass
        
        if selected_ids:
            qs = qs.filter(id__in=selected_ids)
        else:
            qs = qs.none() # if selected but no valid IDs, target none

    return qs.distinct()


def _notify_holiday(holiday, action="added", admin_user=None):
    if action == "added":
        title = f"Holiday Announcement: {holiday.title}"
        body = f"The library will be closed on {holiday.date.strftime('%B %d, %Y')} due to {holiday.title}. {holiday.description}".strip()
    elif action == "cancelled":
        title = f"Holiday Cancelled: {holiday.title}"
        body = f"The previously announced holiday on {holiday.date.strftime('%B %d, %Y')} ({holiday.title}) has been cancelled. The library will be open as usual."
    else:
        return

    notification = Notification.objects.create(
        title=title,
        body=body,
        type="HOLIDAY",
        target="ALL",
        target_group="all",
        send_push=True,
        sent_at=_now(),
        created_by=admin_user,
    )
    students = _notification_students(notification)
    notification.total_recipients = students.count()
    notification.success_count = students.count()
    notification.save(update_fields=["total_recipients", "success_count"])
    for student in students:
        StudentNotification.objects.create(
            student=student,
            notification=notification,
            push_delivered=True,
            delivered_at=_now(),
        )

    if action == "added":
        reminder_time = timezone.make_aware(datetime.datetime.combine(holiday.date - datetime.timedelta(days=1), datetime.time(9, 0)))
        if reminder_time > _now():
            Notification.objects.create(
                title="Reminder: Holiday Tomorrow",
                body=f"Tomorrow ({holiday.date.strftime('%B %d, %Y')}) is a holiday: {holiday.title}.",
                type="HOLIDAY",
                target="ALL",
                target_group="all",
                send_push=True,
                scheduled_at=reminder_time,
                created_by=admin_user,
            )


class LibraryInfoAdminView(APIView):
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get_permissions(self):
        if self.request.method == "GET":
            return [AllowAny()]
        return [HasAdminPermission("manage_library")()]

    def get(self, request):
        info = _library_info()
        return standard_response(data=serialize_library_info(info) if info else None)

    def put(self, request):
        info = _library_info(create=True)
        for field in [
            "name", "tagline", "description", "address", "phone_primary", "phone_secondary",
            "email", "website", "open_time", "close_time", "off_days", "rules", "facilities",
            "about", "google_maps_url", "instagram_url", "facebook_url",
        ]:
            if field in request.data:
                value = request.data[field]
                if field == "off_days" and isinstance(value, str):
                    try:
                        value = json.loads(value)
                    except json.JSONDecodeError:
                        value = []
                setattr(info, field, value)
        image = _image_upload(request, "feature_image", "image", "photo")
        if image:
            info.feature_image = image
        info.save()
        return standard_response(data=serialize_library_info(info))


def _library_info(create=False):
    info = LibraryInfo.objects.first()
    if not info and create:
        info = LibraryInfo.objects.create(
            name="",
            rules="",
            facilities="",
            about="",
        )
    return info


class PublicFacilitiesView(APIView):
    permission_classes = [AllowAny]

    def get(self, request):
        return standard_response(data=[serialize_facility(item) for item in Facility.objects.filter(is_active=True)])


class AdminFacilitiesView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]

    def get(self, request):
        return standard_response(data=[serialize_facility(item) for item in Facility.objects.all()])

    def post(self, request):
        facility = Facility.objects.create(
            name=request.data.get("name"),
            icon_key=request.data.get("icon_key", ""),
            description=request.data.get("description"),
            is_active=request.data.get("is_active", "true").lower() == "true" if isinstance(request.data.get("is_active"), str) else request.data.get("is_active", True),
            order=request.data.get("order", 0),
        )
        if "image" in request.FILES:
            facility.image = request.FILES["image"]
            facility.save()
        return standard_response(data=serialize_facility(facility), status_code=201)


class AdminFacilityDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]

    def put(self, request, pk):
        facility = get_object_or_404(Facility, id=pk)
        for field in ["name", "icon_key", "description", "order"]:
            if field in request.data:
                setattr(facility, field, request.data[field])
        if "is_active" in request.data:
            val = request.data["is_active"]
            facility.is_active = val.lower() == "true" if isinstance(val, str) else val
        if "image" in request.FILES:
            facility.image = request.FILES["image"]
        facility.save()
        return standard_response(data=serialize_facility(facility))

    def delete(self, request, pk):
        get_object_or_404(Facility, id=pk).delete()
        return standard_response(message="Facility deleted.")


class AdminFacilityToggleView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]

    def patch(self, request, pk=None):
        if pk:
            facility = get_object_or_404(Facility, id=pk)
            facility.is_active = request.data.get("is_active", not facility.is_active)
            facility.save()
            return standard_response(data=serialize_facility(facility))
        for item in request.data.get("items", []):
            Facility.objects.filter(id=item.get("id")).update(order=item.get("order", 0))
        return standard_response(message="Facilities reordered.")


class AchieversPublicView(APIView):
    permission_classes = [AllowAny]

    def get(self, request, featured=False):
        qs = Achiever.objects.filter(is_active=True)
        if featured:
            qs = qs.filter(is_featured=True)
        return standard_response(data=[serialize_achiever(item) for item in qs])


class AdminAchieversView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get(self, request):
        return standard_response(data=[serialize_achiever(item) for item in Achiever.objects.all()])

    def post(self, request):
        achiever = Achiever.objects.create(
            name=request.data.get("name"),
            goal=request.data.get("goal"),
            achievement=request.data.get("achievement", ""),
            year=request.data.get("year", timezone.now().year),
            photo=_image_upload(request, "photo", "image"),
            is_featured=_bool(request.data.get("is_featured"), False),
            is_active=_bool(request.data.get("is_active"), True),
            order=request.data.get("order", 0),
        )
        return standard_response(data=serialize_achiever(achiever), status_code=201)


class AdminAchieverDetailView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]
    parser_classes = [JSONParser, MultiPartParser, FormParser]

    def get(self, request, pk):
        return standard_response(data=serialize_achiever(get_object_or_404(Achiever, id=pk)))

    def put(self, request, pk):
        achiever = get_object_or_404(Achiever, id=pk)
        for field in ["name", "goal", "achievement", "year", "is_featured", "is_active", "order"]:
            if field in request.data:
                value = request.data[field]
                if field in {"is_featured", "is_active"}:
                    value = _bool(value, getattr(achiever, field))
                setattr(achiever, field, value)
        image = _image_upload(request, "photo", "image")
        if image:
            achiever.photo = image
        achiever.save()
        return standard_response(data=serialize_achiever(achiever))

    def delete(self, request, pk):
        get_object_or_404(Achiever, id=pk).delete()
        return standard_response(message="Achiever deleted.")


class AdminAchieverToggleView(APIView):
    permission_classes = [HasAdminPermission("manage_library")]

    def patch(self, request, pk=None):
        if pk:
            achiever = get_object_or_404(Achiever, id=pk)
            achiever.is_active = request.data.get("is_active", not achiever.is_active)
            achiever.save()
            return standard_response(data=serialize_achiever(achiever))
        for item in request.data.get("items", []):
            Achiever.objects.filter(id=item.get("id")).update(order=item.get("order", 0))
        return standard_response(message="Achievers reordered.")


class ReviewsSummaryView(APIView):
    permission_classes = [AllowAny]

    def get(self, request):
        qs = Review.objects.filter(is_approved=True)
        return standard_response(data={
            "average_rating": qs.aggregate(avg=Avg("rating"))["avg"] or 0,
            "count": qs.count(),
            "breakdown": {rating: qs.filter(rating=rating).count() for rating in range(1, 6)},
        })


class PublicReviewsView(APIView):
    permission_classes = [AllowAny]

    def get(self, request):
        qs = Review.objects.filter(is_approved=True).select_related("student").order_by("-created_at")
        return standard_response(data=[serialize_review(item) for item in qs])


class AdminReviewsView(APIView):
    permission_classes = [HasAdminPermission("manage_reviews")]

    def get(self, request, pending=False):
        qs = Review.objects.select_related("student").all().order_by("-created_at")
        if pending:
            qs = qs.filter(is_approved=False, rejection_reason__isnull=True)
        return standard_response(data=[serialize_review(item) for item in qs])


class AdminReviewActionView(APIView):
    permission_classes = [HasAdminPermission("manage_reviews")]

    def post(self, request, pk, action):
        review = get_object_or_404(Review, id=pk)
        if action == "approve":
            review.is_approved = True
            review.rejection_reason = None
            review.approved_by = _admin_user(request)
            review.approved_at = _now()
        else:
            review.is_approved = False
            review.rejection_reason = request.data.get("reason", "Rejected")
        review.save()
        return standard_response(data=serialize_review(review))

    def delete(self, request, pk, action=None):
        get_object_or_404(Review, id=pk).delete()
        return standard_response(message="Review deleted.")


class ReportsView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request, kind, export=False):
        user = request.user
        if kind == "attendance":
            if user.role != "super_admin" and not user.permissions.get("manage_attendance"):
                return standard_response("error", "Forbidden", status_code=403)
            rows = [serialize_attendance(item) for item in Attendance.objects.select_related("student").all()]
        elif kind == "payments":
            if user.role != "super_admin" and not user.permissions.get("manage_payments"):
                return standard_response("error", "Forbidden", status_code=403)
            rows = [serialize_payment(item) for item in Payment.objects.select_related("student").all()]
        elif kind == "students":
            if user.role != "super_admin" and not user.permissions.get("manage_students"):
                return standard_response("error", "Forbidden", status_code=403)
            rows = [serialize_student(item) for item in StudentProfile.objects.select_related("user").all()]
        elif kind == "memberships":
            if user.role != "super_admin" and not user.permissions.get("manage_plans"):
                return standard_response("error", "Forbidden", status_code=403)
            rows = [serialize_membership(item) for item in Membership.objects.select_related("student", "plan").all()]
        else:
            rows = {
                "date": timezone.now().date(),
                "students": StudentProfile.objects.count(),
                "attendance": Attendance.objects.filter(date=timezone.now().date()).count(),
                "payments": str(Payment.objects.filter(payment_date=timezone.now().date()).aggregate(total=Sum("amount"))["total"] or 0),
            }
        if export:
            return _export(rows, f"{kind}.{request.query_params.get('format', 'xlsx')}")
        if isinstance(rows, list):
            return _paginate(request, rows)
        return standard_response(data=rows)


class DashboardStatsView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request, section):
        today = timezone.now().date()
        total_students = StudentProfile.objects.count()
        live = StudentProfile.objects.filter(status="LIVE").count()
        suspended = StudentProfile.objects.filter(status="SUSPENDED").count()
        expired = StudentProfile.objects.filter(status="EXPIRED").count()
        girls = StudentProfile.objects.filter(gender__iexact="Female").count()
        boys = StudentProfile.objects.filter(gender__iexact="Male").count()
        other_gender = StudentProfile.objects.exclude(gender__iexact="Female").exclude(gender__iexact="Male").count()
        present = Attendance.objects.filter(date=today, is_present=True).count()
        seats = Seat.objects.all()
        payments_today = Payment.objects.filter(payment_date=today, status="verified")
        payments_month = Payment.objects.filter(payment_date__year=today.year, payment_date__month=today.month, status="verified")
        if section == "students":
            data = {"total": total_students, "live": live, "expired": expired, "suspended": suspended, "girls": girls, "boys": boys, "other": other_gender}
        elif section == "attendance/today":
            is_pending_period = False
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
            
            pending = max(total_students - present, 0) if is_pending_period else 0
            absent = 0 if is_pending_period else max(total_students - present, 0)
            data = {"today_present": present, "today_absent": absent, "today_pending": pending, "today_total": total_students, "today_percentage": round((present / total_students * 100), 2) if total_students else 0}
        elif section == "payments/today":
            data = {"today_amount": str(payments_today.aggregate(total=Sum("amount"))["total"] or 0), "today_count": payments_today.count()}
        elif section == "payments/month":
            data = {"month_amount": str(payments_month.aggregate(total=Sum("amount"))["total"] or 0), "month_count": payments_month.count()}
        elif section == "memberships":
            data = {"active": Membership.objects.filter(status="active").count(), "expiring_in_7_days": Membership.objects.filter(end_date__lte=today + datetime.timedelta(days=7), end_date__gte=today).count(), "expired_today": Membership.objects.filter(end_date=today).count()}
        elif section == "seats":
            data = {"total": seats.count(), "occupied": seats.filter(status="occupied").count(), "available": seats.filter(status="available").count(), "reserved": seats.filter(status="reserved").count()}
        else:
            data = {
                "students": {"total": total_students, "live": live, "expired": expired, "suspended": suspended, "girls": girls, "boys": boys, "other": other_gender, "new_this_month": StudentProfile.objects.filter(created_at__year=today.year, created_at__month=today.month).count()},
                "attendance": {"today_present": present, "today_absent": max(total_students - present, 0), "today_total": total_students, "today_percentage": round((present / total_students * 100), 2) if total_students else 0},
                "payments": {"today_amount": str(payments_today.aggregate(total=Sum("amount"))["total"] or 0), "today_count": payments_today.count(), "month_amount": str(payments_month.aggregate(total=Sum("amount"))["total"] or 0), "month_count": payments_month.count(), "pending_count": Payment.objects.filter(status="pending").count()},
                "memberships": {"active": Membership.objects.filter(status="active").count(), "expiring_in_7_days": Membership.objects.filter(end_date__lte=today + datetime.timedelta(days=7), end_date__gte=today).count(), "expired_today": Membership.objects.filter(end_date=today).count()},
                "seats": {"total": seats.count(), "occupied": seats.filter(status="occupied").count(), "available": seats.filter(status="available").count(), "reserved": seats.filter(status="reserved").count()},
                "notifications": {"sent_today": Notification.objects.filter(sent_at__date=today).count(), "unread_count": StudentNotification.objects.filter(is_read=False).count()},
            }
        return standard_response(data=data)


class DashboardChartView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request, domain, chart):
        today = timezone.now().date()
        if domain == "attendance":
            labels = []
            present = []
            for offset in range(13, -1, -1):
                day = today - datetime.timedelta(days=offset)
                labels.append(day.strftime("%d %b"))
                present.append(Attendance.objects.filter(date=day, is_present=True).count())
            data = {"labels": labels, "present": present, "total_students": StudentProfile.objects.count()}
        elif domain == "revenue":
            labels = []
            revenue = []
            for offset in range(11, -1, -1):
                month = (today.replace(day=1) - datetime.timedelta(days=offset * 30)).replace(day=1)
                labels.append(month.strftime("%b %Y"))
                revenue.append(float(Payment.objects.filter(payment_date__year=month.year, payment_date__month=month.month, status="verified").aggregate(total=Sum("amount"))["total"] or 0))
            data = {"labels": labels, "revenue": revenue, "payment_count": []}
        elif domain == "students":
            data = {"items": list(StudentProfile.objects.values("goal").annotate(count=Count("id")).order_by("goal"))}
        elif domain == "memberships":
            data = {"items": [{"name": plan.name, "active": Membership.objects.filter(plan=plan, status="active").count()} for plan in MembershipPlan.objects.all()]}
        elif domain == "seats":
            data = {"items": list(Seat.objects.values("floor", "status").annotate(count=Count("id")))}
        else:
            data = {"items": []}
        return standard_response(data=data)


class DashboardActivityView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request, export=False):
        logs = ActivityLog.objects.select_related("admin").all().order_by("-timestamp")[:200]
        rows = [{
            "id": item.id,
            "admin_name": _full_name(item.admin) if item.admin else "System",
            "action": item.action,
            "description": item.details.get("description", ""),
            "target_model": item.details.get("target_model", ""),
            "target_id": item.details.get("target_id"),
            "created_at": item.timestamp,
        } for item in logs]
        if export:
            return _export(rows, "activity-log.xlsx")
        return standard_response(data=rows[:20] if request.path.endswith("/recent/") else rows)


class DashboardAlertsView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request):
        today = timezone.now().date()
        return standard_response(data=[
            {"type": "payments", "label": "Pending payments", "count": Payment.objects.filter(status="pending").count()},
            {"type": "memberships", "label": "Expiring in 7 days", "count": Membership.objects.filter(end_date__lte=today + datetime.timedelta(days=7), end_date__gte=today).count()},
            {"type": "reviews", "label": "Pending reviews", "count": Review.objects.filter(is_approved=False, rejection_reason__isnull=True).count()},
        ])


class SuperAdminAdminsView(APIView):
    permission_classes = [IsSuperAdmin]

    def get(self, request):
        return standard_response(data=[serialize_admin(admin) for admin in AdminUser.objects.all()])

    def post(self, request):
        if not request.data.get("password"):
            return standard_response("error", "Password is required.", errors={"password": ["This field is required."]}, status_code=400)
        admin = AdminUser(
            username=request.data.get("username"),
            email=request.data.get("email"),
            mobile=request.data.get("mobile"),
            first_name=request.data.get("first_name", ""),
            last_name=request.data.get("last_name", ""),
            role=request.data.get("role", "admin"),
            permissions=request.data.get("permissions", {}),
            is_active=request.data.get("is_active", True),
            created_by=_admin_user(request),
        )
        admin.set_password(request.data.get("password"))
        admin.save()
        return standard_response(data=serialize_admin(admin), status_code=201)


class SuperAdminAdminDetailView(APIView):
    permission_classes = [IsSuperAdmin]

    def put(self, request, pk):
        admin = get_object_or_404(AdminUser, id=pk)
        for field in ["first_name", "last_name", "email", "mobile", "role", "permissions", "is_active"]:
            if field in request.data:
                setattr(admin, field, request.data[field])
        if request.data.get("password"):
            admin.set_password(request.data["password"])
        admin.save()
        return standard_response(data=serialize_admin(admin))

    def delete(self, request, pk):
        get_object_or_404(AdminUser, id=pk).delete()
        return standard_response(message="Admin deleted.")


class SuperAdminDeactivateView(APIView):
    permission_classes = [IsSuperAdmin]

    def post(self, request, pk):
        admin = get_object_or_404(AdminUser, id=pk)
        admin.is_active = False
        admin.save(update_fields=["is_active"])
        return standard_response(data=serialize_admin(admin))


class SuperAdminToolsView(APIView):
    permission_classes = [IsSuperAdmin]

    def get(self, request, kind):
        if kind == "permissions":
            keys = [
                "manage_students",
                "manage_attendance",
                "manage_plans",
                "manage_payments",
                "manage_notifications",
                "manage_library",
                "manage_seats",
                "manage_holidays",
                "manage_reviews"
            ]
            return standard_response(data=[{"key": key, "label": key.replace("_", " ").title()} for key in keys])
        if kind == "backup/list":
            return standard_response(data=[])
        if kind == "activity-log":
            return DashboardActivityView().get(request)
        try:
            with connection.cursor() as cursor:
                cursor.execute("SELECT 1")
                cursor.fetchone()
            database = {"connected": True, "vendor": connection.vendor}
        except Exception as exc:
            database = {"connected": False, "error": str(exc)}
        return standard_response(data={"database": database, "checked_at": timezone.now()})

    def post(self, request, kind):
        if kind == "permissions/assign":
            admin = get_object_or_404(AdminUser, id=request.data.get("admin_id"))
            admin.permissions = request.data.get("permissions", {})
            admin.save(update_fields=["permissions"])
            return standard_response(data=serialize_admin(admin))
        if kind == "backup/create":
            return standard_response("error", "Backup service is not configured.", status_code=501)
        if kind == "backup/restore":
            return standard_response("error", "Backup restore service is not configured.", status_code=501)
        return standard_response("error", "Unknown super admin action.", status_code=404)


class AdminSettingsView(APIView):
    permission_classes = [IsLibraryAdmin]

    def get(self, request):
        from apps.library.models import LibraryInfo, AppConfig
        lib_info = LibraryInfo.objects.first()
        keys = ["attendance_padding_time"]
        settings = {s.key: s.value for s in GlobalSetting.objects.filter(key__in=keys)}
        config = AppConfig.get_solo()
        return standard_response(data={
            "library_open_time": lib_info.open_time.strftime('%H:%M') if lib_info and lib_info.open_time else "08:00",
            "attendance_padding_time": settings.get("attendance_padding_time", "60"),
            "is_premium_gating_enabled": config.is_premium_gating_enabled,
            "expiry_dialog_title": config.expiry_dialog_title,
            "expiry_dialog_message": config.expiry_dialog_message,
            "allow_non_premium_notifications": config.allow_non_premium_notifications,
            "allow_non_premium_library_info": config.allow_non_premium_library_info,
        })

    def put(self, request):
        from apps.library.models import LibraryInfo, AppConfig
        lib_info = LibraryInfo.objects.first()
        keys = ["attendance_padding_time"]
        for key in keys:
            if key in request.data:
                GlobalSetting.objects.update_or_create(key=key, defaults={"value": str(request.data[key])})
        settings = {s.key: s.value for s in GlobalSetting.objects.filter(key__in=keys)}

        config = AppConfig.get_solo()
        if "is_premium_gating_enabled" in request.data:
            config.is_premium_gating_enabled = bool(request.data["is_premium_gating_enabled"])
        if "expiry_dialog_title" in request.data:
            config.expiry_dialog_title = request.data["expiry_dialog_title"]
        if "expiry_dialog_message" in request.data:
            config.expiry_dialog_message = request.data["expiry_dialog_message"]
        if "allow_non_premium_notifications" in request.data:
            config.allow_non_premium_notifications = bool(request.data["allow_non_premium_notifications"])
        if "allow_non_premium_sliders" in request.data:
            config.allow_non_premium_sliders = bool(request.data["allow_non_premium_sliders"])
        if "allow_non_premium_library_info" in request.data:
            config.allow_non_premium_library_info = bool(request.data["allow_non_premium_library_info"])
        config.save()

        return standard_response(data={
            "library_open_time": lib_info.open_time.strftime('%H:%M') if lib_info and lib_info.open_time else "08:00",
            "attendance_padding_time": settings.get("attendance_padding_time", "60"),
            "is_premium_gating_enabled": config.is_premium_gating_enabled,
            "expiry_dialog_title": config.expiry_dialog_title,
            "expiry_dialog_message": config.expiry_dialog_message,
            "allow_non_premium_notifications": config.allow_non_premium_notifications,
            "allow_non_premium_sliders": getattr(config, 'allow_non_premium_sliders', True),
            "allow_non_premium_library_info": config.allow_non_premium_library_info,
        })


def serialize_slider(s, request=None):
    image_url = s.image.url if s.image else None
    if image_url and request:
        image_url = request.build_absolute_uri(image_url)
    return {
        "id": s.id,
        "title": s.title,
        "subtitle": s.subtitle,
        "image": image_url,
        "link_url": s.link_url,
        "is_active": s.is_active,
        "sort_order": s.sort_order,
        "created_at": s.created_at.isoformat() if s.created_at else None,
    }


class AdminSlidersView(APIView):
    permission_classes = [IsAuthenticated, IsLibraryAdmin | HasAdminPermission('manage_library')]
    parser_classes = [MultiPartParser, FormParser, JSONParser]

    def get(self, request):
        sliders = HomeSlider.objects.all()
        return standard_response(data=[serialize_slider(s, request) for s in sliders])

    def post(self, request):
        try:
            data = request.data
            
            # Safely parse sort_order
            sort_order_raw = data.get('sort_order')
            try:
                sort_order = int(sort_order_raw) if sort_order_raw else 0
            except (ValueError, TypeError):
                sort_order = 0

            # Safely parse is_active
            is_active_raw = data.get('is_active', True)
            if isinstance(is_active_raw, str):
                is_active = is_active_raw.lower() in ['true', '1', 'yes']
            else:
                is_active = bool(is_active_raw)

            slider = HomeSlider.objects.create(
                title=data.get('title', ''),
                subtitle=data.get('subtitle', ''),
                link_url=data.get('link_url', ''),
                is_active=is_active,
                sort_order=sort_order,
            )
            
            if 'image' in request.FILES:
                slider.image = request.FILES['image']
                slider.save()

            _activity(
                request,
                action="CREATE",
                target_model="HomeSlider",
                target_id=str(slider.id),
                description=f"Created slider: {slider.title}"
            )
            return standard_response(data=serialize_slider(slider, request))
        except Exception as e:
            import traceback
            traceback.print_exc()
            return standard_response("error", f"Failed to create slider: {str(e)}", status_code=400)


class AdminSliderDetailView(APIView):
    permission_classes = [IsAuthenticated, IsLibraryAdmin | HasAdminPermission('manage_library')]
    parser_classes = [MultiPartParser, FormParser, JSONParser]

    def get_object(self, pk):
        return get_object_or_404(HomeSlider, pk=pk)

    def put(self, request, pk):
        try:
            slider = self.get_object(pk)
            data = request.data
            
            if 'title' in data:
                slider.title = data['title']
            if 'subtitle' in data:
                slider.subtitle = data['subtitle']
            if 'link_url' in data:
                slider.link_url = data['link_url']
            
            if 'is_active' in data:
                is_active_raw = data['is_active']
                if isinstance(is_active_raw, str):
                    slider.is_active = is_active_raw.lower() in ['true', '1', 'yes']
                else:
                    slider.is_active = bool(is_active_raw)
                    
            if 'sort_order' in data:
                try:
                    slider.sort_order = int(data['sort_order']) if data['sort_order'] else 0
                except (ValueError, TypeError):
                    slider.sort_order = 0
                
            if 'image' in request.FILES:
                slider.image = request.FILES['image']
                
            slider.save()
            
            _activity(
                request,
                action="UPDATE",
                target_model="HomeSlider",
                target_id=str(slider.id),
                description=f"Updated slider: {slider.title}"
            )
            return standard_response(data=serialize_slider(slider, request))
        except Exception as e:
            import traceback
            traceback.print_exc()
            return standard_response("error", f"Failed to update slider: {str(e)}", status_code=400)

    def delete(self, request, pk):
        slider = self.get_object(pk)
        title = slider.title
        slider.delete()
        
        _activity(
            request,
            action="DELETE",
            target_model="HomeSlider",
            target_id=str(pk),
            description=f"Deleted slider: {title}"
        )
        return standard_response(message="Slider deleted")


class PublicSlidersView(APIView):
    permission_classes = [AllowAny]

    def get(self, request):
        sliders = HomeSlider.objects.filter(is_active=True)
        return standard_response(data=[serialize_slider(s, request) for s in sliders])
