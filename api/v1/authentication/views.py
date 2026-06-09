from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework_simplejwt.tokens import RefreshToken
from django.contrib.auth import get_user_model, authenticate
from django.utils import timezone
import datetime
import uuid

from drf_spectacular.utils import extend_schema, OpenApiTypes

from .serializers import (
    CustomUserSerializer, UserRegisterSerializer, SendOTPSerializer, VerifyOTPSerializer,
    UserLoginSerializer, UserLoginMobileSerializer, ForgotPasswordSerializer, ResetPasswordSerializer,
    AdminLoginSerializer, ChangePasswordSerializer
)
from utils.response import standard_response
from core.models import ActivityLog

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

class StudentRegisterView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=UserRegisterSerializer, responses={201: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = UserRegisterSerializer(data=request.data)
        if serializer.is_valid():
            user = serializer.create(serializer.validated_data)
            tokens = get_tokens_for_user(user)
            log_activity(user, "Registered new account", request)
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

    @extend_schema(request=SendOTPSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = SendOTPSerializer(data=request.data)
        if serializer.is_valid():
            mobile = serializer.validated_data['mobile']
            try:
                user = User.objects.get(mobile=mobile)
                # Generate simulated OTP
                user.otp = "123456"  # Mock fixed OTP for dev environment
                user.otp_expiry = timezone.now() + datetime.timedelta(minutes=5)
                user.save()
                log_activity(user, "Sent login OTP", request)
                return standard_response(message="OTP sent successfully (Simulated value: '123456').")
            except User.DoesNotExist:
                return Response({"errors": {"mobile": ["Mobile number not registered."]}}, status=status.HTTP_404_NOT_FOUND)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class VerifyOTPView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(request=VerifyOTPSerializer, responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        serializer = VerifyOTPSerializer(data=request.data)
        if serializer.is_valid():
            mobile = serializer.validated_data['mobile']
            otp = serializer.validated_data['otp']
            try:
                user = User.objects.get(mobile=mobile)
                if user.otp == otp and user.otp_expiry > timezone.now():
                    user.otp = None  # Consume OTP
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
                    return Response({"errors": {"otp": ["Invalid or expired OTP."]}}, status=status.HTTP_400_BAD_REQUEST)
            except User.DoesNotExist:
                return Response({"errors": {"mobile": ["Mobile number not registered."]}}, status=status.HTTP_404_NOT_FOUND)
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class StudentLoginView(APIView):
    permission_classes = [AllowAny]

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
                # Mock password reset token logic (simulated by setting OTP)
                reset_token = f"reset-{uuid.uuid4()}"
                user.otp = reset_token
                user.otp_expiry = timezone.now() + datetime.timedelta(hours=1)
                user.save()
                log_activity(user, "Requested password reset link", request)
                return standard_response(
                    message="Password reset link sent to your email.",
                    data={"reset_token": reset_token}
                )
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
    permission_classes = [IsAuthenticated]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Authentication'])
    def post(self, request):
        # JWT logout is stateless client-side discard, but we can record it
        log_activity(request.user, "Logged out", request)
        return standard_response(message="Logged out successfully.")


from api.v1.admin.serializers import AdminProfileSerializer

class AdminLoginView(APIView):
    permission_classes = [AllowAny]

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
