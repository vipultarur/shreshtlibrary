import jwt
import uuid
from django.conf import settings
from django.contrib.auth import get_user_model
from django.contrib.auth.backends import ModelBackend
from django.contrib.auth.hashers import check_password
from rest_framework import authentication
from rest_framework import exceptions

from apps.accounts.models import CustomUser, AdminUser

User = get_user_model()

class SupabaseJWTAuthentication(authentication.BaseAuthentication):
    def authenticate(self, request):
        auth_header = request.headers.get('Authorization')
        if not auth_header:
            return None
            
        parts = auth_header.split()
        if len(parts) != 2 or parts[0].lower() != 'bearer':
            return None
            
        token = parts[1]
        
        jwt_secret = getattr(settings, 'SUPABASE_JWT_SECRET', None)
        django_secret = settings.SECRET_KEY
        
        payload = None
        error = None
        
        # 1. Try decoding as a Supabase token (using HS256 and SUPABASE_JWT_SECRET)
        if jwt_secret:
            try:
                payload = jwt.decode(
                    token, 
                    jwt_secret, 
                    algorithms=["HS256"], 
                    options={"verify_aud": False}
                )
            except jwt.InvalidTokenError as e:
                error = e
                
        # 2. If it failed or no secret, try Django SECRET_KEY (SimpleJWT format)
        if not payload:
            try:
                payload = jwt.decode(
                    token,
                    django_secret,
                    algorithms=["HS256"],
                    options={"verify_aud": False}
                )
                error = None
            except jwt.InvalidTokenError as e:
                if not error:
                    error = e
                    
        # 3. If still not decoded and settings.DEBUG is True, decode without signature verification
        if not payload and settings.DEBUG:
            try:
                payload = jwt.decode(token, options={"verify_signature": False, "verify_aud": False})
                error = None
            except jwt.InvalidTokenError as e:
                error = e
                
        if not payload:
            raise exceptions.AuthenticationFailed(f'Invalid token: {error}')
            
        # Support both 'sub' (Supabase) and 'user_id' (SimpleJWT)
        user_identifier = payload.get('sub') or payload.get('user_id')
        if not user_identifier:
            raise exceptions.AuthenticationFailed('Token payload missing user identifier.')
            
        email = payload.get('email')
        user_metadata = payload.get('user_metadata', {})
        role = user_metadata.get('role', payload.get('role', 'student'))
        
        if role not in ['student', 'admin', 'super_admin']:
            role = 'student'
            
        user = None
        is_uuid = False
        try:
            uuid.UUID(str(user_identifier))
            is_uuid = True
        except ValueError:
            pass
            
        if role in ['admin', 'super_admin']:
            # Authenticate against separate AdminUser table
            try:
                if is_uuid:
                    user = AdminUser.objects.get(supabase_uid=user_identifier)
                else:
                    user = AdminUser.objects.get(pk=user_identifier)
            except AdminUser.DoesNotExist:
                username = email or f"admin_{user_identifier}"
                user, created = AdminUser.objects.get_or_create(
                    username=username,
                    defaults={
                        'email': email,
                        'role': role,
                        'is_active': True,
                        'supabase_uid': user_identifier if is_uuid else None
                    }
                )
        else:
            # Authenticate against CustomUser (Student) table
            try:
                if is_uuid:
                    user = User.objects.get(supabase_uid=user_identifier)
                else:
                    user = User.objects.get(pk=user_identifier)
            except User.DoesNotExist:
                username = email or f"student_{user_identifier}"
                user, created = User.objects.get_or_create(
                    username=username,
                    defaults={
                        'email': email,
                        'role': role,
                        'is_active': True,
                        'supabase_uid': user_identifier if is_uuid else None
                    }
                )
            
        # Keep roles in sync
        if user.role != role:
            user.role = role
            user.save()
            
        return (user, token)


class ShreshtLibraryAuthBackend(ModelBackend):
    def authenticate(self, request, username=None, password=None, **kwargs):
        # 1. Try AdminUser first (since it's password based for admin login view)
        try:
            admin = AdminUser.objects.get(username=username)
            if admin.check_password(password):
                return admin
        except AdminUser.DoesNotExist:
            # Try by email or mobile as fallback
            try:
                admin = AdminUser.objects.get(email=username)
                if admin.check_password(password):
                    return admin
            except AdminUser.DoesNotExist:
                try:
                    admin = AdminUser.objects.get(mobile=username)
                    if admin.check_password(password):
                        return admin
                except AdminUser.DoesNotExist:
                    pass

        # 2. Try CustomUser (Student)
        try:
            user = CustomUser.objects.get(username=username)
            if user.check_password(password) and user.role == 'student':
                return user
        except CustomUser.DoesNotExist:
            # Try by email or mobile
            try:
                user = CustomUser.objects.get(email=username)
                if user.check_password(password) and user.role == 'student':
                    return user
            except CustomUser.DoesNotExist:
                try:
                    user = CustomUser.objects.get(mobile=username)
                    if user.check_password(password) and user.role == 'student':
                        return user
                except CustomUser.DoesNotExist:
                    pass
        return None

    def get_user(self, user_id):
        try:
            return CustomUser.objects.get(pk=user_id)
        except CustomUser.DoesNotExist:
            try:
                return AdminUser.objects.get(pk=user_id)
            except AdminUser.DoesNotExist:
                return None


from drf_spectacular.extensions import OpenApiAuthenticationExtension

class SupabaseJWTAuthenticationScheme(OpenApiAuthenticationExtension):
    target_class = 'shreshtlibrary.utils.authentication.SupabaseJWTAuthentication'
    name = 'BearerAuth'

    def get_security_definition(self, auto_schema):
        return {
            'type': 'http',
            'scheme': 'bearer',
            'bearerFormat': 'JWT',
        }
