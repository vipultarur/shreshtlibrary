from pathlib import Path
from datetime import timedelta
import os
from dotenv import load_dotenv

# Build paths inside the project like this: BASE_DIR / 'subdir'.
# settings/base.py is 3 levels deep from project root
BASE_DIR = Path(__file__).resolve().parent.parent.parent

# Load environment variables from .env
load_dotenv(dotenv_path=os.path.join(BASE_DIR, '.env'))

# Quick-start development settings - unsuitable for production
SECRET_KEY = os.getenv('SECRET_KEY', 'django-insecure-ing&tc-&$3+-0bw!f0!n2$-vbl63#40i#@^&rbn)gaohd4(c@d')

DEBUG = True

ALLOWED_HOSTS = ['*']

# Application definition
INSTALLED_APPS = [
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    
    # Third party apps
    'rest_framework',
    'rest_framework_simplejwt',
    'drf_spectacular',
    
    # Local Feature Apps under apps/
    'apps.accounts',
    'apps.students',
    'apps.attendance',
    'apps.memberships',
    'apps.payments',
    'apps.seats',
    'apps.notifications',
    'apps.library',
    'apps.study',
    
    # Shared Core Functionality
    'core',
]

MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'utils.cors.SimpleCorsMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

ROOT_URLCONF = 'shreshtlibrary.urls'

TEMPLATES = [
    {
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [os.path.join(BASE_DIR, 'templates')],
        'APP_DIRS': True,
        'OPTIONS': {
            'context_processors': [
                'django.template.context_processors.debug',
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
            ],
        },
    },
]

import dj_database_url

WSGI_APPLICATION = 'shreshtlibrary.wsgi.application'

import sys

# Database Configuration
if 'test' in sys.argv:
    DATABASES = {
        'default': {
            'ENGINE': 'django.db.backends.sqlite3',
            'NAME': BASE_DIR / 'db.sqlite3',
        }
    }
else:
    db_url = os.getenv('DATABASE_URL')
    if db_url and 'supabase' in db_url:
        # Rewrite to use the IPv4 connection pooler (Transaction Mode, port 6543)
        if 'db.crrfhaaqeainuqzkmged.supabase.co' in db_url:
            db_url = db_url.replace('db.crrfhaaqeainuqzkmged.supabase.co', 'aws-1-ap-southeast-1.pooler.supabase.com')
        
        # Ensure it's using port 6543 for transaction mode
        db_url = db_url.replace(':5432/', ':6543/')
        
        if 'postgres.crrfhaaqeainuqzkmged' not in db_url:
            db_url = db_url.replace('://postgres:', '://postgres.crrfhaaqeainuqzkmged:')
            
        os.environ['DATABASE_URL'] = db_url

    DATABASES = {
        'default': dj_database_url.config(
            default=f"sqlite:///{os.path.join(BASE_DIR, 'db.sqlite3')}",
            conn_max_age=0
        )
    }
    DATABASES['default']['DISABLE_SERVER_SIDE_CURSORS'] = True

# Password validation
AUTH_PASSWORD_VALIDATORS = [
    {
        'NAME': 'django.contrib.auth.password_validation.UserAttributeSimilarityValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.MinimumLengthValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.CommonPasswordValidator',
    },
    {
        'NAME': 'django.contrib.auth.password_validation.NumericPasswordValidator',
    },
]

# Internationalization
LANGUAGE_CODE = 'en-us'
TIME_ZONE = 'UTC'
USE_I18N = True
USE_TZ = True

# Static files (CSS, JavaScript, Images)
STATIC_URL = 'static/'
STATIC_ROOT = os.path.join(BASE_DIR, 'static')

DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

# Custom User Model
AUTH_USER_MODEL = 'accounts.CustomUser'

# Django REST Framework Configuration
REST_FRAMEWORK = {
    'DEFAULT_AUTHENTICATION_CLASSES': (
        'shreshtlibrary.utils.authentication.SupabaseJWTAuthentication',
    ),
    'DEFAULT_SCHEMA_CLASS': 'drf_spectacular.openapi.AutoSchema',
    'EXCEPTION_HANDLER': 'utils.response.custom_exception_handler',
    'DEFAULT_THROTTLE_CLASSES': [
        'rest_framework.throttling.AnonRateThrottle',
        'rest_framework.throttling.UserRateThrottle'
    ],
    'DEFAULT_THROTTLE_RATES': {
        'anon': '10/minute',
        'user': '100/minute'
    }
}

# Supabase Auth Configuration
SUPABASE_JWT_SECRET = os.getenv('SUPABASE_JWT_SECRET')

# Authentication Backends
AUTHENTICATION_BACKENDS = [
    'shreshtlibrary.utils.authentication.ShreshtLibraryAuthBackend',
    'django.contrib.auth.backends.ModelBackend',
]

# Simple JWT Configuration
SIMPLE_JWT = {
    'ACCESS_TOKEN_LIFETIME': timedelta(hours=1),
    'REFRESH_TOKEN_LIFETIME': timedelta(days=30),
    'ROTATE_REFRESH_TOKENS': False,
    'BLACKLIST_AFTER_ROTATION': False,
    'ALGORITHM': 'HS256',
    'SIGNING_KEY': SECRET_KEY,
    'VERIFYING_KEY': None,
    'AUDIENCE': None,
    'ISSUER': None,
    'AUTH_HEADER_TYPES': ('Bearer',),
    'USER_ID_FIELD': 'id',
    'USER_ID_CLAIM': 'user_id',
    'AUTH_TOKEN_CLASSES': ('rest_framework_simplejwt.tokens.AccessToken',),
}

# DRF Spectacular Configuration
SPECTACULAR_SETTINGS = {
    'TITLE': 'Shresht Library API',
    'DESCRIPTION': 'Digital Library Management System Backend API',
    'VERSION': '1.0.0',
    'SERVE_INCLUDE_SCHEMA': False,
    'COMPONENT_SPLIT_REQUEST': True,
    'SECURITY': [{'BearerAuth': []}],
}

# Media files
MEDIA_URL = '/media/'
MEDIA_ROOT = os.path.join(BASE_DIR, 'media')
DEFAULT_FILE_STORAGE = 'utils.db_storage.DatabaseStorage'

CORS_ALLOW_ALL_ORIGINS = True
