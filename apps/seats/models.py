from django.db import models
from django.conf import settings

class Seat(models.Model):
    STATUS_CHOICES = (
        ('available', 'Available'),
        ('occupied', 'Occupied'),
        ('reserved', 'Reserved'),
    )
    floor = models.CharField(max_length=50)
    row = models.CharField(max_length=10)
    seat_number = models.CharField(max_length=10)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='available')

    class Meta:
        unique_together = ('floor', 'row', 'seat_number')

    def __str__(self):
        return f"Floor: {self.floor}, Row: {self.row}, Seat: {self.seat_number} ({self.status})"


class SeatAssignment(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='seat_assignments')
    seat = models.ForeignKey('seats.Seat', on_delete=models.CASCADE, related_name='assignments')
    assigned_date = models.DateField(auto_now_add=True)
    released_date = models.DateField(null=True, blank=True)

    def __str__(self):
        return f"{self.student.username} assigned to Seat {self.seat.seat_number} on {self.assigned_date}"
