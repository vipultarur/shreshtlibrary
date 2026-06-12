from django.conf import settings
from django.http import HttpResponse


class SimpleCorsMiddleware:
    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        origin = request.headers.get("Origin")
        allow_all = getattr(settings, 'CORS_ALLOW_ALL_ORIGINS', False)
        allowed_origins = getattr(settings, 'CORS_ALLOWED_ORIGINS', [])
        is_allowed = allow_all or (origin in allowed_origins)

        if request.method == "OPTIONS" and is_allowed:
            response = HttpResponse(status=204)
        else:
            response = self.get_response(request)

        if is_allowed and origin:
            response["Access-Control-Allow-Origin"] = origin
            response["Access-Control-Allow-Credentials"] = "true"
            response["Access-Control-Allow-Headers"] = (
                "authorization, content-type, x-requested-with"
            )
            response["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS"

        return response
