from io import BytesIO
from pathlib import Path

from django.core.files.base import ContentFile
from PIL import Image, ImageOps


def compress_image_field(field_file, *, max_size=(1600, 1600), quality=88):
    """Compress newly uploaded ImageField files while preserving display quality."""
    if not field_file or getattr(field_file, "_committed", True):
        return

    try:
        field_file.file.seek(0)
        image = Image.open(field_file.file)
        image = ImageOps.exif_transpose(image)
    except Exception:
        return

    if image.mode in ("RGBA", "LA", "P"):
        image = image.convert("RGB")

    image.thumbnail(max_size, Image.Resampling.LANCZOS)

    original_name = Path(field_file.name or "upload.jpg")
    output_name = f"{original_name.stem}.jpg"
    output = BytesIO()
    image.save(output, format="JPEG", quality=quality, optimize=True, progressive=True)
    output.seek(0)

    field_file.save(output_name, ContentFile(output.read()), save=False)
