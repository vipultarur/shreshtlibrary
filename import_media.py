import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings.base')
django.setup()

from apps.library.models import DatabaseFile
from django.conf import settings
import mimetypes

media_dir = settings.MEDIA_ROOT
count = 0

for root, dirs, files in os.walk(media_dir):
    for file in files:
        file_path = os.path.join(root, file)
        # Relative name starts without 'media/'
        rel_path = os.path.relpath(file_path, media_dir)
        # Convert windows slashes to forward slashes
        rel_path = rel_path.replace('\\', '/')
        
        with open(file_path, 'rb') as f:
            data = f.read()
            
        content_type, _ = mimetypes.guess_type(rel_path)
        if not content_type:
            content_type = 'application/octet-stream'
            
        DatabaseFile.objects.update_or_create(
            name=rel_path,
            defaults={
                'data': data,
                'content_type': content_type
            }
        )
        count += 1
        print(f"Imported {rel_path}")

print(f"Total imported: {count}")
