from decimal import Decimal
from django.db.models import Q
from django.utils import timezone
from django.shortcuts import get_object_or_404
from rest_framework import generics, status
from rest_framework.views import APIView
from rest_framework.permissions import AllowAny
from django_filters.rest_framework import DjangoFilterBackend
from django.contrib.auth import get_user_model

from apps.memberships.models import Membership, MembershipPlan
from shreshtlibrary.utils.permissions import HasAdminPermission
from api.v1.admin.pagination import AdminStandardPagination
from api.v1.admin.serializers import MembershipSerializer, MembershipPlanSerializer, StudentProfileSerializer
from utils.response import standard_response
from api.v1.v2_admin import _activity, _admin_user, _date

User = get_user_model()

class PlansView(generics.ListCreateAPIView):
    serializer_class = MembershipPlanSerializer

    def get_permissions(self):
        if self.request.method == "GET":
            return [AllowAny()]
        return [HasAdminPermission("manage_plans")()]

    def get_queryset(self):
        return MembershipPlan.objects.filter(is_active=True).order_by("sort_order", "price")

    def create(self, request, *args, **kwargs):
        serializer = self.get_serializer(data=request.data)
        if serializer.is_valid():
            if not request.data.get("duration_months") and request.data.get("duration_days"):
                serializer.validated_data["duration_months"] = max(int(request.data.get("duration_days", 30)) // 30, 1)
            plan = serializer.save()
            _activity(request, "CREATE_PLAN", "MembershipPlan", plan.id, f"Created plan {plan.name}")
            return standard_response(message="Plan created successfully.", data=self.get_serializer(plan).data, status_code=201)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)


class PlansAllView(generics.ListAPIView):
    permission_classes = [HasAdminPermission("manage_plans")]
    serializer_class = MembershipPlanSerializer
    queryset = MembershipPlan.objects.all().order_by("sort_order", "price")

    def list(self, request, *args, **kwargs):
        return standard_response(data=self.get_serializer(self.get_queryset(), many=True).data)


class PlanDetailView(generics.RetrieveUpdateDestroyAPIView):
    serializer_class = MembershipPlanSerializer
    queryset = MembershipPlan.objects.all()

    def get_permissions(self):
        if self.request.method == "GET":
            return [AllowAny()]
        return [HasAdminPermission("manage_plans")()]

    def retrieve(self, request, *args, **kwargs):
        return standard_response(data=self.get_serializer(self.get_object()).data)

    def update(self, request, *args, **kwargs):
        plan = self.get_object()
        serializer = self.get_serializer(plan, data=request.data, partial=True)
        if serializer.is_valid():
            plan = serializer.save()
            _activity(request, "EDIT_PLAN", "MembershipPlan", plan.id, f"Updated plan {plan.name}")
            return standard_response(data=self.get_serializer(plan).data)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)

    def destroy(self, request, *args, **kwargs):
        plan = self.get_object()
        if Membership.objects.filter(plan=plan).exists():
            return standard_response("error", "Plan has memberships and cannot be deleted.", status_code=400)
        plan.delete()
        return standard_response(message="Plan deleted successfully.")


class PlanToggleView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def patch(self, request, pk):
        plan = get_object_or_404(MembershipPlan, id=pk)
        plan.is_active = request.data.get("is_active", not plan.is_active)
        plan.save(update_fields=["is_active", "updated_at"])
        return standard_response(data=MembershipPlanSerializer(plan).data)


class PlanStudentsView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request, pk):
        memberships = Membership.objects.filter(plan_id=pk, status="active").select_related("student", "student__student_profile")
        
        profiles = [item.student.student_profile for item in memberships if hasattr(item.student, "student_profile")]
        data = StudentProfileSerializer(profiles, many=True).data
        
        return standard_response(data=data)


class PlanStatsView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request):
        data = []
        for plan in MembershipPlan.objects.all():
            plan_data = MembershipPlanSerializer(plan).data
            plan_data["active_students"] = Membership.objects.filter(plan=plan, status="active").values("student").distinct().count()
            plan_data["all_time_students"] = Membership.objects.filter(plan=plan).values("student").distinct().count()
            data.append(plan_data)
        return standard_response(data=data)


class AdminMembershipsView(generics.ListAPIView):
    permission_classes = [HasAdminPermission("manage_plans")]
    serializer_class = MembershipSerializer
    pagination_class = AdminStandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['status', 'student_id', 'plan_id']

    def get_queryset(self):
        return Membership.objects.select_related("student", "plan").all().order_by("-start_date")


class AdminMembershipActionView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def post(self, request, action):
        student = get_object_or_404(User, id=request.data.get("student_id"), role="student")
        plan = get_object_or_404(MembershipPlan, id=request.data.get("plan_id"))
        import datetime
        today = timezone.now().date()
        
        if action == "renew":
            current = Membership.objects.filter(student=student, status="active").order_by("-end_date").first()
            start = current.end_date + datetime.timedelta(days=1) if current else today
            renewal_count = (current.renewal_count + 1) if current else 1
        else:
            Membership.objects.filter(student=student, status="active").update(status="cancelled", is_active=False)
            start = _date(request.data.get("start_date"), today)
            renewal_count = 0
            
        end = _date(request.data.get("end_date"), start + datetime.timedelta(days=plan.duration_days))
        membership = Membership.objects.create(
            student=student,
            plan=plan,
            start_date=start,
            end_date=end,
            status="active",
            renewal_count=renewal_count,
            notes=request.data.get("notes"),
            created_by=_admin_user(request),
        )
        _activity(request, f"MEMBERSHIP_{action.upper()}", "Membership", membership.id, f"{action} membership for {student.username}")
        return standard_response(data=MembershipSerializer(membership).data, status_code=201)


class AdminMembershipDetailView(generics.RetrieveUpdateAPIView):
    permission_classes = [HasAdminPermission("manage_plans")]
    serializer_class = MembershipSerializer
    queryset = Membership.objects.all()

    def retrieve(self, request, *args, **kwargs):
        return standard_response(data=self.get_serializer(self.get_object()).data)

    def update(self, request, *args, **kwargs):
        membership = self.get_object()
        serializer = self.get_serializer(membership, data=request.data, partial=True)
        if serializer.is_valid():
            membership = serializer.save()
            return standard_response(data=self.get_serializer(membership).data)
        return standard_response("error", "Validation failed.", errors=serializer.errors, status_code=400)


class AdminMembershipSpecialView(APIView):
    permission_classes = [HasAdminPermission("manage_plans")]

    def get(self, request, kind):
        import datetime
        today = timezone.now().date()
        if kind == "expiring":
            days = int(request.query_params.get("days", 7))
            qs = Membership.objects.filter(status="active", end_date__lte=today + datetime.timedelta(days=days), end_date__gte=today)
        else:
            qs = Membership.objects.filter(end_date=today)
        return standard_response(data=MembershipSerializer(qs.select_related("student", "plan"), many=True).data)
