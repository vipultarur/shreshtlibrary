from rest_framework import serializers
from django.db.models import Sum, Count
from django.utils import timezone
from django.core.cache import cache
from rest_framework import generics, status
from rest_framework.views import APIView
import datetime

from apps.students.models import StudentProfile
from apps.attendance.models import Attendance
from apps.payments.models import Payment
from apps.seats.models import Seat
from apps.memberships.models import Membership, MembershipPlan
from apps.notifications.models import Notification, StudentNotification, AdminInboxNotification
from core.models import ActivityLog
from shreshtlibrary.utils.permissions import IsLibraryAdmin
from utils.response import standard_response
from api.v1.admin.serializers import StudentProfileSerializer
from api.v1.v2_admin import _full_name

class DashboardStatsView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsLibraryAdmin]

    def get(self, request, section):
        today = timezone.now().date()
        cache_key = f"dashboard_stats_{section}_{today}"
        cached_data = cache.get(cache_key)
        
        if cached_data:
            return standard_response(data=cached_data)

        total_students = StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).count()
        present = Attendance.objects.filter(date=today, is_present=True).count()
        
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

        data = {}
        if section == "students":
            data = {
                "total": total_students,
                "live": StudentProfile.objects.filter(status="LIVE").count(),
                "expired": StudentProfile.objects.filter(status="EXPIRED").count(),
                "suspended": StudentProfile.objects.filter(status="SUSPENDED").count(),
                "girls": StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).filter(gender__iexact="Female").count(),
                "boys": StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).filter(gender__iexact="Male").count(),
                "other": StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).exclude(gender__iexact="Female").exclude(gender__iexact="Male").count()
            }
        elif section == "attendance/today":
            pending = max(total_students - present, 0) if is_pending_period else 0
            absent = 0 if is_pending_period else max(total_students - present, 0)
            data = {"today_present": present, "today_absent": absent, "today_pending": pending, "today_total": total_students, "today_percentage": round((present / total_students * 100), 2) if total_students else 0}
        elif section == "payments/today":
            payments_today = Payment.objects.filter(payment_date=today, status="verified")
            data = {"today_amount": str(payments_today.aggregate(total=Sum("amount"))["total"] or 0), "today_count": payments_today.count()}
        elif section == "payments/month":
            payments_month = Payment.objects.filter(payment_date__year=today.year, payment_date__month=today.month, status="verified")
            data = {"month_amount": str(payments_month.aggregate(total=Sum("amount"))["total"] or 0), "month_count": payments_month.count()}
        elif section == "memberships":
            data = {"active": Membership.objects.filter(status="active").count(), "expiring_in_7_days": Membership.objects.filter(end_date__lte=today + datetime.timedelta(days=7), end_date__gte=today).count(), "expired_today": Membership.objects.filter(end_date=today).count()}
        elif section == "seats":
            seats = Seat.objects.all()
            data = {"total": seats.count(), "occupied": seats.filter(status="occupied").count(), "available": seats.filter(status="available").count(), "reserved": seats.filter(status="reserved").count()}
        else:
            payments_today = Payment.objects.filter(payment_date=today, status="verified")
            payments_month = Payment.objects.filter(payment_date__year=today.year, payment_date__month=today.month, status="verified")
            seats = Seat.objects.all()
            data = {
                "students": {"total": total_students, "live": StudentProfile.objects.filter(status="LIVE").count(), "expired": StudentProfile.objects.filter(status="EXPIRED").count(), "suspended": StudentProfile.objects.filter(status="SUSPENDED").count(), "new_this_month": StudentProfile.objects.filter(created_at__year=today.year, created_at__month=today.month).count()},
                "attendance": {"today_present": present, "today_absent": max(total_students - present, 0), "today_total": total_students, "today_percentage": round((present / total_students * 100), 2) if total_students else 0},
                "payments": {"today_amount": str(payments_today.aggregate(total=Sum("amount"))["total"] or 0), "today_count": payments_today.count(), "month_amount": str(payments_month.aggregate(total=Sum("amount"))["total"] or 0), "month_count": payments_month.count(), "pending_count": Payment.objects.filter(status="pending").count()},
                "memberships": {"active": Membership.objects.filter(status="active").count(), "expiring_in_7_days": Membership.objects.filter(end_date__lte=today + datetime.timedelta(days=7), end_date__gte=today).count(), "expired_today": Membership.objects.filter(end_date=today).count()},
                "seats": {"total": seats.count(), "occupied": seats.filter(status="occupied").count(), "available": seats.filter(status="available").count(), "reserved": seats.filter(status="reserved").count()},
                "notifications": {"sent_today": Notification.objects.filter(sent_at__date=today).count(), "unread_count": StudentNotification.objects.filter(is_read=False).count()},
            }
        
        cache.set(cache_key, data, timeout=300) # Cache for 5 minutes
        return standard_response(data=data)

