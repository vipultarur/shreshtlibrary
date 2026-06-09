from rest_framework import serializers
from django.contrib.auth import get_user_model
from apps.accounts.models import AdminUser

User = get_user_model()

class SuperAdminAddAdminSerializer(serializers.Serializer):
    username = serializers.CharField(max_length=150)
    first_name = serializers.CharField(max_length=150)
    last_name = serializers.CharField(max_length=150)
    email = serializers.EmailField()
    mobile = serializers.CharField(max_length=15)
    password = serializers.CharField(write_only=True)
    permissions = serializers.JSONField(default=dict, required=False)

    def validate(self, attrs):
        if AdminUser.objects.filter(username=attrs['username']).exists() or User.objects.filter(username=attrs['username']).exists():
            raise serializers.ValidationError({"username": "Username already taken."})
        if AdminUser.objects.filter(email=attrs['email']).exists() or User.objects.filter(email=attrs['email']).exists():
            raise serializers.ValidationError({"email": "Email already registered."})
        return attrs

    def create(self, validated_data):
        permissions = validated_data.pop('permissions', {})
        admin = AdminUser(
            username=validated_data['username'],
            email=validated_data['email'],
            mobile=validated_data['mobile'],
            first_name=validated_data['first_name'],
            last_name=validated_data['last_name'],
            role='admin',
            permissions=permissions
        )
        admin.set_password(validated_data['password'])
        admin.save()
        return admin
