from .base import *
import os

DEBUG = False

# Base allowed hosts
ALLOWED_HOSTS = ['shreshtlibrary.com', 'shreshtlibrary.onrender.com']

# Automatically append Render external hostname if running on Render
render_external_hostname = os.environ.get('RENDER_EXTERNAL_HOSTNAME')
if render_external_hostname:
    ALLOWED_HOSTS.append(render_external_hostname)

# Trust the reverse proxy header for SSL/HTTPS detection on Render
SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')

# Cookie security settings for HTTPS
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True

# Enable SSL redirection in production (can be disabled via environment variable if needed)
SECURE_SSL_REDIRECT = os.environ.get('SECURE_SSL_REDIRECT', 'True') == 'True'

