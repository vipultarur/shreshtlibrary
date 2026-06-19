from rest_framework import serializers
from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework.throttling import AnonRateThrottle
from rest_framework_simplejwt.tokens import RefreshToken
from rest_framework_simplejwt.views import TokenRefreshView
from django.contrib.auth import get_user_model, authenticate
from django.conf import settings
from django.utils import timezone
import datetime
import jwt
import random
import uuid

from drf_spectacular.utils import extend_schema, OpenApiTypes

from .serializers import (
    CustomUserSerializer, UserRegisterSerializer, SendOTPSerializer, VerifyOTPSerializer,
    UserLoginSerializer, UserLoginMobileSerializer, ForgotPasswordSerializer, ResetPasswordSerializer,
    AdminLoginSerializer, ChangePasswordSerializer
)
from utils.response import standard_response
from core.models import ActivityLog
from shreshtlibrary.utils.authentication import is_token_revoked, revoke_token

from apps.accounts.models import AdminUser

User = get_user_model()

def get_tokens_for_user(user):
    refresh = RefreshToken.for_user(user)
    # Custom claim
    refresh['role'] = user.role
    if getattr(user, 'supabase_uid', None):
        refresh['sub'] = str(user.supabase_uid)
    return {
        'refresh': str(refresh),
        'access': str(refresh.access_token),
    }

def log_activity(user, action, request):
    ip = request.META.get('REMOTE_ADDR')
    params = {
        "action": action,
        "ip_address": ip,
        "details": {"path": request.path, "method": request.method}
    }
    if isinstance(user, AdminUser):
        params["admin"] = user
    else:
        params["user"] = user
    ActivityLog.objects.create(**params)


def _token_subject_is_active(payload):
    user_identifier = payload.get('user_id') or payload.get('sub')
    if not user_identifier:
        return False

    admin = AdminUser.objects.filter(pk=user_identifier).first()
    student = User.objects.filter(pk=user_identifier).first()
    role = payload.get('role')

    if admin and student:
        user = admin if role in ['admin', 'super_admin'] else student
    else:
        user = admin or student

    return bool(user and user.is_active)


class RevokingTokenRefreshView(TokenRefreshView):
    def post(self, request, *args, **kwargs):
        refresh_token = request.data.get("refresh")
        if not refresh_token:
            return standard_response("error", "Refresh token is required.", status_code=status.HTTP_400_BAD_REQUEST)

        try:
            payload = jwt.decode(
                refresh_token,
                settings.SECRET_KEY,
                algorithms=["HS256"],
                options={"verify_aud": False},
            )
        except jwt.InvalidTokenError:
            return standard_response("error", "Invalid refresh token.", status_code=status.HTTP_401_UNAUTHORIZED)

        if payload.get("token_type") != "refresh":
            return standard_response("error", "Invalid refresh token.", status_code=status.HTTP_401_UNAUTHORIZED)
        if is_token_revoked(refresh_token, payload):
            return standard_response("error", "Refresh token has been revoked.", status_code=status.HTTP_401_UNAUTHORIZED)
        if not _token_subject_is_active(payload):
            return standard_response("error", "User account is inactive.", status_code=status.HTTP_401_UNAUTHORIZED)

        return super().post(request, *args, **kwargs)

class StudentRegisterView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=UserRegisterSerializer, responses={201: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = UserRegisterSerializer(data=request.data)
        if serializer.is_valid():
            user = serializer.create(serializer.validated_data)
            tokens = get_tokens_for_user(user)
            log_activity(user, "Registered new account", request)
            
            try:
                from apps.notifications.models import AdminInboxNotification
                AdminInboxNotification.objects.create(
                    type='NEW_STUDENT',
                    title='New Student Registered',
                    message=f"Student {user.username} has just registered.",
                    related_id=str(user.id),
                    student=user
                )
            except Exception:
                pass
            return standard_response(
                message="Registration successful. Welcome to Shresht Library.",
                data={
                    "tokens": tokens,
                    "user": CustomUserSerializer(user).data
                },
                status_code=status.HTTP_201_CREATED
            )
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class SendOTPView(APIView):
    permission_classes = [AllowAny]
    throttle_classes = [AnonRateThrottle]

