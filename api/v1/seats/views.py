from rest_framework.views import APIView
from rest_framework.permissions import AllowAny, IsAuthenticated

from drf_spectacular.utils import extend_schema

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.seats.models import Seat, SeatAssignment
from .serializers import SeatSerializer, SeatAssignmentSerializer

class SeatLayoutView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(responses={200: SeatSerializer(many=True)}, tags=['Seats'])
    def get(self, request):
        seats = Seat.objects.all().order_by('floor', 'row', 'seat_number')
        serializer = SeatSerializer(seats, many=True)
        return standard_response(data=serializer.data)


class StudentSeatHistoryView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: SeatAssignmentSerializer(many=True)}, tags=['Seats'])
    def get(self, request):
        history = SeatAssignment.objects.filter(student=request.user).order_by('-assigned_date')
        serializer = SeatAssignmentSerializer(history, many=True)
        return standard_response(data=serializer.data)
