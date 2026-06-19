from decimal import Decimal
from django.db.models import Sum
from django.utils import timezone
from django.shortcuts import get_object_or_404
from rest_framework import generics, status
from rest_framework.views import APIView
from django_filters.rest_framework import DjangoFilterBackend
from django.contrib.auth import get_user_model

from apps.payments.models import Payment
from apps.memberships.models import Membership
from shreshtlibrary.utils.permissions import HasAdminPermission
from api.v1.admin.pagination import AdminStandardPagination
from api.v1.admin.serializers import PaymentSerializer
from utils.response import standard_response
from utils.exporters import export_to_pdf
from api.v1.v2_admin import _activity, _admin_user, _file_response, _now

User = get_user_model()

class AdminPaymentsView(generics.ListCreateAPIView):
    permission_classes = [HasAdminPermission("manage_payments")]
    serializer_class = PaymentSerializer
    pagination_class = AdminStandardPagination
    # Note: status and method filters are handled manually in get_queryset
    # to normalize case (frontend sends lowercase, DB stores lowercase)

    def get_queryset(self):
        qs = Payment.objects.select_related("student", "membership", "membership__plan").all().order_by("-payment_date", "-id")
        # Normalize status filter — frontend sends lowercase, DB stores lowercase
        status_filter = self.request.query_params.get("status", "").lower()
        if status_filter:
            qs = qs.filter(status=status_filter)
        student_id = self.request.query_params.get("student_id")
        if student_id:
            qs = qs.filter(student_id=student_id)
        method = self.request.query_params.get("method")
        if method:
            qs = qs.filter(payment_mode=method)
        from_date = self.request.query_params.get("from_date")
        if from_date:
            qs = qs.filter(payment_date__gte=from_date)
        return qs

    def create(self, request, *args, **kwargs):
        from apps.memberships.models import MembershipPlan
        from django.db import transaction
        import datetime
        
        student = get_object_or_404(User, id=request.data.get("student_id"), role="student")
        
        # Check active plan restriction
        active_sub = Membership.objects.filter(student=student, status="active").first()
        if active_sub:
            return standard_response("error", "Student already has an active subscription. You can assign a new plan after the current one expires.", status_code=400)
            
        plan_id = request.data.get("plan_id")
        if not plan_id:
            return standard_response("error", "Plan is required.", status_code=400)
            
        plan = get_object_or_404(MembershipPlan, id=plan_id)
        
        try:
            duration_days = int(request.data.get("duration_days", 30))
            if duration_days < 30:
                return standard_response("error", "Minimum custom duration is 30 days.", status_code=400)
        except ValueError:
            return standard_response("error", "Invalid duration.", status_code=400)
            
        # Automatic Price Calculation
        try:
            base_duration = Decimal(str(plan.duration_days or 30))
            price_per_day = plan.price / base_duration
            amount = round(price_per_day * Decimal(str(duration_days)), 2)
        except Exception:
            return standard_response("error", "Failed to calculate amount.", status_code=400)

        payment_mode = request.data.get("payment_mode") or request.data.get("method", "Cash")
        
        today = timezone.now().date()
        end_date = today + datetime.timedelta(days=duration_days)
        
        with transaction.atomic():
            membership = Membership.objects.create(
                student=student,
                plan=plan,
                start_date=today,
                end_date=end_date,
                status="active",
                renewal_count=0,
                notes=request.data.get("notes"),
                created_by=_admin_user(request)
            )
            
            payment = Payment.objects.create(
                student=student,
                membership=membership,
                amount=amount,
                payment_mode=payment_mode,
                method=payment_mode.upper().replace(" ", "_"),
                transaction_id=request.data.get("transaction_id") or request.data.get("transaction_ref"),
                transaction_ref=request.data.get("transaction_ref") or request.data.get("transaction_id"),
                notes=request.data.get("notes"),
                recorded_by=_admin_user(request),
                paid_at=_now(),
                status="pending",
            )
            
            # Update student status to LIVE
            if hasattr(student, 'student_profile'):
                student.student_profile.status = "LIVE"
                student.student_profile.save(update_fields=['status'])

        _activity(request, "RECORD_PAYMENT", "Payment", payment.id, f"Recorded payment {payment.payment_id}")

        try:
            from apps.notifications.models import AdminInboxNotification
            from api.v1.v2_admin import _full_name
            creator = _admin_user(request)
            creator_name = creator.username if creator else "Admin/Keeper"
            AdminInboxNotification.objects.create(
                type='PAYMENT',
                title='New Payment Recorded Manually',
                message=f"A payment of {payment.amount} for {_full_name(student)} was recorded by {creator_name}.",
                related_id=str(payment.id),
                student=student
            )
        except Exception:
            pass

        return standard_response(data=self.get_serializer(payment).data, status_code=201)


