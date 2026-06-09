from django.db import models
from django.conf import settings

class StudySession(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='study_sessions')
    start_time = models.DateTimeField()
    end_time = models.DateTimeField(null=True, blank=True)
    duration_minutes = models.IntegerField(default=0)

    def __str__(self):
        return f"{self.student.username} session: {self.start_time} to {self.end_time or 'Active'}"


class StudyGoal(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='study_goals')
    date = models.DateField()
    target_hours = models.DecimalField(max_digits=4, decimal_places=2)
    achieved_hours = models.DecimalField(max_digits=4, decimal_places=2, default=0.0)

    class Meta:
        unique_together = ('student', 'date')

    def __str__(self):
        return f"{self.student.username} goal on {self.date}: {self.achieved_hours}/{self.target_hours} hrs"