    @extend_schema(request=SendOTPSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = SendOTPSerializer(data=request.data)
        if serializer.is_valid():
            mobile = serializer.validated_data['mobile']
            try:
                user = User.objects.get(mobile=mobile)
                raw_otp = f"{random.randint(0, 999999):06d}"
                # Log or send raw_otp here (e.g., via SMS)
                # print(f"OTP for {mobile}: {raw_otp}")
                from django.contrib.auth.hashers import make_password
                user.otp = make_password(raw_otp)
                user.otp_expiry = timezone.now() + datetime.timedelta(minutes=5)
                user.otp_attempts = 0
                user.save()
                log_activity(user, "Sent login OTP", request)
                return standard_response(message="OTP sent successfully.")
            except User.DoesNotExist:
                return standard_response("error", "Mobile number not registered.", errors={"mobile": ["Mobile number not registered."]}, status_code=404)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)


class VerifyOTPView(APIView):
    permission_classes = [AllowAny]
    throttle_classes = [AnonRateThrottle]

    @extend_schema(request=VerifyOTPSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = VerifyOTPSerializer(data=request.data)
        if serializer.is_valid():
            mobile = serializer.validated_data['mobile']
            otp = serializer.validated_data['otp']
            try:
                user = User.objects.get(mobile=mobile)
                if user.otp_attempts >= 5:
                    return standard_response("error", "Too many failed attempts. Please request a new OTP.", status_code=403)
                if user.otp_expiry < timezone.now():
                    return standard_response("error", "OTP has expired.", errors={"otp": ["OTP has expired."]}, status_code=400)
                
                from django.contrib.auth.hashers import check_password
                if check_password(otp, user.otp):
                    user.otp = None  # Consume OTP
                    user.otp_attempts = 0
                    user.save()
                    tokens = get_tokens_for_user(user)
                    log_activity(user, "Logged in via OTP", request)
                    return standard_response(
                        message="OTP verified successfully.",
                        data={
                            "tokens": tokens,
                            "user": CustomUserSerializer(user).data
                        }
                    )
                else:
                    user.otp_attempts += 1
                    user.save(update_fields=['otp_attempts'])
                    return standard_response("error", "Invalid OTP.", errors={"otp": ["Invalid OTP."]}, status_code=400)
            except User.DoesNotExist:
                return standard_response("error", "Mobile number not registered.", errors={"mobile": ["Mobile number not registered."]}, status_code=404)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)


class StudentLoginView(APIView):
    permission_classes = [AllowAny]
    throttle_classes = [AnonRateThrottle]

