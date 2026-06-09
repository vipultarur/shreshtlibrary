from rest_framework.views import APIView
from rest_framework.response import Response
from rest_framework import status
from django.shortcuts import get_object_or_404

from drf_spectacular.utils import extend_schema, OpenApiTypes

from shreshtlibrary.utils.permissions import IsSuperAdmin
from utils.response import standard_response
from apps.accounts.models import AdminUser
from .serializers import SuperAdminAddAdminSerializer
from api.v1.admin.serializers import AdminProfileSerializer

class SuperAdminAddAdminView(APIView):
    permission_classes = [IsSuperAdmin]

    @extend_schema(request=SuperAdminAddAdminSerializer, responses={201: AdminProfileSerializer}, tags=['Super Admin'])
    def post(self, request):
        serializer = SuperAdminAddAdminSerializer(data=request.data)
        if serializer.is_valid():
            admin = serializer.save(created_by=request.user)
            return standard_response(
                message="New admin registered successfully.",
                data=AdminProfileSerializer(admin).data,
                status_code=status.HTTP_201_CREATED
            )
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)


class SuperAdminRemoveAdminView(APIView):
    permission_classes = [IsSuperAdmin]

    @extend_schema(responses={200: OpenApiTypes.OBJECT}, tags=['Super Admin'])
    def delete(self, request, pk):
        admin = get_object_or_404(AdminUser, id=pk)
        admin.delete()
        return standard_response(message="Admin account deleted successfully.")


class SuperAdminListAdminsView(APIView):
    permission_classes = [IsSuperAdmin]

    @extend_schema(responses={200: AdminProfileSerializer(many=True)}, tags=['Super Admin'])
    def get(self, request):
        admins = AdminUser.objects.all()
        serializer = AdminProfileSerializer(admins, many=True)
        return standard_response(data=serializer.data)
