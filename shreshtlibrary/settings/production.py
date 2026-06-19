from .base import *
import os

DEBUG = False

# ── Allowed Hosts ────────────────────────────────────────────────
# Production base URL: https://shreshtlibrary.onrender.com
ALLOWED_HOSTS = [
    'shreshtlibrary.onrender.com',
    'shreshtlibrary.com',
    'www.shreshtlibrary.com',
]

# Automatically append Render external hostname if running on Render
render_external_hostname = os.environ.get('RENDER_EXTERNAL_HOSTNAME')
if render_external_hostname:
    ALLOWED_HOSTS.append(render_external_hostname)

# Also accept any .onrender.com subdomain (Render internal routing)
render_service_name = os.environ.get('RENDER_SERVICE_NAME')
if render_service_name:
    ALLOWED_HOSTS.append(f'{render_service_name}.onrender.com')

# ── CORS ─────────────────────────────────────────────────────────
# Lock CORS to known frontend origins in production
CORS_ALLOW_ALL_ORIGINS = False
CORS_ALLOWED_ORIGINS = [
    'https://shreshtlibrary.vercel.app',
    'https://shreshtlibrary.com',
    'https://www.shreshtlibrary.com',
]
# Append any extra origins from environment
_extra_cors = os.environ.get('CORS_EXTRA_ORIGINS', '')
if _extra_cors:
    CORS_ALLOWED_ORIGINS += [o.strip() for o in _extra_cors.split(',') if o.strip()]

# ── Security Headers ─────────────────────────────────────────────
# Trust the reverse proxy header for SSL/HTTPS detection on Render
SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')

# Cookie security settings for HTTPS
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True

# Enable SSL redirection in production
SECURE_SSL_REDIRECT = os.environ.get('SECURE_SSL_REDIRECT', 'True') == 'True'

# HSTS — tell browsers to always use HTTPS (1 year)
SECURE_HSTS_SECONDS = 31536000
SECURE_HSTS_INCLUDE_SUBDOMAINS = True
SECURE_HSTS_PRELOAD = True

# Prevent MIME type sniffing
SECURE_CONTENT_TYPE_NOSNIFF = True

# Clickjacking protection
X_FRAME_OPTIONS = 'DENY'

# ── Throttle Rates (production) ──────────────────────────────────
REST_FRAMEWORK['DEFAULT_THROTTLE_RATES'] = {
    'anon': '30/minute',
    'user': '300/minute',
}

