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

    # API Version 1 endpoints
    path('api/v1/', include('api.v1.urls')),
]

from django.urls import re_path
from django.views.static import serve
import os

if settings.DEBUG:
    urlpatterns += [
        # Built-in django admin panel
        path('admin/', admin.site.urls),

        # Swagger / OpenAPI Schema endpoints
        path('api/schema/', SpectacularAPIView.as_view(), name='schema'),
        path('swagger/', SpectacularSwaggerView.as_view(url_name='schema'), name='swagger-ui'),
        path('redoc/', SpectacularRedocView.as_view(url_name='schema'), name='redoc-ui'),
    ]

# Serve media files regardless of DEBUG status for Render deployment without external storage
urlpatterns += [
    re_path(r'^media/(?P<path>.*)$', serve, {
        'document_root': settings.MEDIA_ROOT,
    }),
]