class AdminPaymentDetailView(generics.RetrieveUpdateAPIView):
    permission_classes = [HasAdminPermission("manage_payments")]
    serializer_class = PaymentSerializer
    queryset = Payment.objects.all()

    def retrieve(self, request, *args, **kwargs):
        return standard_response(data=self.get_serializer(self.get_object()).data)

    def update(self, request, *args, **kwargs):
        payment = self.get_object()
        serializer = self.get_serializer(payment, data=request.data, partial=True)
        if serializer.is_valid():
            payment = serializer.save()
            return standard_response(data=self.get_serializer(payment).data)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)


class AdminPaymentActionView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def post(self, request, pk, action):
        payment = get_object_or_404(Payment, id=pk)
        if action == "verify":
            if payment.membership:
                # Check for active subscription
                active_sub = Membership.objects.filter(student=payment.student, status="active").exclude(id=payment.membership.id).first()
                if active_sub:
                    return standard_response("error", "Student already has an active subscription. Cannot verify this payment and activate a new plan.", status_code=400)
            
            payment.status = "verified"
            payment.verified_by = _admin_user(request)
            payment.verified_at = _now()
            if payment.membership:
                payment.membership.status = "active"
                payment.membership.save()
                
                # Update student status
                if hasattr(payment.student, 'student_profile'):
                    payment.student.student_profile.status = "LIVE"
                    payment.student.student_profile.save(update_fields=['status'])
                    
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
        return standard_response(data=PaymentSerializer(payment).data)


class AdminPaymentReceiptView(APIView):
    permission_classes = [HasAdminPermission("manage_payments")]

    def get(self, request, pk):
        payment = get_object_or_404(Payment, id=pk)
        data = PaymentSerializer(payment).data
        receipt_text = (
            f"PAYMENT RECEIPT\n"
            f"{'=' * 40}\n"
            f"Receipt No : {data.get('payment_id') or data.get('id')}\n"
            f"Student    : {data.get('student_name')}\n"
            f"Plan       : {data.get('plan_name') or 'N/A'}\n"
            f"Amount     : {data.get('amount')}\n"
            f"Status     : {data.get('status', '').upper()}\n"
            f"Mode       : {data.get('payment_mode')}\n"
            f"Date       : {data.get('payment_date')}\n"
            f"Txn Ref    : {data.get('transaction_ref') or data.get('transaction_id') or 'N/A'}\n"
            f"Notes      : {data.get('notes') or 'N/A'}\n"
            f"{'=' * 40}\n"
        )
        return _file_response(export_to_pdf(receipt_text), f"{payment.payment_id or payment.id}.pdf", "application/pdf")


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
            # Overdue = pending payments not actioned within 3 days
            from datetime import timedelta
            overdue_cutoff = timezone.now() - timedelta(days=3)
            qs = Payment.objects.filter(status="pending", created_at__lte=overdue_cutoff)
        return standard_response(data=PaymentSerializer(qs.select_related("student", "membership", "membership__plan"), many=True).data)
