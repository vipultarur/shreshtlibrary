from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone
import datetime
from django.db import transaction

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.payments.models import Payment
from apps.memberships.models import MembershipPlan, Membership
from .serializers import PaymentSerializer

class StudentPaymentHistoryView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: PaymentSerializer(many=True)}, tags=['Payments'])
    def get(self, request):
        payments = Payment.objects.select_related('student', 'membership', 'membership__plan').filter(student=request.user).order_by('-payment_date')
        serializer = PaymentSerializer(payments, many=True)
        return standard_response(data=serializer.data)


class StudentInitiatePaymentView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(
        request=OpenApiTypes.OBJECT,
        responses={201: OpenApiTypes.OBJECT},
        tags=['Payments']
    )
    def post(self, request):
        plan_id = request.data.get('plan_id')
        payment_mode = request.data.get('payment_mode', 'UPI')
        transaction_id = request.data.get('transaction_id')
        
        try:
            duration_days = int(request.data.get('duration_days', 30))
            if duration_days < 30:
                return Response({"errors": {"duration_days": ["Minimum custom duration is 30 days."]}}, status=status.HTTP_400_BAD_REQUEST)
        except ValueError:
            return Response({"errors": {"duration_days": ["Invalid duration."]}}, status=status.HTTP_400_BAD_REQUEST)

        if not plan_id:
            return Response({"errors": {"plan_id": ["Plan ID is required."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        user = request.user
        
        # Check active plan restriction
        active_sub = Membership.objects.filter(student=user, status="active").first()
        if active_sub:
            return Response({"errors": {"plan_id": ["You already have an active subscription. You can purchase a new plan after the current one expires."]}}, status=status.HTTP_400_BAD_REQUEST)

        plan = get_object_or_404(MembershipPlan, id=plan_id, is_active=True)
        
        from decimal import Decimal
        try:
            base_duration = Decimal(str(plan.duration_days or 30))
            price_per_day = plan.price / base_duration
            amount = round(price_per_day * Decimal(str(duration_days)), 2)
        except Exception:
            return Response({"errors": {"amount": ["Failed to calculate amount."]}}, status=status.HTTP_400_BAD_REQUEST)

        # Create inactive membership to be activated on verification
        start = timezone.now().date()
        end = start + datetime.timedelta(days=duration_days)
        
        with transaction.atomic():
            membership = Membership.objects.create(
                student=user,
                plan=plan,
                start_date=start,
                end_date=end,
                status='suspended'  # Will be 'active' once payment is verified
            )

            payment = Payment.objects.create(
                student=user,
                membership=membership,
                amount=amount,
                payment_mode=payment_mode,
                transaction_id=transaction_id,
                status='pending'
            )

        try:
            from apps.notifications.models import AdminInboxNotification
            from api.v1.v2_admin import _full_name
            AdminInboxNotification.objects.create(
                type='PAYMENT',
                title='New Payment Initiated',
                message=f"Student {_full_name(user)} initiated a payment of {plan.price} via {payment_mode}.",
                related_id=str(payment.id),
                student=user
            )
        except Exception:
            pass

        return standard_response(
            message="Payment transaction initiated successfully. Pending admin approval.",
            data=PaymentSerializer(payment).data,
            status_code=status.HTTP_201_CREATED
        )