class DashboardChartView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsLibraryAdmin]

    def get(self, request, domain, chart):
        today = timezone.now().date()
        cache_key = f"dashboard_chart_{domain}_{chart}_{today}"
        cached_data = cache.get(cache_key)

        if cached_data:
            return standard_response(data=cached_data)

        if domain == "attendance":
            labels, present = [], []
            start_date = today - datetime.timedelta(days=13)
            
            from django.db.models import Count
            attendance_counts = Attendance.objects.filter(
                date__gte=start_date, date__lte=today, is_present=True
            ).values('date').annotate(count=Count('id'))
            count_map = {item['date']: item['count'] for item in attendance_counts}
            
            for offset in range(13, -1, -1):
                day = today - datetime.timedelta(days=offset)
                labels.append(day.strftime("%d %b"))
                present.append(count_map.get(day, 0))
            data = {"labels": labels, "present": present, "total_students": StudentProfile.objects.exclude(status__in=['EXPIRED', 'SUSPENDED']).count()}
        elif domain == "revenue":
            labels, revenue = [], []
            from django.db.models.functions import TruncMonth
            start_month = (today.replace(day=1) - datetime.timedelta(days=11 * 30)).replace(day=1)
            
            revenue_counts = Payment.objects.filter(
                payment_date__gte=start_month, status="verified"
            ).annotate(month=TruncMonth('payment_date')).values('month').annotate(total=Sum('amount'))
            revenue_map = {item['month'].strftime("%Y-%m"): float(item['total'] or 0) for item in revenue_counts if item['month']}
            
            for offset in range(11, -1, -1):
                month = (today.replace(day=1) - datetime.timedelta(days=offset * 30)).replace(day=1)
                month_key = month.strftime("%Y-%m")
                labels.append(month.strftime("%b %Y"))
                revenue.append(revenue_map.get(month_key, 0.0))
            data = {"labels": labels, "revenue": revenue, "payment_count": []}
        elif domain == "students":
            data = {"items": list(StudentProfile.objects.values("goal").annotate(count=Count("id")).order_by("goal"))}
        elif domain == "memberships":
            data = {"items": [{"name": plan.name, "active": Membership.objects.filter(plan=plan, status="active").count()} for plan in MembershipPlan.objects.all()]}
        elif domain == "seats":
            data = {"items": list(Seat.objects.values("floor", "status").annotate(count=Count("id")))}
        else:
            data = {"items": []}
            
        cache.set(cache_key, data, timeout=300)
        return standard_response(data=data)

