from rest_framework import serializers
from apps.payments.models import Payment

class PaymentSerializer(serializers.ModelSerializer):
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)
    plan_name = serializers.CharField(source='membership.plan.name', read_only=True)

    class Meta:
        ref_name = "StudentPayment"
        model = Payment
        fields = ['id', 'student_name', 'plan_name', 'amount', 'status', 'payment_mode', 'payment_date', 'transaction_id', 'receipt_url', 'notes']
        read_only_fields = ['id', 'student_name', 'plan_name', 'status', 'payment_date', 'receipt_url']
