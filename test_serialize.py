import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'shreshtlibrary.settings')
django.setup()

from apps.library.models import HomeSlider
from api.v1.v2_admin import serialize_slider

try:
    slider = HomeSlider.objects.create(
        title="Test Slider No Image",
        subtitle="Subtitle",
        link_url="http://example.com",
        is_active=True,
        sort_order=0,
    )
    print("Slider created successfully:", slider.id)

    # NO IMAGE UPLOADED
    serialized = serialize_slider(slider)
    print("Serialized:", serialized)

except Exception as e:
    import traceback
    traceback.print_exc()

