from .base import *
import os

DEBUG = False

# Base allowed hosts — always include known production domains
ALLOWED_HOSTS = [
    'shreshtlibrary.com',
    'www.shreshtlibrary.com',
    'shreshtlibrary.onrender.com',
]

# Merge in any hosts from the env variable (e.g. localhost for dev)
ALLOWED_HOSTS += env_list('DJANGO_ALLOWED_HOSTS', '')

# Automatically append Render external hostname if running on Render
render_external_hostname = os.environ.get('RENDER_EXTERNAL_HOSTNAME')
if render_external_hostname:
    ALLOWED_HOSTS.append(render_external_hostname)

# Deduplicate
ALLOWED_HOSTS = list(set(ALLOWED_HOSTS))

# CORS — allow Vercel frontend + any env-configured origins
CORS_ALLOW_ALL_ORIGINS = False
CORS_ALLOWED_ORIGINS = [
    'https://shreshtlibrary.vercel.app',
    'https://shreshtlibrary.com',
    'https://www.shreshtlibrary.com',
] + env_list('CORS_ALLOWED_ORIGINS', '')
CORS_ALLOWED_ORIGINS = list(set(CORS_ALLOWED_ORIGINS))
CORS_ALLOW_CREDENTIALS = True

# Trust the reverse proxy header for SSL/HTTPS detection on Render
SECURE_PROXY_SSL_HEADER = ('HTTP_X_FORWARDED_PROTO', 'https')

# Cookie security settings for HTTPS
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True

# Disable SSL redirect — Render's load balancer handles TLS termination.
# Enabling this causes infinite redirect loops.
SECURE_SSL_REDIRECT = os.environ.get('SECURE_SSL_REDIRECT', 'False') == 'True'

