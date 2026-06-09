from django.db import models
from django.conf import settings

class LibraryInfo(models.Model):
    rules = models.TextField()
    facilities = models.TextField()
    about = models.TextField()

    def __str__(self):
        return "Library Information"


class Achiever(models.Model):
    name = models.CharField(max_length=100)
    photo = models.ImageField(upload_to='achievers/', null=True, blank=True)
    achievement = models.CharField(max_length=255)
    year = models.IntegerField()

    def __str__(self):
        return f"{self.name} - {self.achievement} ({self.year})"


class Review(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='reviews')
    rating = models.IntegerField()
    comment = models.TextField()
    is_approved = models.BooleanField(default=False)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"Review by {self.student.username} - Rating: {self.rating}"
