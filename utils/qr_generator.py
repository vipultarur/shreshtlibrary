import uuid
from django.utils import timezone

def generate_qr_token():
    return f"library-qr-{timezone.now().date()}-{uuid.uuid4()}"
