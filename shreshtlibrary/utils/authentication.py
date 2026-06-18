import datetime as dt
import hashlib
import jwt
import uuid
from django.conf import settings
from django.contrib.auth import get_user_model
from django.contrib.auth.backends import ModelBackend
from django.contrib.auth.hashers import check_password
from django.db.models import Q
from django.utils import timezone
from rest_framework import authentication
from rest_framework import exceptions

from apps.accounts.models import AuthTokenRevocation, CustomUser, AdminUser

User = get_user_model()


def _hash_token(token):
    return hashlib.sha256(token.encode("utf-8")).hexdigest()


def _expires_at_from_payload(payload):
    exp = payload.get("exp")
    if not exp:
        return None
    try:
        return dt.datetime.fromtimestamp(int(exp), tz=dt.timezone.utc)
    except (TypeError, ValueError, OSError):
        return None


def is_token_revoked(token, payload):
    token_hash = _hash_token(token)
    filters = Q(token_hash=token_hash)
    jti = payload.get("jti")
    if jti:
        filters |= Q(jti=jti)
    return AuthTokenRevocation.objects.filter(
        filters,
    ).filter(
        Q(expires_at__isnull=True) | Q(expires_at__gt=timezone.now())
    ).exists()


def revoke_token(token, payload=None, user=None):
    if payload is None:
        try:
            payload = jwt.decode(token, options={"verify_signature": False, "verify_aud": False})
        except jwt.InvalidTokenError:
            payload = {}

    token_hash = _hash_token(token)
    expires_at = _expires_at_from_payload(payload)
    user_identifier = payload.get("sub") or payload.get("user_id")
    AuthTokenRevocation.objects.get_or_create(
        token_hash=token_hash,
        defaults={
            "jti": payload.get("jti") or "",
            "user_identifier": str(user_identifier or getattr(user, "pk", "")),
            "expires_at": expires_at,
        },
    )


class SupabaseJWTAuthentication(authentication.BaseAuthentication):
    def authenticate_header(self, request):
        return "Bearer"

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
        token_source = None
        
        # 1. Try decoding as a Supabase token (using HS256 and SUPABASE_JWT_SECRET)
        if jwt_secret:
            try:
                payload = jwt.decode(
                    token, 
                    jwt_secret, 
                    algorithms=["HS256"], 
                    options={"verify_aud": False}
                )
                token_source = "supabase"
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
                token_source = "django"
            except jwt.InvalidTokenError as e:
                if not error:
                    error = e
                
        if not payload:
            raise exceptions.AuthenticationFailed(f'Invalid token: {error}')

        if payload.get("token_type") and payload.get("token_type") != "access":
            raise exceptions.AuthenticationFailed('Invalid token type.')

        if is_token_revoked(token, payload):
            raise exceptions.AuthenticationFailed('Token has been revoked.')
            
        # Support both 'sub' (Supabase) and 'user_id' (SimpleJWT)
        user_identifier = payload.get('sub') or payload.get('user_id')
        if not user_identifier:
            raise exceptions.AuthenticationFailed('Token payload missing user identifier.')
            
        user_metadata = payload.get('user_metadata', {})
        token_role = user_metadata.get('role', payload.get('role', 'student'))
        if token_role not in ['student', 'admin', 'super_admin']:
            token_role = 'student'
            
        is_uuid = False
        try:
            uuid.UUID(str(user_identifier))
            is_uuid = True
        except ValueError:
            pass

        if is_uuid:
            admin_user = AdminUser.objects.filter(supabase_uid=user_identifier).first()
            student_user = User.objects.filter(supabase_uid=user_identifier).first()
        else:
            admin_user = AdminUser.objects.filter(pk=user_identifier).first()
            student_user = User.objects.filter(pk=user_identifier).first()

        if admin_user and student_user:
            if token_source == "django":
                user = admin_user if token_role in ['admin', 'super_admin'] else student_user
            else:
                raise exceptions.AuthenticationFailed('Token maps to multiple local users.')
        else:
            user = admin_user or student_user

        if not user:
            raise exceptions.AuthenticationFailed('User not found.')

        if not getattr(user, 'is_active', False):
            raise exceptions.AuthenticationFailed('User account is inactive.')
            
        request.auth_payload = payload
        return (user, token)


class ShreshtLibraryAuthBackend(ModelBackend):
    def authenticate(self, request, username=None, password=None, **kwargs):
        # 1. Try AdminUser first (since it's password based for admin login view)
        try:
            admin = AdminUser.objects.get(username=username)
            if admin.is_active and admin.check_password(password):
                return admin
        except AdminUser.DoesNotExist:
            # Try by email or mobile as fallback
            try:
                admin = AdminUser.objects.get(email=username)
                if admin.is_active and admin.check_password(password):
                    return admin
            except AdminUser.DoesNotExist:
                try:
                    admin = AdminUser.objects.get(mobile=username)
                    if admin.is_active and admin.check_password(password):
                        return admin
                except AdminUser.DoesNotExist:
                    pass

        # 2. Try CustomUser (Student)
        try:
            user = CustomUser.objects.get(username=username)
            if user.is_active and user.check_password(password) and user.role == 'student':
                return user
        except CustomUser.DoesNotExist:
            # Try by email or mobile
            try:
                user = CustomUser.objects.get(email=username)
                if user.is_active and user.check_password(password) and user.role == 'student':
                    return user
            except CustomUser.DoesNotExist:
                try:
                    user = CustomUser.objects.get(mobile=username)
                    if user.is_active and user.check_password(password) and user.role == 'student':
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
