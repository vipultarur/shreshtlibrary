import os, django
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from apps.accounts.models import AdminUser
from rest_framework_simplejwt.tokens import RefreshToken
import requests
import re

admin = AdminUser.objects.filter(role='super_admin').first()
refresh = RefreshToken.for_user(admin)
token = str(refresh.access_token)

headers = {'Authorization': f'Bearer {token}'}
files = {'image': ('test.jpg', b'filecontent', 'image/jpeg')}
data = {'title': 'Test', 'sort_order': '0', 'is_active': 'true', 'subtitle': '', 'link_url': ''}
r = requests.post('http://127.0.0.1:8000/api/v1/admin/sliders/', headers=headers, data=data, files=files)
print(r.status_code)
if r.status_code == 500:
    match = re.search(r'(?si)<textarea id="traceback_area".*?>(.*?)</textarea>', r.text)
    if match:
        print(match.group(1).strip())
    else:
        print('No traceback found. Response html start:', r.text[:500])
else:
    print(r.text)