    @extend_schema(request=UserLoginSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = UserLoginSerializer(data=request.data)
        if serializer.is_valid():
            email = serializer.validated_data['email']
            password = serializer.validated_data['password']
            user = authenticate(username=email, password=password)
            if not user:
                # Fallback to authenticating with email since username field is mapped to mobile
                try:
                    u = User.objects.get(email=email)
                    user = authenticate(username=u.username, password=password)
                except User.DoesNotExist:
                    pass

            if user and user.role == 'student':
                tokens = get_tokens_for_user(user)
                log_activity(user, "Logged in via Email/Password", request)
                return standard_response(
                    message="Login successful.",
                    data={
                        "tokens": tokens,
                        "user": CustomUserSerializer(user).data
                    }
                )
            return Response({"errors": {"non_field_errors": ["Invalid credentials or not a student."]}}, status=status.HTTP_400_BAD_REQUEST)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class StudentLoginMobileView(APIView):
    permission_classes = [AllowAny]
    throttle_classes = [AnonRateThrottle]

    @extend_schema(request=UserLoginMobileSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = UserLoginMobileSerializer(data=request.data)
        if serializer.is_valid():
            mobile = serializer.validated_data['mobile']
            password = serializer.validated_data['password']
            user = authenticate(username=mobile, password=password)
            if user and user.role == 'student':
                tokens = get_tokens_for_user(user)
                log_activity(user, "Logged in via Mobile/Password", request)
                return standard_response(
                    message="Login successful.",
                    data={
                        "tokens": tokens,
                        "user": CustomUserSerializer(user).data
                    }
                )
            return Response({"errors": {"non_field_errors": ["Invalid credentials or not a student."]}}, status=status.HTTP_400_BAD_REQUEST)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class ForgotPasswordView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=ForgotPasswordSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = ForgotPasswordSerializer(data=request.data)
        if serializer.is_valid():
            email = serializer.validated_data['email']
            try:
                user = User.objects.get(email=email)
                reset_token = f"reset-{uuid.uuid4()}"
                user.otp = reset_token
                user.otp_expiry = timezone.now() + datetime.timedelta(hours=1)
                user.save()
                log_activity(user, "Requested password reset link", request)
                return standard_response(message="Password reset link sent to your email.")
            except User.DoesNotExist:
                return Response({"errors": {"detail": ["User not found."]}}, status=status.HTTP_404_NOT_FOUND)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class ResetPasswordView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=ResetPasswordSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = ResetPasswordSerializer(data=request.data)
        if serializer.is_valid():
            token = serializer.validated_data['token']
            new_password = serializer.validated_data['new_password']
            try:
                user = User.objects.get(otp=token, otp_expiry__gt=timezone.now())
                user.set_password(new_password)
                user.otp = None  # Consume token
                user.save()
                log_activity(user, "Reset password", request)
                return standard_response(message="Password reset successfully.")
            except User.DoesNotExist:
                return Response({"errors": {"token": ["Invalid or expired reset token."]}}, status=status.HTTP_400_BAD_REQUEST)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class LogoutView(APIView):
    serializer_class = serializers.Serializer
    permission_classes = [IsAuthenticated]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        refresh_token = request.data.get("refresh")
        if refresh_token:
            revoke_token(str(refresh_token))

        if request.auth:
            revoke_token(str(request.auth), getattr(request, "auth_payload", None), request.user)
        log_activity(request.user, "Logged out", request)
        return standard_response(message="Logged out successfully.")


from api.v1.admin.serializers import AdminProfileSerializer

class AdminLoginView(APIView):
    permission_classes = [AllowAny]
    throttle_classes = [AnonRateThrottle]

    @extend_schema(request=AdminLoginSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = AdminLoginSerializer(data=request.data)
        if serializer.is_valid():
            username = serializer.validated_data['username']
            password = serializer.validated_data['password']
            user = authenticate(username=username, password=password)
            if user and user.role in ['admin', 'super_admin']:
                tokens = get_tokens_for_user(user)
                log_activity(user, "Admin logged in", request)
                return standard_response(
                    message="Admin login successful.",
                    data={
                        "tokens": tokens,
                        "user": AdminProfileSerializer(user).data
                    }
                )
            return Response({"errors": {"non_field_errors": ["Invalid credentials or not an admin."]}}, status=status.HTTP_400_BAD_REQUEST)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class AdminChangePasswordView(APIView):
    permission_classes = [IsAuthenticated]

    @extend_schema(request=ChangePasswordSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = ChangePasswordSerializer(data=request.data)
        if serializer.is_valid():
            user = request.user
            if user.check_password(serializer.validated_data['old_password']):
                user.set_password(serializer.validated_data['new_password'])
                user.save()
                log_activity(user, "Changed password", request)
                return standard_response(message="Password changed successfully.")
            else:
                return Response({"errors": {"old_password": ["Incorrect old password."]}}, status=status.HTTP_400_BAD_REQUEST)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)
