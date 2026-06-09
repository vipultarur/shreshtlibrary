from rest_framework import serializers
from apps.seats.models import Seat, SeatAssignment

class SeatSerializer(serializers.ModelSerializer):
    class Meta:
        model = Seat
        fields = ['id', 'floor', 'row', 'seat_number', 'status']
        read_only_fields = ['id']


class SeatAssignmentSerializer(serializers.ModelSerializer):
    student_name = serializers.CharField(source='student.get_full_name', read_only=True)
    seat_details = serializers.CharField(source='seat.__str__', read_only=True)

    class Meta:
        model = SeatAssignment
        fields = ['id', 'student_name', 'seat_details', 'assigned_date', 'released_date']
        read_only_fields = ['id', 'student_name', 'seat_details', 'assigned_date']
