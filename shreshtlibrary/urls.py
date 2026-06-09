from django.contrib import admin
from django.urls import path, include
from django.conf import settings
from django.conf.urls.static import static
from drf_spectacular.views import (
    SpectacularAPIView, SpectacularRedocView, SpectacularSwaggerView
)
from apps.library.views import LandingPageView

urlpatterns = [
    # Main library landing page showing features, seats & pricing
    path('', LandingPageView.as_view(), name='landing'),

    # Built-in django admin panel
    path('admin/', admin.site.urls),

    # API Version 1 endpoints
    path('api/v1/', include('api.v1.urls')),

    # Swagger / OpenAPI Schema endpoints
    path('api/schema/', SpectacularAPIView.as_view(), name='schema'),
    path('swagger/', SpectacularSwaggerView.as_view(url_name='schema'), name='swagger-ui'),
    path('redoc/', SpectacularRedocView.as_view(url_name='schema'), name='redoc-ui'),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)

