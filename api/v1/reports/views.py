from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.utils import timezone
from django.db.models import Count

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsLibraryAdmin
from utils.response import standard_response
from apps.attendance.models import Attendance
from apps.payments.models import Payment
from apps.seats.models import Seat

class AdminAttendanceReportView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Reports'])
    def get(self, request):
        # Query parameters for date range
        start_date = request.query_param.get('start_date')
        end_date = request.query_param.get('end_date')

        queryset = Attendance.objects.all()
        if start_date:
            queryset = queryset.filter(date__gte=start_date)
        if end_date:
            queryset = queryset.filter(date__lte=end_date)

        records = [{
            "id": r.id,
            "student_name": r.student.get_full_name() or r.student.username,
            "date": r.date,
            "time_in": r.time_in,
            "is_manual": r.is_manual
        } for r in queryset.order_by('-date')]

        return standard_response(data={"attendance_report": records})


class AdminPaymentReportView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Reports'])
    def get(self, request):
        queryset = Payment.objects.all()
        records = [{
            "id": r.id,
            "student_name": r.student.get_full_name() or r.student.username,
            "plan_name": r.membership.plan.name if r.membership else "N/A",
            "amount": r.amount,
            "status": r.status,
            "payment_mode": r.payment_mode,
            "payment_date": r.payment_date,
            "transaction_id": r.transaction_id
        } for r in queryset.order_by('-payment_date')]

        return standard_response(data={"payment_report": records})


class AdminSeatOccupancyReportView(APIView):
    permission_classes = [IsLibraryAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Reports'])
    def get(self, request):
        total_seats = Seat.objects.count()
        occupied_seats = Seat.objects.filter(status='occupied').count()
        reserved_seats = Seat.objects.filter(status='reserved').count()
        available_seats = total_seats - occupied_seats - reserved_seats

        occupancy_by_floor = Seat.objects.values('floor', 'status').annotate(count=Count('id'))

        data = {
            "summary": {
                "total_seats": total_seats,
                "occupied_seats": occupied_seats,
                "reserved_seats": reserved_seats,
                "available_seats": available_seats,
                "occupancy_rate": f"{round((occupied_seats / total_seats * 100), 2) if total_seats > 0 else 0}%"
            },
            "floor_details": list(occupancy_by_floor)
        }
        return standard_response(data=data)
