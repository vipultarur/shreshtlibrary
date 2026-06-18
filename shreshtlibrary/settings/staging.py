from .base import *

DEBUG = False
ALLOWED_HOSTS = ['staging.shreshtlibrary.com']
CORS_ALLOW_ALL_ORIGINS = False
CORS_ALLOWED_ORIGINS = env_list('CORS_ALLOWED_ORIGINS')
