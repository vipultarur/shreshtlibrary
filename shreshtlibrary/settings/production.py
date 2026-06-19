from .base import *
import os

DEBUG = False

# ── Allowed Hosts ────────────────────────────────────────────────
# Production base URL: https://shreshtlibrary.onrender.com
ALLOWED_HOSTS = [
    'shreshtlibrary.onrender.com',
    'shreshtlibrary.com',
    'www.shreshtlibrary.com',
    '*', # Added as fail-safe for Render health checks and internal IP requests
]

# Automatically append Render external hostname if running on Render
render_external_hostname = os.environ.get('RENDER_EXTERNAL_HOSTNAME')
if render_external_hostname:
    ALLOWED_HOSTS.append(render_external_hostname)

# Also accept any .onrender.com subdomain (Render internal routing)
render_service_name = os.environ.get('RENDER_SERVICE_NAME')
if render_service_name:
    ALLOWED_HOSTS.append(f'{render_service_name}.onrender.com')

# ── Security ─────────────────────────────────────────────────────
# Trust the reverse proxy header for SSL/HTTPS detection on Render
SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')

# Cookie security settings for HTTPS
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True

# Enable SSL redirection in production (can be disabled via environment variable if needed)
SECURE_SSL_REDIRECT = os.environ.get('SECURE_SSL_REDIRECT', 'True') == 'True'

# ── Throttle Rates (production) ──────────────────────────────────
REST_FRAMEWORK['DEFAULT_THROTTLE_RATES'] = {
    'anon': '30/minute',
    'user': '300/minute',
}

