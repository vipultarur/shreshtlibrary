import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from rest_framework.test import APIRequestFactory, force_authenticate
from apps.accounts.models import AdminUser
from api.v1.v2_admin import AdminSlidersView
from django.core.files.uploadedfile import SimpleUploadedFile

def test_api():
    factory = APIRequestFactory()
    
    admin_user = AdminUser.objects.first()
    if not admin_user:
        admin_user = AdminUser.objects.create(username='admin', role='ADMIN')

    dummy_image = SimpleUploadedFile("test_image.jpg", b"file_content", content_type="image/jpeg")
    
    request = factory.post('/api/v1/admin/sliders/', {
        'title': 'Test Title',
        'subtitle': 'Test Subtitle',
        'link_url': 'http://example.com',
        'is_active': 'true',
        'sort_order': '1',
        'image': dummy_image
    }, format='multipart')
    
    force_authenticate(request, user=admin_user)
    
    view = AdminSlidersView.as_view()
    
    try:
        response = view(request)
        print("Response status:", response.status_code)
        print("Response data:", response.data)
    except Exception as e:
        import traceback
        traceback.print_exc()

if __name__ == '__main__':
    test_api()
