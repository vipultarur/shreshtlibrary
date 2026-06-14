import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from rest_framework.test import APIRequestFactory, force_authenticate
from django.contrib.auth import get_user_model
from api.v1.v2_admin import AdminSlidersView
from django.core.files.uploadedfile import SimpleUploadedFile

User = get_user_model()

def test_api():
    factory = APIRequestFactory()
    
    user = User.objects.first()
    if not user:
        user = User.objects.create_superuser('adminuser', 'admin@example.com', 'password123')

    # 1MB dummy image
    dummy_image = SimpleUploadedFile("super_test_image.jpg", b"1", content_type="image/jpeg")
    
    request = factory.post('/api/v1/admin/sliders/', {
        'title': 'Test Title Super',
        'subtitle': 'Test Subtitle',
        'link_url': 'http://example.com',
        'is_active': 'true',
        'sort_order': '1',
        'image': dummy_image
    }, format='multipart')
    
    force_authenticate(request, user=user)
    
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
