from rest_framework.views import APIView
from rest_framework.permissions import AllowAny, IsAuthenticated
from django.utils import timezone

from drf_spectacular.utils import extend_schema

from shreshtlibrary.utils.permissions import IsStudent
from utils.response import standard_response
from apps.memberships.models import MembershipPlan, Membership
from .serializers import MembershipPlanSerializer, MembershipSerializer

class MembershipPlansListView(APIView):
    permission_classes = [AllowAny]

    @extend_schema(responses={200: MembershipPlanSerializer(many=True)}, tags=['Membership Plans'])
    def get(self, request):
        plans = MembershipPlan.objects.filter(is_active=True).order_by('price')
        serializer = MembershipPlanSerializer(plans, many=True)
        return standard_response(data=serializer.data)


class StudentMembershipHistoryView(APIView):
    permission_classes = [IsStudent]

    @extend_schema(responses={200: MembershipSerializer(many=True)}, tags=['Membership Plans'])
    def get(self, request):
        memberships = Membership.objects.filter(student=request.user).order_by('-start_date')
        serializer = MembershipSerializer(memberships, many=True)
        return standard_response(data=serializer.data)
