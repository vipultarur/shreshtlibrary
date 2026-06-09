from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404
from django.utils import timezone
import datetime

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
        payments = Payment.objects.filter(student=request.user).order_by('-payment_date')
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

        if not plan_id:
            return Response({"errors": {"plan_id": ["Plan ID is required."]}}, status=status.HTTP_400_BAD_REQUEST)
        
        plan = get_object_or_404(MembershipPlan, id=plan_id, is_active=True)
        user = request.user

        # Create inactive membership to be activated on verification
        start = timezone.now().date()
        end = start + datetime.timedelta(days=30 * plan.duration_months)
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
            amount=plan.price,
            payment_mode=payment_mode,
            transaction_id=transaction_id,
            status='pending'
        )

        return standard_response(
            message="Payment transaction initiated successfully. Pending admin approval.",
            data=PaymentSerializer(payment).data,
            status_code=status.HTTP_201_CREATED
        )
