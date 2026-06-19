from .base import *
import os

DEBUG = False

# Base allowed hosts
ALLOWED_HOSTS = [
    'shreshtlibrary.com',
    'www.shreshtlibrary.com',
    'shreshtlibrary.onrender.com',
]

# Automatically append Render hostnames if running on Render
render_external_hostname = os.environ.get('RENDER_EXTERNAL_HOSTNAME')
if render_external_hostname and render_external_hostname not in ALLOWED_HOSTS:
    ALLOWED_HOSTS.append(render_external_hostname)

render_service_name = os.environ.get('RENDER_SERVICE_NAME')
if render_service_name:
    render_service_host = f"{render_service_name}.onrender.com"
    if render_service_host not in ALLOWED_HOSTS:
        ALLOWED_HOSTS.append(render_service_host)

# Trust the reverse proxy header for SSL/HTTPS detection on Render
SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')

# Cookie security settings for HTTPS
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True

# Enable SSL redirection in production (can be disabled via environment variable if needed)
SECURE_SSL_REDIRECT = os.environ.get('SECURE_SSL_REDIRECT', 'True') == 'True'

