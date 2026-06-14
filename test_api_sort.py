import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from rest_framework.test import APIRequestFactory, force_authenticate
from django.contrib.auth import get_user_model
from api.v1.v2_admin import AdminSlidersView

User = get_user_model()

def test_api():
    factory = APIRequestFactory()
    
    user = User.objects.first()
    
    request = factory.post('/api/v1/admin/sliders/', {
        'title': 'Test Title',
        'subtitle': 'Test Subtitle',
        'link_url': 'http://example.com',
        'is_active': 'true',
        'sort_order': '',
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