class AdminInboxView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsLibraryAdmin]

    def get(self, request):
        today = timezone.now().date()
        two_days_from_now = today + datetime.timedelta(days=2)
        
        existing_expiring = set(AdminInboxNotification.objects.filter(type='EXPIRING_SOON').values_list('related_id', flat=True))
        existing_expired = set(AdminInboxNotification.objects.filter(type='EXPIRED').values_list('related_id', flat=True))

        # Auto-generate EXPIRING_SOON notifications
        # Capture memberships expiring between tomorrow and 3 days from now
        expiring_memberships = Membership.objects.filter(
            end_date__lte=today + datetime.timedelta(days=3),
            end_date__gt=today,
            status='active'
        ).select_related('student')
        
        new_notifications = []
        for mem in expiring_memberships:
            related_id = f"exp_{mem.student_id}_{mem.end_date}"
            if related_id not in existing_expiring:
                new_notifications.append(AdminInboxNotification(
                    type='EXPIRING_SOON',
                    title='Student Plan Expiring Soon',
                    message=f"Membership for {_full_name(mem.student)} is expiring on {mem.end_date}.",
                    related_id=related_id,
                    student=mem.student
                ))
                existing_expiring.add(related_id)
                
        # Auto-generate EXPIRED notifications
        # Capture memberships that have expired today or earlier, but are still marked active
        expired_memberships = Membership.objects.filter(
            end_date__lte=today,
            status='active'
        ).select_related('student')
        for mem in expired_memberships:
            related_id = f"expired_{mem.student_id}_{mem.end_date}"
            if related_id not in existing_expired:
                new_notifications.append(AdminInboxNotification(
                    type='EXPIRED',
                    title='Student Plan Expired',
                    message=f"Membership for {_full_name(mem.student)} expired on {mem.end_date}.",
                    related_id=related_id,
                    student=mem.student
                ))
                existing_expired.add(related_id)
                
        if new_notifications:
            AdminInboxNotification.objects.bulk_create(new_notifications)
                
        notifications = AdminInboxNotification.objects.select_related('student', 'student__student_profile').all().order_by('-created_at')
        
        data = []
        for n in notifications:
            student_name = _full_name(n.student) if n.student else None
            student_avatar = None
            if n.student and hasattr(n.student, 'student_profile') and n.student.student_profile.profile_photo:
                student_avatar = request.build_absolute_uri(n.student.student_profile.profile_photo.url)
                
            data.append({
                'id': n.id,
                'title': n.title,
                'message': n.message,
                'type': n.type,
                'is_read': n.is_read,
                'created_at': n.created_at,
                'related_id': n.related_id,
                'student_id': n.student.id if n.student else None,
                'student_name': student_name,
                'student_avatar': student_avatar
            })
            
        return standard_response(data=data)

class AdminInboxNotificationDetailView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsLibraryAdmin]

    def post(self, request, pk, action):
        notification = AdminInboxNotification.objects.filter(id=pk).first()
        if not notification:
            return standard_response('error', 'Notification not found', status_code=404)
            
        if action == 'read':
            notification.is_read = True
            notification.save()
            return standard_response(message='Notification marked as read')
        elif action == 'unread':
            notification.is_read = False
            notification.save()
            return standard_response(message='Notification marked as unread')
            
        return standard_response('error', 'Invalid action', status_code=400)
        
    def delete(self, request, pk):
        notification = AdminInboxNotification.objects.filter(id=pk).first()
        if notification:
            notification.delete()
        return standard_response(message='Notification deleted')

class GlobalSearchView(APIView):
    from rest_framework import serializers
    permission_classes = [IsLibraryAdmin]
    serializer_class = serializers.Serializer

    def get(self, request):
        query = request.query_params.get("q", "").strip()
        if len(query) < 2:
            return standard_response(data={"students": [], "seats": [], "payments": []})
            
        from django.db.models import Q
        
        students = StudentProfile.objects.select_related("user").filter(
            Q(user__username__icontains=query) |
            Q(user__first_name__icontains=query) |
            Q(user__last_name__icontains=query) |
            Q(user__mobile__icontains=query) |
            Q(student_id__icontains=query) |
            Q(parent_mobile__icontains=query)
        ).exclude(status__in=['EXPIRED', 'SUSPENDED'])[:10]
        
        seats = Seat.objects.filter(
            Q(seat_number__icontains=query) |
            Q(floor__icontains=query) |
            Q(row__icontains=query)
        )[:10]
        
        payments = Payment.objects.filter(
            Q(transaction_id__icontains=query) |
            Q(student__first_name__icontains=query) |
            Q(student__last_name__icontains=query) |
            Q(student__username__icontains=query)
        )[:10]

        from api.v1.admin.serializers import PaymentSerializer
        from api.v1.v2_admin import AdminSeatSerializer
        
        return standard_response(data={
            "students": StudentProfileSerializer(students, many=True, context={'request': request}).data,
            "seats": AdminSeatSerializer(seats, many=True, context={'request': request}).data,
            "payments": PaymentSerializer(payments, many=True, context={'request': request}).data
        })
