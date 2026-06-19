from django.db import models
from django.conf import settings


class Floor(models.Model):
    name = models.CharField(max_length=50, unique=True)
    description = models.TextField(null=True, blank=True)
    order = models.IntegerField(default=0)
    is_active = models.BooleanField(default=True)

    class Meta:
        ordering = ['order', 'name']

    def __str__(self):
        return self.name


class SeatRow(models.Model):
    floor = models.ForeignKey('seats.Floor', on_delete=models.CASCADE, related_name='rows')
    label = models.CharField(max_length=10)
    order = models.IntegerField(default=0)

    class Meta:
        unique_together = ('floor', 'label')
        ordering = ['floor__order', 'order', 'label']

    def __str__(self):
        return f"{self.floor.name} - {self.label}"


class Seat(models.Model):
    STATUS_CHOICES = (
        ('available', 'Available'),
        ('occupied', 'Occupied'),
        ('reserved', 'Reserved'),
        ('inactive', 'Inactive'),
    )
    row_ref = models.ForeignKey('seats.SeatRow', on_delete=models.SET_NULL, null=True, blank=True, related_name='seats')
    floor = models.CharField(max_length=50)
    row = models.CharField(max_length=10)
    seat_number = models.CharField(max_length=10)
    status = models.CharField(max_length=20, choices=STATUS_CHOICES, default='available')
    student = models.OneToOneField(settings.AUTH_USER_MODEL, on_delete=models.SET_NULL, null=True, blank=True, related_name='current_seat')
    assigned_at = models.DateTimeField(null=True, blank=True)
    assigned_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True, related_name='assigned_seats')
    is_reserved_for_girls = models.BooleanField(default=False)
    notes = models.TextField(null=True, blank=True)

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


class SeatChangeLog(models.Model):
    seat = models.ForeignKey('seats.Seat', on_delete=models.CASCADE, related_name='change_logs')
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.SET_NULL, null=True, blank=True)
    action = models.CharField(max_length=30)
    changed_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True)
    previous_seat = models.ForeignKey('seats.Seat', on_delete=models.SET_NULL, null=True, blank=True, related_name='move_logs')
    reason = models.TextField(null=True, blank=True)
    changed_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        ordering = ['-changed_at']
