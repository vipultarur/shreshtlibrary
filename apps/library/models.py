from django.db import models
from django.conf import settings

class LibraryInfo(models.Model):
    name = models.CharField(max_length=200, default='Shresht Library')
    tagline = models.CharField(max_length=300, null=True, blank=True)
    description = models.TextField(null=True, blank=True)
    feature_image = models.ImageField(upload_to='library/', null=True, blank=True)
    address = models.TextField(null=True, blank=True)
    phone_primary = models.CharField(max_length=20, null=True, blank=True)
    phone_secondary = models.CharField(max_length=20, null=True, blank=True)
    email = models.EmailField(null=True, blank=True)
    website = models.URLField(null=True, blank=True)
    open_time = models.TimeField(null=True, blank=True)
    close_time = models.TimeField(null=True, blank=True)
    off_days = models.JSONField(default=list, blank=True)
    rules = models.TextField()
    facilities = models.TextField()
    about = models.TextField()
    google_maps_url = models.URLField(null=True, blank=True)
    instagram_url = models.URLField(null=True, blank=True)
    facebook_url = models.URLField(null=True, blank=True)
    updated_at = models.DateTimeField(auto_now=True, null=True)

    def __str__(self):
        return self.name

    def save(self, *args, **kwargs):
        super().save(*args, **kwargs)


class Facility(models.Model):
    name = models.CharField(max_length=100)
    icon_key = models.CharField(max_length=50, blank=True)
    image = models.ImageField(upload_to='facilities/', null=True, blank=True)
    description = models.TextField(null=True, blank=True)
    is_active = models.BooleanField(default=True)
    order = models.IntegerField(default=0)

    class Meta:
        ordering = ['order', 'name']

    def __str__(self):
        return self.name


class Achiever(models.Model):
    name = models.CharField(max_length=100)
    photo = models.ImageField(upload_to='achievers/', null=True, blank=True)
    goal = models.CharField(max_length=100, null=True, blank=True)
    achievement = models.CharField(max_length=255)
    year = models.IntegerField()
    is_featured = models.BooleanField(default=False)
    is_active = models.BooleanField(default=True)
    order = models.IntegerField(default=0)
    created_at = models.DateTimeField(auto_now_add=True, null=True)

    def save(self, *args, **kwargs):
        super().save(*args, **kwargs)

    def __str__(self):
        return f"{self.name} - {self.achievement} ({self.year})"


class Review(models.Model):
    student = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name='reviews')
    rating = models.IntegerField()
    comment = models.TextField()
    is_approved = models.BooleanField(default=False)
    rejection_reason = models.TextField(null=True, blank=True)
    approved_by = models.ForeignKey('accounts.AdminUser', on_delete=models.SET_NULL, null=True, blank=True)
    approved_at = models.DateTimeField(null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True, null=True)
    updated_at = models.DateTimeField(auto_now=True, null=True)

    def __str__(self):
        return f"Review by {self.student.username} - Rating: {self.rating}"



class HomeSlider(models.Model):
    title = models.CharField(max_length=200, blank=True)
    subtitle = models.CharField(max_length=200, blank=True)
    image = models.ImageField(upload_to='sliders/', blank=True, null=True)
    link_url = models.CharField(max_length=500, blank=True)
    is_active = models.BooleanField(default=True)
    sort_order = models.IntegerField(default=0)
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        ordering = ['sort_order', '-created_at']

    def __str__(self):
        return self.title or f"Slider {self.id}"

class AppConfig(models.Model):
    is_premium_gating_enabled = models.BooleanField(default=True)
    expiry_dialog_title = models.CharField(max_length=200, default='Plan Expired')
    expiry_dialog_message = models.TextField(default='Your plan has expired. Please renew to continue using premium features.')
    allow_non_premium_notifications = models.BooleanField(default=True)

    allow_non_premium_sliders = models.BooleanField(default=True)
    allow_non_premium_library_info = models.BooleanField(default=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        verbose_name_plural = 'App Config'

    @classmethod
    def get_solo(cls):
        obj, created = cls.objects.get_or_create(id=1)
        return obj

    def __str__(self):
        return "App Configuration"

class DatabaseFile(models.Model):
    name = models.CharField(max_length=255, unique=True)
    data = models.BinaryField()
    content_type = models.CharField(max_length=100)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return self.name
