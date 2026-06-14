import os
from django.core.files.storage import Storage
from django.core.files.base import ContentFile
from django.utils.deconstruct import deconstructible
import mimetypes

@deconstructible
class DatabaseStorage(Storage):
    def _open(self, name, mode='rb'):
        from apps.library.models import DatabaseFile
        try:
            f = DatabaseFile.objects.get(name=name)
            return ContentFile(f.data, name=name)
        except DatabaseFile.DoesNotExist:
            return None

    def _save(self, name, content):
        from apps.library.models import DatabaseFile
        content.seek(0)
        data = content.read()
        
        # Try to guess content type
        content_type = getattr(content, 'content_type', None)
        if not content_type:
            content_type, _ = mimetypes.guess_type(name)
        if not content_type:
            content_type = 'application/octet-stream'

        DatabaseFile.objects.update_or_create(
            name=name,
            defaults={
                'data': data,
                'content_type': content_type
            }
        )
        return name

    def exists(self, name):
        from apps.library.models import DatabaseFile
        return DatabaseFile.objects.filter(name=name).exists()

    def url(self, name):
        return f"/media/{name}"
